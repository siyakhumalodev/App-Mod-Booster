using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly ExpenseService _expenseService;
    private AzureOpenAIClient? _client;
    private string? _modelName;
    private bool _isConfigured;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, ExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        InitializeClient();
    }

    private void InitializeClient()
    {
        try
        {
            var endpoint = _configuration["OpenAI__Endpoint"];
            _modelName = _configuration["OpenAI__DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(_modelName))
            {
                _logger.LogWarning("OpenAI configuration not found. Chat service will return dummy responses.");
                _isConfigured = false;
                return;
            }

            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            Azure.Core.TokenCredential credential;

            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            _client = new AzureOpenAIClient(new Uri(endpoint), credential);
            _isConfigured = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
            _isConfigured = false;
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage> history)
    {
        if (!_isConfigured || _client == null || string.IsNullOrEmpty(_modelName))
        {
            return "GenAI services are not deployed. Please run 'bash deploy-with-chat.sh' to deploy Azure OpenAI and enable the AI assistant functionality.";
        }

        try
        {
            var chatClient = _client.GetChatClient(_modelName);
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt())
            };

            messages.AddRange(history);
            messages.Add(new UserChatMessage(userMessage));

            var options = new ChatCompletionOptions();
            
            // Define function tools for database operations
            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_all_expenses",
                functionDescription: "Retrieves all expenses from the database"
            ));

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_pending_expenses",
                functionDescription: "Retrieves all pending (submitted but not reviewed) expenses"
            ));

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_expenses_by_status",
                functionDescription: "Retrieves expenses filtered by status",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "status": {
                            "type": "string",
                            "description": "The status to filter by (Draft, Submitted, Approved, Rejected)",
                            "enum": ["Draft", "Submitted", "Approved", "Rejected"]
                        }
                    },
                    "required": ["status"]
                }
                """)
            ));

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "create_expense",
                functionDescription: "Creates a new expense entry",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "amount": {
                            "type": "number",
                            "description": "The amount in GBP"
                        },
                        "category": {
                            "type": "string",
                            "description": "The expense category",
                            "enum": ["Travel", "Meals", "Supplies", "Accommodation", "Other"]
                        },
                        "date": {
                            "type": "string",
                            "description": "The expense date in YYYY-MM-DD format"
                        },
                        "description": {
                            "type": "string",
                            "description": "Description of the expense"
                        }
                    },
                    "required": ["amount", "category", "date"]
                }
                """)
            ));

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "approve_expense",
                functionDescription: "Approves an expense by ID",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {
                            "type": "integer",
                            "description": "The ID of the expense to approve"
                        }
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: "reject_expense",
                functionDescription: "Rejects an expense by ID",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expenseId": {
                            "type": "integer",
                            "description": "The ID of the expense to reject"
                        }
                    },
                    "required": ["expenseId"]
                }
                """)
            ));

            // Initial chat completion
            var response = await chatClient.CompleteChatAsync(messages, options);
            var responseMessage = response.Value.Content[0].Text;

            // Handle function calling
            while (response.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(response.Value));

                foreach (var toolCall in response.Value.ToolCalls)
                {
                    var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments);
                    messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                }

                response = await chatClient.CompleteChatAsync(messages, options);
                responseMessage = response.Value.Content[0].Text;
            }

            return FormatResponse(responseMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"I encountered an error: {ex.Message}. Please try again.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData argumentsData)
    {
        try
        {
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsData.ToString());

            switch (functionName)
            {
                case "get_all_expenses":
                    var allExpenses = await _expenseService.GetAllExpensesAsync();
                    return JsonSerializer.Serialize(allExpenses);

                case "get_pending_expenses":
                    var pendingExpenses = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pendingExpenses);

                case "get_expenses_by_status":
                    var status = arguments?["status"].GetString() ?? "";
                    var expensesByStatus = await _expenseService.GetExpensesByStatusAsync(status);
                    return JsonSerializer.Serialize(expensesByStatus);

                case "create_expense":
                    var createRequest = new CreateExpenseRequest
                    {
                        UserId = 1, // Default user
                        CategoryId = GetCategoryId(arguments?["category"].GetString() ?? "Other"),
                        Amount = arguments?["amount"].GetDecimal() ?? 0,
                        Currency = "GBP",
                        ExpenseDate = DateTime.Parse(arguments?["date"].GetString() ?? DateTime.Today.ToString("yyyy-MM-dd")),
                        Description = arguments?.ContainsKey("description") == true ? arguments["description"].GetString() : null
                    };
                    var expenseId = await _expenseService.CreateExpenseAsync(createRequest);
                    return JsonSerializer.Serialize(new { success = true, expenseId });

                case "approve_expense":
                    var approveId = arguments?["expenseId"].GetInt32() ?? 0;
                    await _expenseService.ApproveExpenseAsync(approveId, 2); // Default manager
                    return JsonSerializer.Serialize(new { success = true });

                case "reject_expense":
                    var rejectId = arguments?["expenseId"].GetInt32() ?? 0;
                    await _expenseService.RejectExpenseAsync(rejectId, 2); // Default manager
                    return JsonSerializer.Serialize(new { success = true });

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private int GetCategoryId(string categoryName)
    {
        return categoryName switch
        {
            "Travel" => 1,
            "Meals" => 2,
            "Supplies" => 3,
            "Accommodation" => 4,
            _ => 5 // Other
        };
    }

    private string GetSystemPrompt()
    {
        return @"You are a helpful AI assistant for an Expense Management System. You can help users:
- View their expenses
- Create new expense entries
- Check pending expenses that need approval
- Approve or reject expenses (if they are a manager)

When listing expenses or data, always format the response in a clear, structured way using markdown formatting:
- Use **bold** for important information
- Use numbered lists (1. ) for ordered items
- Use bullet lists (- ) for unordered items
- Format amounts as currency (£XX.XX)

Be conversational, helpful, and concise. If you need to use a function to retrieve or modify data, do so automatically.";
    }

    private string FormatResponse(string response)
    {
        // The response is already formatted by the LLM with markdown
        // Client-side JavaScript will handle HTML escaping and markdown rendering
        return response;
    }
}
