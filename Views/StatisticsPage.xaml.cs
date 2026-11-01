using System.Collections.ObjectModel;
using System.Globalization;
using BudgetWise.Graphics;
using BudgetWise.Services;

namespace BudgetWise.Views;

public partial class StatisticsPage : ContentPage
{
    private static readonly Color[] ChartColors =
    {
        Color.FromArgb("#C9F27A"),
        Color.FromArgb("#7CF7E8"),
        Color.FromArgb("#D56E75"),
        Color.FromArgb("#8CA98A"),
        Color.FromArgb("#73A7CB"),
        Color.FromArgb("#C0A0EF"),
        Color.FromArgb("#F2B982"),
        Color.FromArgb("#E58F9A")
    };

    private readonly DatabaseService _database;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("ro-RO");
    private readonly CategoryDonutDrawable _donutDrawable = new();
    private readonly MonthlyBarsDrawable _barsDrawable = new();
    private bool _loading;
    private bool _refreshPending;

    public ObservableCollection<CategoryLegendItem> CategoryLegend { get; } = new();

    public StatisticsPage(DatabaseService database)
    {
        InitializeComponent();

        _database = database;
        BindingContext = this;
        DonutChart.Drawable = _donutDrawable;
        MonthlyBarsChart.Drawable = _barsDrawable;

        _loading = true;
        PeriodPicker.Date = DateTime.Today;
        _loading = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatisticsAsync();
    }

    private async void OnPeriodChanged(object sender, DateChangedEventArgs e)
    {
        await LoadStatisticsAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        if (_loading)
        {
            _refreshPending = true;
            return;
        }

        _loading = true;
        try
        {
            do
            {
                _refreshPending = false;
                await RefreshStatisticsAsync();
            }
            while (_refreshPending);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RefreshStatisticsAsync()
    {
        try
        {
            DateTime selectedDate = PeriodPicker.Date ?? DateTime.Today;
            DateTime monthStart = new(selectedDate.Year, selectedDate.Month, 1);
            var transactions = await _database.GetTransactionsAsync();
            var categories = await _database.GetCategoriesAsync();
            var categoryLookup = categories.ToDictionary(category => category.Id, category => category.Name);

            var monthlyTransactions = transactions
                .Where(transaction => transaction.Date >= monthStart && transaction.Date < monthStart.AddMonths(1))
                .ToList();

            long income = monthlyTransactions
                .Where(transaction => transaction.Type == "Income")
                .Sum(transaction => transaction.AmountRonMinor);
            long expenses = monthlyTransactions
                .Where(transaction => transaction.Type == "Expense")
                .Sum(transaction => transaction.AmountRonMinor);
            long net = income - expenses;

            IncomeLabel.Text = FormatMoney(income);
            ExpensesLabel.Text = FormatMoney(expenses);
            NetLabel.Text = FormatMoney(net);
            NetLabel.TextColor = Color.FromArgb(net < 0 ? "#D56E75" : "#7CF7E8");

            var categoryGroups = monthlyTransactions
                .Where(transaction => transaction.Type == "Expense")
                .GroupBy(transaction => transaction.CategoryId)
                .Select(group => new
                {
                    CategoryId = group.Key,
                    AmountMinor = group.Sum(transaction => transaction.AmountRonMinor)
                })
                .Where(group => group.AmountMinor > 0)
                .OrderByDescending(group => group.AmountMinor)
                .ToList();

            CategoryLegend.Clear();
            var slices = new List<CategorySlice>(categoryGroups.Count);
            for (int index = 0; index < categoryGroups.Count; index++)
            {
                var group = categoryGroups[index];
                string name = categoryLookup.GetValueOrDefault(group.CategoryId, "Unknown category");
                Color color = ChartColors[index % ChartColors.Length];
                slices.Add(new CategorySlice(name, group.AmountMinor, color));

                decimal percentage = expenses > 0
                    ? group.AmountMinor * 100m / expenses
                    : 0;

                CategoryLegend.Add(new CategoryLegendItem
                {
                    Name = name,
                    DotColor = color,
                    AmountText = FormatMoney(group.AmountMinor),
                    PercentageText = percentage.ToString("0.#", _culture) + "%"
                });
            }

            _donutDrawable.Slices = slices;
            DonutChart.IsVisible = slices.Count > 0;
            CategoryEmptyLabel.IsVisible = slices.Count == 0;
            DonutChart.Invalidate();

            var monthlyPoints = new List<MonthlyChartPoint>(6);
            for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
            {
                DateTime start = monthStart.AddMonths(-monthsAgo);
                DateTime end = start.AddMonths(1);
                var monthTransactions = transactions
                    .Where(transaction => transaction.Date >= start && transaction.Date < end)
                    .ToList();

                long monthIncome = monthTransactions
                    .Where(transaction => transaction.Type == "Income")
                    .Sum(transaction => transaction.AmountRonMinor);
                long monthExpenses = monthTransactions
                    .Where(transaction => transaction.Type == "Expense")
                    .Sum(transaction => transaction.AmountRonMinor);

                monthlyPoints.Add(new MonthlyChartPoint(
                    start.ToString("MMM", _culture), monthIncome, monthExpenses));
            }

            _barsDrawable.Points = monthlyPoints;
            MonthlyBarsChart.Invalidate();

            long sixMonthIncome = monthlyPoints.Sum(point => point.IncomeMinor);
            long sixMonthExpenses = monthlyPoints.Sum(point => point.ExpenseMinor);
            TrendSummaryLabel.Text = sixMonthIncome == 0 && sixMonthExpenses == 0
                ? "No transactions in this period"
                : $"6-month net: {FormatMoney(sixMonthIncome - sixMonthExpenses)}";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Statistics error", ex.Message, "OK");
        }
    }

    private string FormatMoney(long amountMinor)
    {
        return (amountMinor / 100m).ToString("N2", _culture) + " RON";
    }
}

public sealed class CategoryLegendItem
{
    public string Name { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string PercentageText { get; set; } = string.Empty;
    public Color DotColor { get; set; } = Colors.Gray;
}
