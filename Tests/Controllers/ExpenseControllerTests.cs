using Domain.Controllers;
using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Controllers
{
    public class ExpenseControllerTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;
        private readonly ExpenseController _controller;

        public ExpenseControllerTests()
        {
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestLedgerContext(options);
            _expenseService = new ExpenseService(_context);
            _categoryService = new CategoryService(_context);
            _exportImportService = new ExportImportService(_expenseService, _categoryService);
            _controller = new ExpenseController(_expenseService, _categoryService, _exportImportService, _context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region InitializeDatabaseAsync Tests

        [Fact]
        public async Task InitializeDatabaseAsync_CreatesDefaultCategories()
        {
            // Act
            await _controller.InitializeDatabaseAsync();

            // Assert
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.True(categories.Count >= 9);
            Assert.Contains(categories, c => c.Name == "Food & Dining");
            Assert.Contains(categories, c => c.Name == "Transportation");
            Assert.Contains(categories, c => c.Name == "Other");
        }

        [Fact]
        public async Task InitializeDatabaseAsync_DoesNotDuplicateCategories()
        {
            // Act
            await _controller.InitializeDatabaseAsync();
            await _controller.InitializeDatabaseAsync(); // Call twice

            // Assert
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Equal(9, categories.Count); // Should still be 9
        }

        #endregion

        #region GetAllExpensesAsync Tests

        [Fact]
        public async Task GetAllExpensesAsync_ReturnsAllExpenses()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var expenses = await _controller.GetAllExpensesAsync();

            // Assert
            Assert.Equal(2, expenses.Count);
        }

        #endregion

        #region GetAllCategoriesAsync Tests

        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsAllCategories()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();

            // Act
            var categories = await _controller.GetAllCategoriesAsync();

            // Assert
            Assert.Equal(9, categories.Count);
        }

        #endregion

        #region AddExpenseAsync Tests

        [Fact]
        public async Task AddExpenseAsync_ValidExpense_ReturnsSuccess()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id };

            // Act
            var result = await _controller.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region GetCategorySummariesAsync Tests

        [Fact]
        public async Task GetCategorySummariesAsync_ReturnsCorrectSummaries()
        {
            // Arrange - Use fresh categories without sample data
            var foodCategory = new Category { Name = "Food" };
            var transportCategory = new Category { Name = "Transport" };
            await _categoryService.AddCategoryAsync(foodCategory);
            await _categoryService.AddCategoryAsync(transportCategory);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = DateTime.Today, CategoryId = foodCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = DateTime.Today, CategoryId = foodCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = transportCategory.Id });

            // Act
            var summaries = await _controller.GetCategorySummariesAsync(DateTime.Today.AddDays(-1), DateTime.Today);

            // Assert
            Assert.Equal(2, summaries.Count);
            var foodSummary = summaries.First(s => s.CategoryName == "Food");
            Assert.Equal(150, foodSummary.TotalAmount);
            Assert.Equal(2, foodSummary.ExpenseCount);
        }

        #endregion

        #region ExportToCsvAsync Tests

        [Fact]
        public async Task ExportToCsvAsync_WithExpenses_ReturnsValidCsv()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            await _expenseService.AddExpenseAsync(new Expense 
            { 
                Amount = 50, 
                Date = new DateTime(2025, 1, 15), 
                CategoryId = category.Id,
                Notes = "Test expense"
            });

            // Act
            var csv = await _controller.ExportToCsvAsync();

            // Assert
            Assert.Contains("Date,Amount,Category,Notes", csv);
            Assert.Contains("2025-01-15", csv);
            Assert.Contains("50", csv);
        }

        #endregion

        #region ImportFromCsvAsync Tests

        [Fact]
        public async Task ImportFromCsvAsync_ValidCsv_ImportsSuccessfully()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var csv = "Date,Amount,Category,Notes\n2025-01-15,50.00,Food & Dining,Lunch";

            // Act
            var result = await _controller.ImportFromCsvAsync(csv);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.ImportedCount);
        }

        #endregion

        #region ClearAllDataAsync Tests

        [Fact]
        public async Task ClearAllDataAsync_RemovesAllExpensesAndCategories()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var result = await _controller.ClearAllDataAsync();

            // Assert
            Assert.True(result.Success);
            var expenses = await _expenseService.GetExpensesAsync();
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Empty(expenses);
            Assert.Equal(9, categories.Count); // Should have default categories
        }

        [Fact]
        public async Task ClearAllDataAsync_ReinitializesDefaultCategories()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();

            // Act
            var result = await _controller.ClearAllDataAsync();

            // Assert
            Assert.True(result.Success);
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Contains(categories, c => c.Name == "Food & Dining");
            Assert.Contains(categories, c => c.Name == "Other");
        }

        #endregion

        #region AddCategoryAsync Tests

        [Fact]
        public async Task AddCategoryAsync_ValidCategory_ReturnsSuccess()
        {
            // Arrange
            var category = new Category { Name = "Custom Category" };

            // Act
            var result = await _controller.AddCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region UpdateCategoryAsync Tests

        [Fact]
        public async Task UpdateCategoryAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            category.Name = "Updated Name";

            // Act
            var result = await _controller.UpdateCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region DeleteCategoryAsync Tests

        [Fact]
        public async Task DeleteCategoryAsync_CategoryWithoutExpenses_ReturnsSuccess()
        {
            // Arrange
            var category = new Category { Name = "Temporary" };
            await _categoryService.AddCategoryAsync(category);

            // Act
            var result = await _controller.DeleteCategoryAsync(category.Id);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DeleteCategoryAsync_CategoryWithExpenses_ReturnsFailure()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var result = await _controller.DeleteCategoryAsync(category.Id);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("associated expenses", result.Message);
        }

        #endregion

        #region GetExpenseByIdAsync Tests

        [Fact]
        public async Task GetExpenseByIdAsync_ExistingId_ReturnsExpense()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Act
            var result = await _controller.GetExpenseByIdAsync(expense.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Amount);
        }

        #endregion

        #region GetCategoryByIdAsync Tests

        [Fact]
        public async Task GetCategoryByIdAsync_ExistingId_ReturnsCategory()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();

            // Act
            var result = await _controller.GetCategoryByIdAsync(category.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(category.Name, result.Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _controller.GetCategoryByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetExpenseCountAsync Tests

        [Fact]
        public async Task GetExpenseCountAsync_WithExpenses_ReturnsCorrectCount()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var count = await _controller.GetExpenseCountAsync();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetExpenseCountAsync_EmptyDatabase_ReturnsZero()
        {
            // Act
            var count = await _controller.GetExpenseCountAsync();

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region GetExpensesByDateRangeAsync Tests

        [Fact]
        public async Task GetExpensesByDateRangeAsync_ReturnsExpensesInRange()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = new DateTime(2025, 1, 15), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = new DateTime(2025, 2, 15), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = new DateTime(2025, 1, 20), CategoryId = category.Id });

            // Act
            var expenses = await _controller.GetExpensesByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Equal(2, expenses.Count);
            Assert.All(expenses, e => Assert.True(e.Date >= startDate && e.Date <= endDate));
        }

        #endregion

        #region GetTotalSpentAsync Tests

        [Fact]
        public async Task GetTotalSpentAsync_WithExpenses_ReturnsCorrectTotal()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            await _expenseService.AddExpenseAsync(new Expense { Amount = 100.50m, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 200.75m, Date = DateTime.Today, CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50.25m, Date = DateTime.Today, CategoryId = category.Id });

            // Act
            var total = await _controller.GetTotalSpentAsync();

            // Assert
            Assert.Equal(351.50m, total);
        }

        [Fact]
        public async Task GetTotalSpentAsync_EmptyDatabase_ReturnsZero()
        {
            // Act
            var total = await _controller.GetTotalSpentAsync();

            // Assert
            Assert.Equal(0, total);
        }

        #endregion

        #region GetTotalSpentByDateRangeAsync Tests

        [Fact]
        public async Task GetTotalSpentByDateRangeAsync_ReturnsCorrectTotal()
        {
            // Arrange
            var category = new Category { Name = "Test" };
            await _categoryService.AddCategoryAsync(category);
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 100, Date = new DateTime(2025, 1, 15), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 200, Date = new DateTime(2025, 2, 15), CategoryId = category.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = new DateTime(2025, 1, 20), CategoryId = category.Id });

            // Act
            var total = await _controller.GetTotalSpentByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.Equal(150, total);
        }

        #endregion

        #region UpdateExpenseAsync Tests

        [Fact]
        public async Task UpdateExpenseAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);
            expense.Amount = 75;

            // Act
            var result = await _controller.UpdateExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region DeleteExpenseAsync Tests

        [Fact]
        public async Task DeleteExpenseAsync_ExistingExpense_ReturnsSuccess()
        {
            // Arrange
            await _controller.InitializeDatabaseAsync();
            var category = (await _categoryService.GetCategoriesAsync()).First();
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Act
            var result = await _controller.DeleteExpenseAsync(expense.Id);

            // Assert
            Assert.True(result.Success);
        }

        #endregion
    }

    public class TestLedgerContext : LedgerContext
    {
        private readonly DbContextOptions<LedgerContext> _options;

        public TestLedgerContext(DbContextOptions<LedgerContext> options) : base()
        {
            _options = options;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
        }
    }
}
