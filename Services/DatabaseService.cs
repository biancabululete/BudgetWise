using BudgetWise.Models;
using SQLite;

namespace BudgetWise.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public DatabaseService()
    {
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "budgetwise.db3");

        _db = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();

        try
        {
            if (_initialized)
                return;

            await _db.CreateTableAsync<Category>();
            await _db.CreateTableAsync<Transaction>();
            await _db.CreateTableAsync<Budget>();

            if (await _db.Table<Category>().CountAsync() == 0)
            {
                var categories = new List<Category>
                {
                    new() { Name = "Food", Type = "Expense", Icon = "food", Color = "#FFB86C" },
                    new() { Name = "Transport", Type = "Expense", Icon = "car", Color = "#A78BFA" },
                    new() { Name = "Shopping", Type = "Expense", Icon = "shopping", Color = "#60A5FA" },
                    new() { Name = "Bills", Type = "Expense", Icon = "bills", Color = "#F472B6" },
                    new() { Name = "Entertainment", Type = "Expense", Icon = "movie", Color = "#FBBF24" },
                    new() { Name = "Other", Type = "Expense", Icon = "other", Color = "#94A3B8" },
                    new() { Name = "Salary", Type = "Income", Icon = "salary", Color = "#7CF7E8" },
                    new() { Name = "Freelance", Type = "Income", Icon = "work", Color = "#34D399" },
                    new() { Name = "Other income", Type = "Income", Icon = "income", Color = "#22D3EE" }
                };

                await _db.InsertAllAsync(categories);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await InitializeAsync();

        return await _db.Table<Transaction>()
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionAsync(int id)
    {
        await InitializeAsync();

        return await _db.Table<Transaction>()
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveTransactionAsync(Transaction transaction)
    {
        await InitializeAsync();

        if (transaction.Id == 0)
            return await _db.InsertAsync(transaction);

        return await _db.UpdateAsync(transaction);
    }

    public async Task<int> DeleteTransactionAsync(Transaction transaction)
    {
        await InitializeAsync();
        return await _db.DeleteAsync(transaction);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await InitializeAsync();

        return await _db.Table<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<int> SaveCategoryAsync(Category category)
    {
        await InitializeAsync();

        return category.Id == 0
            ? await _db.InsertAsync(category)
            : await _db.UpdateAsync(category);
    }

    public async Task<int> DeleteCategoryAsync(Category category)
    {
        await InitializeAsync();
        return await _db.DeleteAsync(category);
    }

    public async Task<List<Budget>> GetBudgetsAsync(int month, int year)
    {
        await InitializeAsync();

        return await _db.Table<Budget>()
            .Where(b => b.Month == month && b.Year == year)
            .ToListAsync();
    }

    public async Task<Budget?> GetBudgetAsync(int id)
    {
        await InitializeAsync();

        return await _db.Table<Budget>()
            .Where(budget => budget.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveBudgetAsync(Budget budget)
    {
        await InitializeAsync();

        return budget.Id == 0
            ? await _db.InsertAsync(budget)
            : await _db.UpdateAsync(budget);
    }

    public async Task<int> DeleteBudgetAsync(Budget budget)
    {
        await InitializeAsync();
        return await _db.DeleteAsync(budget);
    }
}
