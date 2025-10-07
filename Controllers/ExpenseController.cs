using Domain.Models;
using Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Controllers
{
    public class ExpenseController
    {
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;

        public ExpenseController(ExpenseService expenseService, CategoryService categoryService, ExportImportService exportImportService)
        {
            _expenseService = expenseService;
            _categoryService = categoryService;
            _exportImportService = exportImportService;
        }

        // Expense Management
        public async Task<List<Expense>> GetAllExpensesAsync()
        {
            return await _expenseService.GetExpensesAsync();
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            return await _expenseService.GetExpenseAsync(id);
        }

        public async Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _expenseService.GetExpensesByDateRangeAsync(startDate, endDate);
        }

        public async Task AddExpenseAsync(Expense expense)
        {
            await _expenseService.AddExpenseAsync(expense);
        }

        public async Task<bool> UpdateExpenseAsync(Expense expense)
        {
            var result = await _expenseService.UpdateExpenseAsync(expense);
            return result.Success;
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            var result = await _expenseService.DeleteExpenseAsync(id);
            return result.Success;
        }

        public async Task<decimal> GetTotalSpentAsync()
        {
            return await _expenseService.GetTotalSpentAsync();
        }

        public async Task<decimal> GetTotalSpentByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _expenseService.GetTotalSpentByDateRangeAsync(startDate, endDate);
        }

        // Category Management
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _categoryService.GetCategoriesAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryService.GetCategoryByIdAsync(id);
        }

        public async Task AddCategoryAsync(Category category)
        {
            await _categoryService.AddCategoryAsync(category);
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            try
            {
                await _categoryService.UpdateCategoryAsync(category);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Analytics
        public async Task<List<CategorySummary>> GetCategorySummariesAsync(DateTime startDate, DateTime endDate)
        {
            return await _expenseService.GetCategorySummariesAsync(startDate, endDate);
        }

        // Data Operations
        public async Task<string> ExportToCsvAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _exportImportService.ExportToCsvAsync(startDate, endDate);
        }

        public async Task<(bool Success, string Message, int ImportedCount)> ImportFromCsvAsync(string csvContent)
        {
            return await _exportImportService.ImportFromCsvAsync(csvContent);
        }

        public async Task InitializeDatabaseAsync()
        {
            var context = new Data.LedgerContext();
            var initService = new DatabaseInitializationService(context, _categoryService);
            await initService.InitializeDatabaseAsync();
        }

        public async Task ClearAllDataAsync()
        {
            await _expenseService.DeleteAllExpensesAsync();
            await _categoryService.InitializeDefaultCategoriesAsync();
        }
    }

    public class CategorySummary
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
        public int ExpenseCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
