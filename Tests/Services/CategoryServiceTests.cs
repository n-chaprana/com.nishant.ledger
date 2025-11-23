using Domain.Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ledger.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly LedgerContext _context;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestLedgerContext(options);
            _categoryService = new CategoryService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region AddCategoryAsync Tests

        [Fact]
        public async Task AddCategoryAsync_ValidCategory_ReturnsSuccess()
        {
            // Arrange
            var category = new Category { Name = "Food" };

            // Act
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Category added successfully", result.Message);
            Assert.Single(await _context.Categories.ToListAsync());
        }

        [Fact]
        public async Task AddCategoryAsync_EmptyName_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "" };

            // Act
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category name cannot be empty", result.Message);
        }

        [Fact]
        public async Task AddCategoryAsync_WhitespaceName_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "   " };

            // Act
            var result = await _categoryService.AddCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category name cannot be empty", result.Message);
        }

        [Fact]
        public async Task AddCategoryAsync_DuplicateName_ReturnsFailure()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var duplicateCategory = new Category { Name = "Food" };

            // Act
            var result = await _categoryService.AddCategoryAsync(duplicateCategory);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        [Fact]
        public async Task AddCategoryAsync_DuplicateNameCaseInsensitive_ReturnsFailure()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var duplicateCategory = new Category { Name = "FOOD" };

            // Act
            var result = await _categoryService.AddCategoryAsync(duplicateCategory);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        #endregion

        #region GetCategoriesAsync Tests

        [Fact]
        public async Task GetCategoriesAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var categories = await _categoryService.GetCategoriesAsync();

            // Assert
            Assert.Empty(categories);
        }

        [Fact]
        public async Task GetCategoriesAsync_WithCategories_ReturnsAllCategories()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            await _categoryService.AddCategoryAsync(new Category { Name = "Transport" });
            await _categoryService.AddCategoryAsync(new Category { Name = "Entertainment" });

            // Act
            var categories = await _categoryService.GetCategoriesAsync();

            // Assert
            Assert.Equal(3, categories.Count);
        }

        #endregion

        #region GetCategoryByIdAsync Tests

        [Fact]
        public async Task GetCategoryByIdAsync_ExistingId_ReturnsCategory()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);

            // Act
            var result = await _categoryService.GetCategoryByIdAsync(category.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Food", result.Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _categoryService.GetCategoryByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateCategoryAsync Tests

        [Fact]
        public async Task UpdateCategoryAsync_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            category.Name = "Food & Dining";

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category);

            // Assert
            Assert.True(result.Success);
            var updated = await _categoryService.GetCategoryByIdAsync(category.Id);
            Assert.Equal("Food & Dining", updated?.Name);
        }

        [Fact]
        public async Task UpdateCategoryAsync_EmptyName_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            category.Name = "";

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category name cannot be empty", result.Message);
        }

        [Fact]
        public async Task UpdateCategoryAsync_NonExistingCategory_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Id = 999, Name = "NonExisting" };

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category not found", result.Message);
        }

        [Fact]
        public async Task UpdateCategoryAsync_DuplicateName_ReturnsFailure()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Food" });
            var category2 = new Category { Name = "Transport" };
            await _categoryService.AddCategoryAsync(category2);
            category2.Name = "Food";

            // Act
            var result = await _categoryService.UpdateCategoryAsync(category2);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        #endregion

        #region DeleteCategoryAsync Tests

        [Fact]
        public async Task DeleteCategoryAsync_ExistingCategoryNoExpenses_ReturnsSuccess()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);

            // Act
            var result = await _categoryService.DeleteCategoryAsync(category.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Category deleted successfully", result.Message);
        }

        [Fact]
        public async Task DeleteCategoryAsync_NonExistingCategory_ReturnsFailure()
        {
            // Act
            var result = await _categoryService.DeleteCategoryAsync(999);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Category not found", result.Message);
        }

        [Fact]
        public async Task DeleteCategoryAsync_CategoryWithExpenses_ReturnsFailure()
        {
            // Arrange
            var category = new Category { Name = "Food" };
            await _categoryService.AddCategoryAsync(category);
            
            _context.Expenses.Add(new Expense 
            { 
                Amount = 10, 
                Date = DateTime.Today, 
                CategoryId = category.Id 
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _categoryService.DeleteCategoryAsync(category.Id);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("associated expenses", result.Message);
        }

        #endregion

        #region InitializeDefaultCategoriesAsync Tests

        [Fact]
        public async Task InitializeDefaultCategoriesAsync_EmptyDatabase_CreatesDefaultCategories()
        {
            // Act
            await _categoryService.InitializeDefaultCategoriesAsync();

            // Assert
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.True(categories.Count >= 9); // At least 9 default categories
            Assert.Contains(categories, c => c.Name == "Food & Dining");
            Assert.Contains(categories, c => c.Name == "Other");
        }

        [Fact]
        public async Task InitializeDefaultCategoriesAsync_ExistingCategories_DoesNotDuplicate()
        {
            // Arrange
            await _categoryService.AddCategoryAsync(new Category { Name = "Existing" });

            // Act
            await _categoryService.InitializeDefaultCategoriesAsync();

            // Assert
            var categories = await _categoryService.GetCategoriesAsync();
            Assert.Single(categories); // Only the existing one
        }

        #endregion
    }

    // Test context that uses in-memory database
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
