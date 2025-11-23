using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    public class EdgeCaseTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;

        public EdgeCaseTests()
        {
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestLedgerContext(options);
            _expenseService = new ExpenseService(_context);
            _categoryService = new CategoryService(_context);
            _exportImportService = new ExportImportService(_expenseService, _categoryService);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Boundary Value Tests

        [Fact]
        public async Task AddExpense_MinimumValidAmount_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 0.01m, Date = DateTime.Today, CategoryId = category.Id };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_MaximumDecimalAmount_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 999999999.99m, Date = DateTime.Today, CategoryId = category.Id };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_TodayDate_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 10, Date = DateTime.Today, CategoryId = category.Id };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_VeryOldDate_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 10, Date = new DateTime(1900, 1, 1), CategoryId = category.Id };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddCategory_MaxLengthName_Succeeds()
        {
            // Arrange
            var category = new Category { Name = new string('A', 100) }; // Max length

            // Act
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_NotesExactly500Characters_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = new string('A', 500)
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            var saved = await _expenseService.GetExpenseAsync(expense.Id);
            Assert.Equal(500, saved?.Notes?.Length);
        }

        #endregion

        #region Special Character Tests

        [Fact]
        public async Task AddCategory_SpecialCharactersInName_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Food & Dining (2024)" };

            // Act
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_UnicodeCharactersInNotes_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Café ☕ 日本語 中文 한국어"
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            var saved = await _expenseService.GetExpenseAsync(expense.Id);
            Assert.Contains("☕", saved?.Notes);
        }

        [Fact]
        public async Task ExportCsv_NewlineInNotes_ProperlyEscaped()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Line 1\nLine 2"
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("\"Line 1\nLine 2\"", csv);
        }

        [Fact]
        public async Task ExportCsv_QuotesInNotes_ProperlyEscaped()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "He said \"hello\""
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("\"\"", csv); // Quotes should be doubled
        }

        #endregion

        #region Concurrent Operation Tests

        [Fact]
        public async Task AddMultipleExpenses_Concurrently_AllSucceed()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);

            var tasks = new List<Task<(bool Success, string Message)>>();
            for (int i = 0; i < 10; i++)
            {
                var expense = new Expense { Amount = i + 1, Date = DateTime.Today, CategoryId = category.Id };
                tasks.Add(_expenseService.AddExpenseAsync(expense));
            }

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.True(r.Success));
            var expenses = await _expenseService.GetExpensesAsync();
            Assert.Equal(10, expenses.Count);
        }

        #endregion

        #region Large Dataset Tests

        [Fact]
        public async Task GetExpenses_LargeDataset_ReturnsAll()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);

            // Add 150 expenses to test pagination (default page size is 100)
            for (int i = 0; i < 150; i++)
            {
                await _expenseService.AddExpenseAsync(new Expense 
                { 
                    Amount = i + 1, 
                    Date = DateTime.Today.AddDays(-i), 
                    CategoryId = category.Id 
                });
            }

            // Act
            var expenses = await _expenseService.GetExpensesAsync(pageSize: 200); // Request larger page size

            // Assert
            Assert.Equal(150, expenses.Count);
        }

        [Fact]
        public async Task GetCategorySummaries_ManyCategories_ReturnsAll()
        {
            // Arrange
            for (int i = 0; i < 50; i++)
            {
                var category = new Category { Name = $"Category {i}" };
                await _categoryService.AddCategoryAsync(category);
                await _expenseService.AddExpenseAsync(new Expense 
                { 
                    Amount = 10, 
                    Date = DateTime.Today, 
                    CategoryId = category.Id 
                });
            }

            // Act
            var summaries = await _expenseService.GetCategorySummariesAsync(
                DateTime.Today.AddDays(-1), 
                DateTime.Today);

            // Assert
            Assert.Equal(50, summaries.Count);
        }

        #endregion

        #region Null and Empty Tests

        [Fact]
        public async Task AddExpense_NullNotes_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = null
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddExpense_EmptyNotes_Succeeds()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = ""
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ImportCsv_EmptyNotes_Succeeds()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Test,";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        #endregion

        #region Date Range Tests

        [Fact]
        public async Task GetExpensesByDateRange_SameStartAndEnd_ReturnsExpensesOnThatDay()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var targetDate = new DateTime(2025, 1, 15);
            
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = targetDate, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = targetDate.AddDays(-1), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = targetDate.AddDays(1), CategoryId = category.Id });

            // Act
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(targetDate, targetDate);

            // Assert
            Assert.Single(expenses);
            Assert.Equal(10, expenses[0].Amount);
        }

        [Fact]
        public async Task GetExpensesByDateRange_NoExpensesInRange_ReturnsEmpty()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 10, 
                Date = new DateTime(2025, 1, 1), 
                CategoryId = category.Id 
            });

            // Act
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(
                new DateTime(2025, 2, 1), 
                new DateTime(2025, 2, 28));

            // Assert
            Assert.Empty(expenses);
        }

        #endregion

        #region Decimal Precision Tests

        [Fact]
        public async Task AddExpense_TwoDecimalPlaces_PreservesExactValue()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 123.45m, Date = DateTime.Today, CategoryId = category.Id };

            // Act
            await _expenseService.AddExpenseAsync(expense);
            var saved = await _expenseService.GetExpenseAsync(expense.Id);

            // Assert
            Assert.Equal(123.45m, saved?.Amount);
        }

        [Fact]
        public async Task GetTotalSpent_MultipleExpenses_CorrectSum()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10.50m, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20.75m, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30.25m, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var total = await _expenseService.GetTotalSpentAsync();

            // Assert
            Assert.Equal(61.50m, total);
        }

        #endregion

        #region CSV Edge Cases

        [Fact]
        public async Task ImportCsv_WindowsLineEndings_Succeeds()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\r\n2025-01-15,50.00,Test,Lunch";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        [Fact]
        public async Task ImportCsv_MixedLineEndings_Succeeds()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\r\n2025-01-15,50.00,Test,Lunch\n2025-01-16,30.00,Test,Dinner";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.ImportedCount);
        }

        [Fact]
        public async Task ImportCsv_TrailingComma_HandlesGracefully()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes,\n2025-01-15,50.00,Test,Lunch,";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
        }

        #endregion
    }
}
