using SQLite;

namespace BudgetWise.Models;

public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 1250 = 12.50 in the transaction currency
    public long AmountMinor { get; set; }

    public string Currency { get; set; } = "RON";

    // RON equivalent, expressed in bani
    public long AmountRonMinor { get; set; }

    public string Type { get; set; } = "Expense";

    public int CategoryId { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string Description { get; set; } = string.Empty;
}
