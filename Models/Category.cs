using SQLite;

namespace BudgetWise.Models;

public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    // Income / Expense
    public string Type { get; set; } = "Expense";

    public string Icon { get; set; } = string.Empty;

    public string Color { get; set; } = "#FFFFFF";
}
