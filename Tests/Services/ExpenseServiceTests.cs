using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    public class ExpenseServiceTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private Category _testCategory = null!;

        public ExpenseServiceTests()
        {
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestLedgerContext(options);
            _expenseService = new ExpenseService(_context);
            _categoryService = new CategoryService(_context);
            
            // Setup test category
            SetupTestCategory().Wait();
        }

        private async Task SetupTestCategory()
        {
            _testCategory = new Category { Name = "Test Category" };
            await _categoryService.AddCategoryAsync(_testCategory);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region AddExpenseAsync Tests

        [Fact]
        public async Task AddExpenseAsync_ValidExpense_ReturnsSuccess()
        {
            // Arrange
            var expense = new Expense
            {
                Amount = 50.00m,
                Date = DateTime.Today,
                CategoryId = _testCategory.Id,
                Notes = "Test expense"
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Expense added successfully", result.Message);
        }

        [Fact]
        public async Task AddExpenseAsync_ZeroAmount_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense
            {
                Amount = 0,
                Date = DateTime.Today,
                CategoryId = _testCategory.Id
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than zero", result.Message);
        }

        [Fact]
        public async Task AddExpenseAsync_NegativeAmount_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense
            {
                Amount = -10.00m,
                Date = DateTime.Today,
                CategoryId = _testCategory.Id
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than zero", result.Message);
        }

        [Fact]
        public async Task AddExpenseAsync_FutureDate_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense
            {
                Amount = 50.00m,
                Date = DateTime.Today.AddDays(1),
                CategoryId = _testCategory.Id
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cannot add expenses for future dates", result.Message);
        }

        [Fact]
        public async Task AddExpenseAsync_InvalidCategory_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense
            {
                Amount = 50.00m,
                Date = DateTime.Today,
                CategoryId = 999
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Selected category does not exist", result.Message);
        }

        [Fact]
        public async Task AddExpenseAsync_LongNotes_TruncatesTo500Characters()
        {
            // Arrange
            var longNotes = new string('a', 600);
            var expense = new Expense
            {
                Amount = 50.00m,
                Date = DateTime.Today,
                CategoryId = _testCategory.Id,
                Notes = longNotes
            };

            // Act
            var result = await _expenseService.AddExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            var savedExpense = await _expenseService.GetExpenseAsync(expense.Id);
            Assert.Equal(500, savedExpense?.Notes?.Length);
        }

        #endregion

        #region GetExpensesAsync Tests

        [Fact]
        public async Task GetExpensesAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var expenses = await _expenseService.GetExpensesAsync();

            // Assert
            Assert.Empty(expenses);
        }

        [Fact]
        public async Task GetExpensesAsync_WithExpenses_ReturnsAllExpenses()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = _testCategory.Id });

            // Act
            var expenses = await _expenseService.GetExpensesAsync();

            // Assert
            Assert.Equal(3, expenses.Count);
        }

        [Fact]
        public async Task GetExpensesAsync_OrderedByDateDescending()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today.AddDays(-2), CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today.AddDays(-1), CategoryId = _testCategory.Id });

            // Act
            var expenses = await _expenseService.GetExpensesAsync();

            // Assert
            Assert.Equal(20, expenses[0].Amount); // Today
            Assert.Equal(30, expenses[1].Amount); // Yesterday
            Assert.Equal(10, expenses[2].Amount); // 2 days ago
        }

        [Fact]
        public async Task GetExpensesAsync_IncludesCategory()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = _testCategory.Id });

            // Act
            var expenses = await _expenseService.GetExpensesAsync();

            // Assert
            Assert.NotNull(expenses[0].Category);
            Assert.Equal("Test Category", expenses[0].Category?.Name);
        }

        #endregion

        #region GetExpensesByDateRangeAsync Tests

        [Fact]
        public async Task GetExpensesByDateRangeAsync_ReturnsExpensesInRange()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today.AddDays(-5), CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today.AddDays(-2), CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = _testCategory.Id });

            // Act
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(
                DateTime.Today.AddDays(-3), 
                DateTime.Today);

            // Assert
            Assert.Equal(2, expenses.Count);
        }

        #endregion

        #region GetExpenseAsync Tests

        [Fact]
        public async Task GetExpenseAsync_ExistingId_ReturnsExpense()
        {
            // Arrange
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = _testCategory.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Act
            var result = await _expenseService.GetExpenseAsync(expense.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Amount);
        }

        [Fact]
        public async Task GetExpenseAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _expenseService.GetExpenseAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateExpenseAsync Tests

        [Fact]
        public async Task UpdateExpenseAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = _testCategory.Id };
            await _expenseService.AddExpenseAsync(expense);
            expense.Amount = 75;

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.True(result.Success);
            var updated = await _expenseService.GetExpenseAsync(expense.Id);
            Assert.Equal(75, updated?.Amount);
        }

        [Fact]
        public async Task UpdateExpenseAsync_NonExistingExpense_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense { Id = 999, Amount = 50, Date = DateTime.Today, CategoryId = _testCategory.Id };

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Expense not found", result.Message);
        }

        [Fact]
        public async Task UpdateExpenseAsync_ZeroAmount_ReturnsFailure()
        {
            // Arrange
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = _testCategory.Id };
            await _expenseService.AddExpenseAsync(expense);
            expense.Amount = 0;

            // Act
            var result = await _expenseService.UpdateExpenseAsync(expense);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Amount must be greater than zero", result.Message);
        }

        #endregion

        #region DeleteExpenseAsync Tests

        [Fact]
        public async Task DeleteExpenseAsync_ExistingExpense_ReturnsSuccess()
        {
            // Arrange
            var expense = new Expense { Amount = 50, Date = DateTime.Today, CategoryId = _testCategory.Id };
            await _expenseService.AddExpenseAsync(expense);

            // Act
            var result = await _expenseService.DeleteExpenseAsync(expense.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Expense deleted successfully", result.Message);
        }

        [Fact]
        public async Task DeleteExpenseAsync_NonExistingExpense_ReturnsFailure()
        {
            // Act
            var result = await _expenseService.DeleteExpenseAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Expense not found", result.Message);
        }

        #endregion

        #region GetTotalSpentAsync Tests

        [Fact]
        public async Task GetTotalSpentAsync_EmptyDatabase_ReturnsZero()
        {
            // Act
            var total = await _expenseService.GetTotalSpentAsync();

            // Assert
            Assert.Equal(0, total);
        }

        [Fact]
        public async Task GetTotalSpentAsync_WithExpenses_ReturnsCorrectTotal()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 30, Date = DateTime.Today, CategoryId = _testCategory.Id });

            // Act
            var total = await _expenseService.GetTotalSpentAsync();

            // Assert
            Assert.Equal(60, total);
        }

        #endregion

        #region GetCategorySummariesAsync Tests

        [Fact]
        public async Task GetCategorySummariesAsync_ReturnsCorrectSummaries()
        {
            // Arrange
            var category2 = new Category { Name = "Category 2" };
            await _categoryService.AddCategoryAsync(category2);

            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 50, Date = DateTime.Today, CategoryId = category2.Id });

            // Act
            var summaries = await _expenseService.GetCategorySummariesAsync(DateTime.Today.AddDays(-1), DateTime.Today);

            // Assert
            Assert.Equal(2, summaries.Count);
            var testCategorySummary = summaries.First(s => s.CategoryName == "Test Category");
            Assert.Equal(30, testCategorySummary.TotalAmount);
            Assert.Equal(2, testCategorySummary.ExpenseCount);
        }

        #endregion

        #region DeleteAllExpensesAsync Tests

        [Fact]
        public async Task DeleteAllExpensesAsync_WithExpenses_DeletesAll()
        {
            // Arrange
            await _expenseService.AddExpenseAsync(new Expense { Amount = 10, Date = DateTime.Today, CategoryId = _testCategory.Id });
            await _expenseService.AddExpenseAsync(new Expense { Amount = 20, Date = DateTime.Today, CategoryId = _testCategory.Id });

            // Act
            var result = await _expenseService.DeleteAllExpensesAsync();

            // Assert
            Assert.True(result.Success);
            var expenses = await _expenseService.GetExpensesAsync();
            Assert.Empty(expenses);
        }

        [Fact]
        public async Task DeleteAllExpensesAsync_EmptyDatabase_ReturnsSuccess()
        {
            // Act
            var result = await _expenseService.DeleteAllExpensesAsync();

            // Assert
            Assert.True(result.Success);
        }

        #endregion
    }
}
