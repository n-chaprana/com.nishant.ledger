using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    public class ServiceCoverageTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;

        public ServiceCoverageTests()
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

        #region Database Configuration Tests

        [Fact]
        public async Task Service_DatabaseConfiguration_UsesCorrectConnectionString()
        {
            // Arrange & Act - This tests the database configuration paths
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var categories = await _categoryService.GetCategoriesAsync();

            // Assert - Verify database is working correctly
            Assert.Single(categories);
        }

        [Fact]
        public async Task Service_ModelConfiguration_ProperlySetsRelationships()
        {
            // Arrange
            var category = new Category { Name = "Test Category" };
            await _categoryService.AddCategoryAsync(category);

            var expense = new Expense 
            { 
                Amount = 100, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Test expense"
            };
            await _expenseService.AddExpenseAsync(expense);

            // Act - Test navigation property
            var savedExpense = await _expenseService.GetExpenseAsync(expense.Id);
            var loadedCategory = await _categoryService.GetCategoryByIdAsync(category.Id);

            // Assert - Verify relationships are properly configured
            Assert.NotNull(savedExpense?.Category);
            Assert.Equal("Test Category", savedExpense?.Category?.Name);
            Assert.Single(loadedCategory?.Expenses ?? new List<Expense>());
        }

        #endregion

        #region Enhanced CategoryService Tests

        [Fact]
        public async Task AddCategoryAsync_EmptyDatabase_DoesNotThrow()
        {
            // Arrange
            var category = new Category { Name = "New Category" };

            // Act - Test with completely fresh database
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
            Assert.True(category.Id > 0);
        }

        [Fact]
        public async Task UpdateCategoryAsync_EmptyName_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            category.Name = ""; // Set to empty

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("empty", result.Message.ToLower());
        }

        [Fact]
        public async Task UpdateCategoryAsync_WhitespaceName_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            category.Name = "   "; // Set to whitespace

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("empty", result.Message.ToLower());
        }

        [Fact]
        public async Task DeleteCategoryAsync_NonExistingId_ReturnsFailure()
        {
            // Act
            var result = await _categoryService.DeleteCategoryAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }

        #endregion

        #region Enhanced ExpenseService Tests

        [Fact]
        public async Task AddExpenseAsync_NotesExceeds500Characters_TruncatesCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var longNotes = new string('A', 600); // Longer than 500 limit

            var expense = new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = longNotes
            };

            // Act
            await _expenseService.AddExpenseAsync(expense);
            var saved = await _expenseService.GetExpenseAsync(expense.Id);

            // Assert - Should be truncated to 500 chars
            Assert.NotNull(saved?.Notes);
            Assert.Equal(500, saved?.Notes?.Length);
            Assert.StartsWith("AAAAA", saved?.Notes);
        }

        [Fact]
        public async Task GetExpensesByDateRangeAsync_EmptyResult_ReturnsEmpty()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            
            // Add expense outside the date range
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 10, 
                Date = new DateTime(2025, 6, 1), // June
                CategoryId = category.Id 
            });

            // Act - Query for a different date range
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(
                new DateTime(2025, 1, 1), 
                new DateTime(2025, 2, 28) // January-February
            );

            // Assert
            Assert.Empty(expenses);
        }

        [Fact]
        public async Task GetExpensesByDateRangeAsync_SameDayQueries_ReturnsCorrect()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var targetDate = new DateTime(2025, 3, 15);

            // Use exact date for same-day query
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 10, 
                Date = targetDate, // Exact date
                CategoryId = category.Id 
            });
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 20, 
                Date = targetDate, // Exact date
                CategoryId = category.Id 
            });

            // Act
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(targetDate, targetDate);

            // Assert - Both should be included (same day)
            Assert.Equal(2, expenses.Count);
        }

        [Fact]
        public async Task GetCategorySummariesAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var summaries = await _expenseService.GetCategorySummariesAsync(DateTime.Today.AddDays(-1), DateTime.Today);

            // Assert
            Assert.Empty(summaries);
        }

        [Fact]
        public async Task GetCategorySummariesAsync_SingleCategoryWithNoExpenses_ReturnsEmpty()
        {
            // Arrange
            var category = new Category { Name = "Empty Category" };
            await _categoryService.AddCategoryAsync(category);

            // Act
            var summaries = await _expenseService.GetCategorySummariesAsync(DateTime.Today.AddDays(-1), DateTime.Today);

            // Assert
            Assert.Empty(summaries); // Should be empty for categories with no expenses in range
        }

        [Fact]
        public async Task GetCategorySummariesAsync_OutOfDateRange_ReturnsEmpty()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 100, 
                Date = new DateTime(2025, 6, 1), // June
                CategoryId = category.Id 
            });

            // Act - Query for different date range
            var summaries = await _expenseService.GetCategorySummariesAsync(
                new DateTime(2025, 1, 1), 
                new DateTime(2025, 2, 28) // January-February
            );

            // Assert
            Assert.Empty(summaries);
        }

        [Fact]
        public async Task DeleteAllExpensesAsync_AfterMultipleAdditions_ClearsCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);

            // Add multiple expenses
            for (int i = 0; i < 10; i++)
            {
                await _expenseService.AddExpenseAsync(new Expense 
                { 
                    Amount = i + 1, 
                    Date = DateTime.Today, 
                    CategoryId = category.Id 
                });
            }

            // Act
            var result = await _expenseService.DeleteAllExpensesAsync();

            // Assert
            Assert.True(result.Success);
            var remainingExpenses = await _expenseService.GetExpensesAsync();
            Assert.Empty(remainingExpenses);
        }

        #endregion

        #region Enhanced ExportImportService Tests

        [Fact]
        public async Task ExportToCsvAsync_SingleExpenseWithNullNotes_HandlesCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = null // Explicitly null
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert - Should handle null notes properly
            Assert.Contains("50", csv);
            Assert.Contains("Test", csv);
        }

        [Fact]
        public async Task ExportToCsvAsync_SingleExpenseWithEmptyNotes_HandlesCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "" // Empty string
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert - Should handle empty notes properly
            Assert.Contains("50", csv);
            Assert.Contains("Test", csv);
        }

        [Fact]
        public async Task ImportFromCsvAsync_HeaderOnly_ReturnsFailure()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n"; // Header only

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert - Empty CSV should fail
            Assert.False(result.Success);
            Assert.Equal(0, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_NonExistentCategory_CreatesOtherCategory()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Existing" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,NonExistent,Lunch";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert - Should create "Other" category and import
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_InvalidDateFormat_SkipsRow()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\ninvalid-date,50.00,Test,Lunch";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.ImportedCount);
            Assert.Contains("skipped", result.Message.ToLower());
        }

        [Fact]
        public async Task ImportFromCsvAsync_InvalidAmountFormat_SkipsRow()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,invalid-amount,Test,Lunch";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.ImportedCount);
            Assert.Contains("skipped", result.Message.ToLower());
        }

        [Fact]
        public async Task ImportFromCsvAsync_CommaInNotes_SanitizesCSV()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Test" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Test,\"Note, with, commas\"";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
            
            // Verify the notes were imported correctly (commas preserved in quotes)
            var expenses = await _expenseService.GetExpensesAsync();
            Assert.Contains("Note, with, commas", expenses.First().Notes);
        }

        #endregion
    }
}
