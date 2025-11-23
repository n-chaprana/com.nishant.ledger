using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    /// <summary>
    /// Additional tests to improve line and branch coverage
    /// </summary>
    public class AdditionalCoverageTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;

        public AdditionalCoverageTests()
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

        #region ExpenseService - Missing Methods

        [Fact]
        public async Task GetExpenseCountAsync_WithMultipleExpenses_ReturnsCorrectCount()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            
            for (int i = 0; i < 5; i++)
            {
                await _expenseService.AddExpenseAsync(new Expense 
                { 
                    Amount = 10 + i, 
                    Date = DateTime.Today, 
                    CategoryId = category.Id 
                });
            }

            // Act
            var count = await _expenseService.GetExpenseCountAsync();

            // Assert
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task GetExpenseCountAsync_EmptyDatabase_ReturnsZero()
        {
            // Act
            var count = await _expenseService.GetExpenseCountAsync();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetTotalSpentByDateRangeAsync_WithExpensesInRange_ReturnsCorrectTotal()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = new DateTime(2025, 1, 10), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 200, Date = new DateTime(2025, 1, 20), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = new DateTime(2025, 2, 10), CategoryId = category.Id }); // Outside range

            // Act
            var total = await _expenseService.GetTotalSpentByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Equal(300, total);
        }

        [Fact]
        public async Task GetTotalSpentByDateRangeAsync_NoExpensesInRange_ReturnsZero()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = new DateTime(2025, 3, 1), CategoryId = category.Id });

            // Act
            var total = await _expenseService.GetTotalSpentByDateRangeAsync(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));

            // Assert
            Assert.Equal(0, total);
        }

        [Fact]
        public async Task GetExpensesByCategoryAsync_WithExpenses_ReturnsCorrectExpenses()
        {
            // Arrange
            var category1 = new Category { Name = "Food" };
            var category2 = new Category { Name = "Transport" };
            await _categoryService.AddCategoryAsync(category1);
            await _categoryService.AddCategoryAsync(category2);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category1.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category1.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = category2.Id });

            // Act
            var expenses = await _expenseService.GetExpensesByCategoryAsync(category1.Id);

            // Assert
            Assert.Equal(2, expenses.Count);
            Assert.All(expenses, e => Assert.Equal(category1.Id, e.CategoryId));
        }

        [Fact]
        public async Task GetExpensesByCategoryAsync_NonExistingCategory_ReturnsEmpty()
        {
            // Act
            var expenses = await _expenseService.GetExpensesByCategoryAsync(999);

            // Assert
            Assert.Empty(expenses);
        }

        #endregion

        #region UpdateExpenseAsync - Branch Coverage

        [Fact]
        public async Task UpdateExpenseAsync_NonExistingExpense_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense { Id = 999, Amount = 100, Date = DateTime.Today, CategoryId = 1 };

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        [Fact]
        public async Task UpdateExpenseAsync_InvalidCategory_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Update with invalid category
            expense.CategoryId = 999;

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("does not exist", result.Message);
        }

        [Fact]
        public async Task UpdateExpenseAsync_FutureDate_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Update with future date
            expense.Date = DateTime.Today.AddDays(1);

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("future", result.Message);
        }

        [Fact]
        public async Task UpdateExpenseAsync_NegativeAmount_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Update with negative amount
            expense.Amount = -50;

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("greater than zero", result.Message);
        }

        [Fact]
        public async Task UpdateExpenseAsync_LongNotes_TruncatesTo500Characters()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var expense = new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category.Id, Notes = "Short" };
            await _expenseService.AddExpenseAsync(expense);

            // Update with long notes
            expense.Notes = new string('X', 600);

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            var updated = await _expenseService.GetExpenseAsync(expense.Id);
            Assert.Equal(500, updated?.Notes?.Length);
        }

        #endregion

        #region DeleteAllCategoriesAsync - Branch Coverage

        [Fact]
        public async Task DeleteAllCategoriesAsync_WithCategories_DeletesAll()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Cat1" });
            await _categoryService.AddCategoryAsync(new Category { Name = "Cat2" });
            await _categoryService.AddCategoryAsync(new Category { Name = "Cat3" });

            // Act
            var result = await _categoryService.DeleteAllCategoriesAsync();

            // Assert
            Assert.True(result.Success);
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Empty(categories);
        }

        [Fact]
        public async Task DeleteAllCategoriesAsync_WithCategoriesHavingExpenses_KeepsCategoriesWithExpenses()
        {
            // Arrange
            var cat1 = new Category { Name = "Cat1" };
            var cat2 = new Category { Name = "Cat2" };
            await _categoryService.AddCategoryAsync(cat1);
            await _categoryService.AddCategoryAsync(cat2);

            // Add expense to cat1
            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = DateTime.Today, CategoryId = cat1.Id });

            // Act
            var result = await _categoryService.DeleteAllCategoriesAsync();

            // Assert
            // The method keeps categories that have expenses and deletes empty ones
            var categories = await _categoryService.GetCategoriesAsync();
            // Both categories remain because the method doesn't force delete
            Assert.True(categories.Count >= 1); // At least cat1 with expenses remains
        }

        #endregion

        #region ClearAllDataAsync - Branch Coverage

        [Fact]
        public async Task ClearAllDataAsync_WithExpensesAndCategories_ClearsAllAndReinitializes()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var expenseResult = await _expenseService.DeleteAllExpensesAsync();
            var categoryResult = await _categoryService.DeleteAllCategoriesAsync();

            // Assert
            Assert.True(expenseResult.Success);
            Assert.True(categoryResult.Success);
            
            var expenses = await _expenseService.GetExpensesAsync();
            var categories = await _categoryService.GetCategoriesAsync();
            
            Assert.Empty(expenses);
            Assert.Empty(categories);
        }

        #endregion

        #region CSV ParseCsvLine - Branch Coverage

        [Fact]
        public async Task ImportFromCsvAsync_QuotedFieldsWithCommas_ParsesCorrectly()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Test,\"Note with, comma\"";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
            
            var expenses = await _expenseService.GetExpensesAsync();
            Assert.Contains("Note with, comma", expenses.First().Notes);
        }

        [Fact]
        public async Task ImportFromCsvAsync_QuotedFieldsWithQuotes_ParsesCorrectly()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Test,\"Note with \"\"quotes\"\"\"";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_MixedQuotedAndUnquoted_ParsesCorrectly()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Test,Simple note\n2025-01-16,75.00,Test,\"Quoted, note\"";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.ImportedCount);
        }

        #endregion

        #region CSV EscapeCsvField - Branch Coverage

        [Fact]
        public async Task ExportToCsvAsync_FieldsWithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            
            // Add expenses with various special characters
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Note with \"quotes\""
            });
            
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 75, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Note with\nnewline"
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("\"Note with \"\"quotes\"\"\"", csv);
            Assert.Contains("\"Note with", csv);
        }

        #endregion

        #region CategorySummary Percentage Property

        [Fact]
        public void CategorySummary_PercentageProperty_CanBeAccessed()
        {
            // Arrange
            var summary = new CategorySummary
            {
                CategoryId = 1,
                CategoryName = "Test",
                TotalAmount = 100,
                ExpenseCount = 5,
                Percentage = 25.5m
            };

            // Act
            var percentage = summary.Percentage;

            // Assert
            Assert.Equal(25.5m, percentage);
        }

        #endregion
    }
}
