namespace BudgetWise.Services;

public static class CategoryIcons
{
    public const string Fallback = "cat_other";

    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "food", "car", "shopping", "bills", "movie", "other", "salary", "work", "income"
    };

    // Category.Icon holds a short name such as "food"; the matching image is cat_food.svg.
    public static string For(string? icon)
    {
        return !string.IsNullOrWhiteSpace(icon) && Known.Contains(icon)
            ? "cat_" + icon.ToLowerInvariant()
            : Fallback;
    }
}
