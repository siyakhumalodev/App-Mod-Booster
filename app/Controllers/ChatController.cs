using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;
using OpenAI.Chat;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChatController(ILogger<ChatController> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> PostMessage([FromBody] ChatRequest request)
    {
        try
        {
            // Try to get ChatService from DI
            var chatService = _serviceProvider.GetService<ChatService>();
            
            if (chatService == null)
            {
                return Ok(new ChatResponse
                {
                    Response = "GenAI services are not deployed. Please run 'bash deploy-with-chat.sh' to deploy Azure OpenAI and enable the AI assistant functionality."
                });
            }

            var history = request.History?.Select(h => h.Role == "user" 
                ? (ChatMessage)new UserChatMessage(h.Content)
                : (ChatMessage)new AssistantChatMessage(h.Content))
                .ToList() ?? new List<ChatMessage>();

            var response = await chatService.GetChatResponseAsync(request.Message, history);
            
            return Ok(new ChatResponse { Response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return Ok(new ChatResponse 
            { 
                Response = "I apologize, but I encountered an error. Please try again or contact support if the issue persists." 
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<MessageHistory>? History { get; set; }
    }

    public class MessageHistory
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}
