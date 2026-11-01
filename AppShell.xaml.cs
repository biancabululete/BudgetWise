using BudgetWise.Views;

namespace BudgetWise;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(TransactionFormPage),
            typeof(TransactionFormPage));

        Routing.RegisterRoute(
            nameof(BudgetFormPage),
            typeof(BudgetFormPage));

        Routing.RegisterRoute(
            nameof(CurrencyConverterPage),
            typeof(CurrencyConverterPage));
    }
}
