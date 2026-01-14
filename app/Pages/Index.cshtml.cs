using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ExpenseService _expenseService;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IndexModel(ILogger<IndexModel> logger, ExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            ErrorMessage = _expenseService.LastError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading page");
            ErrorMessage = $"Error loading data: {ex.Message} (Index.cshtml.cs:OnGetAsync). If this is a managed identity issue, ensure the App Service has a user-assigned managed identity with proper database permissions.";
        }
    }
}
