using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    public class ExportImportServiceTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;

        public ExportImportServiceTests()
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

        #region ExportToCsvAsync Tests

        [Fact]
        public async Task ExportToCsvAsync_EmptyDatabase_ReturnsHeaderOnly()
        {
            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("Date,Amount,Category,Notes", csv);
            Assert.Equal(2, csv.Split('\n').Length); // Header + empty line
        }

        [Fact]
        public async Task ExportToCsvAsync_WithExpenses_ReturnsCorrectFormat()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50.00m, 
                Date = new DateTime(2025, 1, 15), 
                CategoryId = category.Id,
                Notes = "Lunch"
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("2025-01-15", csv);
            Assert.Contains("50", csv);
            Assert.Contains("Food", csv);
            Assert.Contains("Lunch", csv);
        }

        [Fact]
        public async Task ExportToCsvAsync_WithSpecialCharactersInNotes_EscapesCorrectly()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50.00m, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "Lunch, with comma"
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("\"Lunch, with comma\"", csv);
        }

        [Fact]
        public async Task ExportToCsvAsync_WithDangerousCharacters_SanitizesForCSVInjection()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50.00m, 
                Date = DateTime.Today, 
                CategoryId = category.Id,
                Notes = "=SUM(A1:A10)"
            });

            // Act
            var csv = await _exportImportService.ExportToCsvAsync();

            // Assert
            Assert.Contains("'=SUM(A1:A10)", csv); // Should be prefixed with '
        }

        #endregion

        #region ImportFromCsvAsync Tests

        [Fact]
        public async Task ImportFromCsvAsync_ValidCsv_ImportsSuccessfully()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Food,Lunch";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_EmptyCsv_ReturnsFailure()
        {
            // Arrange
            var csv = "";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("empty or invalid", result.Message);
        }

        [Fact]
        public async Task ImportFromCsvAsync_HeaderOnly_ReturnsFailure()
        {
            // Arrange
            var csv = "Date,Amount,Category,Notes";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("empty or invalid", result.Message);
        }

        [Fact]
        public async Task ImportFromCsvAsync_NonExistingCategory_CreatesOtherCategory()
        {
            // Arrange
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,NonExisting,Test";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Contains(categories, c => c.Name == "Other");
        }

        [Fact]
        public async Task ImportFromCsvAsync_InvalidAmount_SkipsRow()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var csv = "Date,Amount,Category,Notes\n2025-01-15,invalid,Food,Test";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_InvalidDate_SkipsRow()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var csv = "Date,Amount,Category,Notes\ninvalid-date,50.00,Food,Test";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.ImportedCount);
        }

        [Fact]
        public async Task ImportFromCsvAsync_MultipleRows_ImportsAll()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            await _categoryService.AddCategoryAsync(new Category { Name = "Transport" });
            var csv = @"Date,Amount,Category,Notes
2025-01-15,50.00,Food,Lunch
2025-01-16,20.00,Transport,Bus fare
2025-01-17,30.00,Food,Dinner";

            // Act
            var result = await _exportImportService.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.ImportedCount);
        }

        #endregion
    }
}
