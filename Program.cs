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
        var controller = new ExpenseController(expenseService, categoryService, exportImportService, context);

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
                        .PageSize(12)
                        .AddChoices(new[] {
                            "View all expenses",
                            "Add new expense",
                            "Edit expense",
                            "Delete expense",
                            "View category summaries",
                            "Manage categories",
                            "Export data to CSV",
                            "Import data from CSV",
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
                    case "Edit expense":
                        await EditExpense(controller);
                        break;
                    case "Delete expense":
                        await DeleteExpense(controller);
                        break;
                    case "View category summaries":
                        await ViewCategorySummaries(controller);
                        break;
                    case "Manage categories":
                        await ManageCategories(controller);
                        break;
                    case "Export data to CSV":
                        await ExportData(controller);
                        break;
                    case "Import data from CSV":
                        await ImportData(controller);
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
                summary.CategoryName ?? "Unknown",
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

    static async Task EditExpense(ExpenseController controller)
    {
        var expenses = await controller.GetAllExpensesAsync();

        if (!expenses.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No expenses found to edit.[/]");
            return;
        }

        var expenseChoices = expenses.Select(e => 
            $"{e.Id}: {e.Date:yyyy-MM-dd} - {e.Amount:C} - {e.Category?.Name ?? "No Category"}").ToList();

        var expenseChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an expense to edit:")
                .PageSize(10)
                .AddChoices(expenseChoices));

        var expenseId = int.Parse(expenseChoice.Split(':')[0]);
        var expense = await controller.GetExpenseByIdAsync(expenseId);

        if (expense == null)
        {
            AnsiConsole.MarkupLine("[red]Expense not found![/]");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]Editing expense from {expense.Date:yyyy-MM-dd}[/]");

        var categories = await controller.GetAllCategoriesAsync();
        var categoryChoices = categories.Select(c => $"{c.Id}: {c.Name}").ToList();

        var categoryChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select category (current: {expense.Category?.Name}):")
                .AddChoices(categoryChoices));

        var categoryId = int.Parse(categoryChoice.Split(':')[0]);
        var amount = AnsiConsole.Ask("Enter amount:", expense.Amount);
        var date = AnsiConsole.Ask("Enter date (yyyy-MM-dd):", expense.Date);
        var notes = AnsiConsole.Ask("Enter notes:", expense.Notes ?? "");

        expense.Amount = amount;
        expense.Date = date;
        expense.CategoryId = categoryId;
        expense.Notes = notes;

        var result = await controller.UpdateExpenseAsync(expense);
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
        }
    }

    static async Task DeleteExpense(ExpenseController controller)
    {
        var expenses = await controller.GetAllExpensesAsync();

        if (!expenses.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No expenses found to delete.[/]");
            return;
        }

        var expenseChoices = expenses.Select(e => 
            $"{e.Id}: {e.Date:yyyy-MM-dd} - {e.Amount:C} - {e.Category?.Name ?? "No Category"}").ToList();

        var expenseChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an expense to delete:")
                .PageSize(10)
                .AddChoices(expenseChoices));

        var expenseId = int.Parse(expenseChoice.Split(':')[0]);
        
        var confirm = AnsiConsole.Confirm($"Are you sure you want to delete this expense?");
        if (!confirm)
        {
            AnsiConsole.MarkupLine("[grey]Operation cancelled.[/]");
            return;
        }

        var result = await controller.DeleteExpenseAsync(expenseId);
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
        }
    }

    static async Task ManageCategories(ExpenseController controller)
    {
        bool managing = true;
        while (managing)
        {
            var categories = await controller.GetAllCategoriesAsync();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Category Management")
                    .AddChoices(new[] {
                        "View all categories",
                        "Add new category",
                        "Edit category",
                        "Delete category",
                        "Back to main menu"
                    }));

            switch (action)
            {
                case "View all categories":
                    var table = new Table();
                    table.AddColumn("ID");
                    table.AddColumn("Name");
                    foreach (var cat in categories)
                    {
                        table.AddRow(cat.Id.ToString(), cat.Name);
                    }
                    AnsiConsole.Write(table);
                    break;

                case "Add new category":
                    var newName = AnsiConsole.Ask<string>("Enter category name:");
                    var addResult = await controller.AddCategoryAsync(new Category { Name = newName });
                    if (addResult.Success)
                    {
                        AnsiConsole.MarkupLine($"[green]{addResult.Message}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]{addResult.Message}[/]");
                    }
                    break;

                case "Edit category":
                    if (!categories.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No categories to edit.[/]");
                        break;
                    }
                    var editChoices = categories.Select(c => $"{c.Id}: {c.Name}").ToList();
                    var editChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select category to edit:")
                            .AddChoices(editChoices));
                    var editId = int.Parse(editChoice.Split(':')[0]);
                    var editCat = categories.First(c => c.Id == editId);
                    var editedName = AnsiConsole.Ask("Enter new name:", editCat.Name);
                    editCat.Name = editedName;
                    var editResult = await controller.UpdateCategoryAsync(editCat);
                    if (editResult.Success)
                    {
                        AnsiConsole.MarkupLine($"[green]{editResult.Message}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]{editResult.Message}[/]");
                    }
                    break;

                case "Delete category":
                    if (!categories.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No categories to delete.[/]");
                        break;
                    }
                    var deleteChoices = categories.Select(c => $"{c.Id}: {c.Name}").ToList();
                    var deleteChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select category to delete:")
                            .AddChoices(deleteChoices));
                    var deleteId = int.Parse(deleteChoice.Split(':')[0]);
                    var confirmDelete = AnsiConsole.Confirm("Are you sure?");
                    if (confirmDelete)
                    {
                        var deleteResult = await controller.DeleteCategoryAsync(deleteId);
                        if (deleteResult.Success)
                        {
                            AnsiConsole.MarkupLine($"[green]{deleteResult.Message}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]{deleteResult.Message}[/]");
                        }
                    }
                    break;

                case "Back to main menu":
                    managing = false;
                    break;
            }

            if (managing)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                Console.ReadKey();
                AnsiConsole.Clear();
            }
        }
    }

    static async Task ClearAllData(ExpenseController controller)
    {
        var confirm = AnsiConsole.Confirm("Are you sure you want to delete all data? This cannot be undone!");

        if (confirm)
        {
            var result = await controller.ClearAllDataAsync();
            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]{result.Message}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]Operation cancelled.[/]");
        }
    }
}
