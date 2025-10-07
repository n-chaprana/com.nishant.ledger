using Domain.Controllers;
using Domain.Data;
using Domain.Models;
using Domain.Services;
using Spectre.Console;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up SQLite to use the bundled provider
        SQLitePCL.Batteries_V2.Init();

        // Initialize MVC components
        var context = new LedgerContext();
        var categoryService = new CategoryService(context);
        var expenseService = new ExpenseService(context);
        var exportImportService = new ExportImportService(expenseService, categoryService);
        var controller = new ExpenseController(expenseService, categoryService, exportImportService);

        AnsiConsole.Write(
            new FigletText("Daily Ledger")
                .Centered()
                .Color(Color.Green));

        AnsiConsole.WriteLine();

        try
        {
            // Initialize database through Controller
            await AnsiConsole.Status()
                .StartAsync("Initializing database...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    await controller.InitializeDatabaseAsync();
                });

            // Main menu loop
            bool running = true;
            while (running)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to do?")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "View all expenses",
                            "Add new expense",
                            "View category summaries",
                            "Export data to CSV",
                            "Import data from CSV",
                            "View settings",
                            "Clear all data",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "View all expenses":
                        await ViewAllExpenses(controller);
                        break;
                    case "Add new expense":
                        await AddNewExpense(controller);
                        break;
                    case "View category summaries":
                        await ViewCategorySummaries(controller);
                        break;
                    case "Export data to CSV":
                        await ExportData(controller);
                        break;
                    case "Import data from CSV":
                        await ImportData(controller);
                        break;
                    case "View settings":
                        await ViewSettings(controller);
                        break;
                    case "Clear all data":
                        await ClearAllData(controller);
                        break;
                    case "Exit":
                        running = false;
                        break;
                }

                if (running)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                    Console.ReadKey();
                    AnsiConsole.Clear();
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        AnsiConsole.MarkupLine("[green]Thank you for using Daily Ledger![/]");
    }

    static async Task ViewAllExpenses(ExpenseController controller)
    {
        var expenses = await controller.GetAllExpensesAsync();

        if (!expenses.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No expenses found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Date");
        table.AddColumn("Amount");
        table.AddColumn("Category");
        table.AddColumn("Notes");

        foreach (var expense in expenses)
        {
            table.AddRow(
                expense.Date.ToString("yyyy-MM-dd"),
                expense.Amount.ToString("C"),
                expense.Category?.Name ?? "No Category",
                expense.Notes ?? ""
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[bold green]Total: {expenses.Sum(e => e.Amount):C} across {expenses.Count} expenses[/]");
    }

    static async Task AddNewExpense(ExpenseController controller)
    {
        var categories = await controller.GetAllCategoriesAsync();

        var categoryChoices = categories.Select(c => $"{c.Id}: {c.Name}").ToList();
        categoryChoices.Add("Add new category");

        var categoryChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a category:")
                .AddChoices(categoryChoices));

        int categoryId;
        if (categoryChoice == "Add new category")
        {
            var newCategoryName = AnsiConsole.Ask<string>("Enter new category name:");
            var newCategory = new Category { Name = newCategoryName };
            await controller.AddCategoryAsync(newCategory);
            categoryId = newCategory.Id;
            categories = await controller.GetAllCategoriesAsync(); // Refresh
        }
        else
        {
            categoryId = int.Parse(categoryChoice.Split(':')[0]);
        }

        var amount = AnsiConsole.Ask<decimal>("Enter amount:");
        var date = AnsiConsole.Ask<DateTime>("Enter date (yyyy-MM-dd):");
        var notes = AnsiConsole.Ask<string>("Enter notes (optional):");

        var expense = new Expense
        {
            Amount = amount,
            Date = date,
            CategoryId = categoryId,
            Notes = notes
        };

        await controller.AddExpenseAsync(expense);
        AnsiConsole.MarkupLine("[green]Expense added successfully![/]");
    }

    static async Task ViewCategorySummaries(ExpenseController controller)
    {
        var startDate = DateTime.Today.AddDays(-30); // Last 30 days
        var endDate = DateTime.Today;

        var summaries = await controller.GetCategorySummariesAsync(startDate, endDate);

        if (!summaries.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No expenses found in the last 30 days.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Category");
        table.AddColumn("Total Amount");
        table.AddColumn("Transactions");
        table.AddColumn("Percentage");

        var totalAmount = summaries.Sum(s => s.TotalAmount);

        foreach (var summary in summaries)
        {
            var percentage = totalAmount > 0 ? (summary.TotalAmount / totalAmount) * 100 : 0;
            table.AddRow(
                summary.CategoryName,
                summary.TotalAmount.ToString("C"),
                summary.ExpenseCount.ToString(),
                $"{percentage:F1}%"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[bold green]Total for period: {totalAmount:C}[/]");
    }

    static async Task ExportData(ExpenseController controller)
    {
        var csvContent = await controller.ExportToCsvAsync();
        await File.WriteAllTextAsync("expenses_export.csv", csvContent);

        AnsiConsole.MarkupLine("[green]Data exported to expenses_export.csv[/]");
        AnsiConsole.MarkupLine($"[grey]File contains {csvContent.Split('\n').Length - 1} rows[/]");
    }

    static async Task ImportData(ExpenseController controller)
    {
        var filePath = AnsiConsole.Ask<string>("Enter CSV file path:");

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine("[red]File not found![/]");
            return;
        }

        var csvContent = await File.ReadAllTextAsync(filePath);
        var result = await controller.ImportFromCsvAsync(csvContent);

        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
        }
    }

    static async Task ViewSettings(ExpenseController controller)
    {
        var categories = await controller.GetAllCategoriesAsync();

        AnsiConsole.MarkupLine("[bold]Current Categories:[/]");
        foreach (var category in categories)
        {
            AnsiConsole.MarkupLine($"• {category.Name}");
        }
    }

    static async Task ClearAllData(ExpenseController controller)
    {
        var confirm = AnsiConsole.Confirm("Are you sure you want to delete all data? This cannot be undone!");

        if (confirm)
        {
            await controller.ClearAllDataAsync();
            AnsiConsole.MarkupLine("[green]All data cleared successfully![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Operation cancelled.[/]");
        }
    }
}
