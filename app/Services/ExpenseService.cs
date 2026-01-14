using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using Azure.Identity;
using Azure.Core;

namespace ExpenseManagement.Services;

public class ExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;
    private string? _lastError;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? LastError => _lastError;

    private SqlConnection GetConnection()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration");
            }

            var connection = new SqlConnection(connectionString);
            _lastError = null;
            return connection;
        }
        catch (Exception ex)
        {
            _lastError = $"Connection Error: {ex.Message} (ExpenseService.cs:GetConnection)";
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetAllExpenses", connection);
            using var reader = await command.ExecuteReaderAsync();

            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetAllExpensesAsync)";
            _logger.LogError(ex, "Failed to get all expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetExpensesByUser @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetExpensesByUserAsync)";
            _logger.LogError(ex, "Failed to get expenses for user {UserId}", userId);
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetExpenseById @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                _lastError = null;
                return MapExpenseFromReader(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetExpenseByIdAsync)";
            _logger.LogError(ex, "Failed to get expense {ExpenseId}", expenseId);
            return null;
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetPendingExpenses", connection);
            using var reader = await command.ExecuteReaderAsync();

            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetPendingExpensesAsync)";
            _logger.LogError(ex, "Failed to get pending expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetExpensesByStatus @StatusName", connection);
            command.Parameters.AddWithValue("@StatusName", statusName);

            using var reader = await command.ExecuteReaderAsync();

            var expenses = new List<Expense>();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpenseFromReader(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetExpensesByStatusAsync)";
            _logger.LogError(ex, "Failed to get expenses by status {StatusName}", statusName);
            return GetDummyExpenses();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "EXEC dbo.sp_CreateExpense @UserId, @CategoryId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile",
                connection);

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            _lastError = null;
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:CreateExpenseAsync)";
            _logger.LogError(ex, "Failed to create expense");
            throw;
        }
    }

    public async Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "EXEC dbo.sp_UpdateExpense @ExpenseId, @CategoryId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile",
                connection);

            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return reader.GetInt32(0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:UpdateExpenseAsync)";
            _logger.LogError(ex, "Failed to update expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_SubmitExpense @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return reader.GetInt32(0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:SubmitExpenseAsync)";
            _logger.LogError(ex, "Failed to submit expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_ApproveExpense @ExpenseId, @ReviewedBy", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return reader.GetInt32(0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:ApproveExpenseAsync)";
            _logger.LogError(ex, "Failed to approve expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_RejectExpense @ExpenseId, @ReviewedBy", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return reader.GetInt32(0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:RejectExpenseAsync)";
            _logger.LogError(ex, "Failed to reject expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_DeleteExpense @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return reader.GetInt32(0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:DeleteExpenseAsync)";
            _logger.LogError(ex, "Failed to delete expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetCategories", connection);
            using var reader = await command.ExecuteReaderAsync();

            var categories = new List<ExpenseCategory>();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }

            _lastError = null;
            return categories;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetCategoriesAsync)";
            _logger.LogError(ex, "Failed to get categories");
            return new List<ExpenseCategory>
            {
                new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
                new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
                new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true }
            };
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand("EXEC dbo.sp_GetUsers", connection);
            using var reader = await command.ExecuteReaderAsync();

            var users = new List<User>();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    RoleName = reader.GetString(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    IsActive = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                });
            }

            _lastError = null;
            return users;
        }
        catch (Exception ex)
        {
            _lastError = $"Database Error: {ex.Message} (ExpenseService.cs:GetUsersAsync)";
            _logger.LogError(ex, "Failed to get users");
            return new List<User>
            {
                new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true }
            };
        }
    }

    private Expense MapExpenseFromReader(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Demo User",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                Amount = 25.40m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi to client site",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Demo User",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                Amount = 14.25m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Client lunch meeting",
                CreatedAt = DateTime.Now.AddDays(-10)
            }
        };
    }
}
