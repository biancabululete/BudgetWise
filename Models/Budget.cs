using SQLite;

namespace BudgetWise.Models;

public class Budget
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CategoryId { get; set; }

    // Buget exprimat in bani (1 RON = 100 bani)
    public long LimitMinor { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }
}
