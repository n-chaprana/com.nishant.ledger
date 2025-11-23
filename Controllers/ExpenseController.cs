using Domain.Data;
using Domain.Models;
using Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Controllers
{
    public class ExpenseController
    {
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;
        private readonly ExportImportService _exportImportService;
        private readonly LedgerContext _context;

        public ExpenseController(ExpenseService expenseService, CategoryService categoryService, 
            ExportImportService exportImportService, LedgerContext context)
        {
            _expenseService = expenseService;
            _categoryService = categoryService;
            _exportImportService = exportImportService;
            _context = context;
        }

        // Expense Management
        public async Task<List<Expense>> GetAllExpensesAsync(int page = 1, int pageSize = 100)
        {
            return await _expenseService.GetExpensesAsync(page, pageSize);
        }

        public async Task<int> GetExpenseCountAsync()
        {
            return await _expenseService.GetExpenseCountAsync();
        }

        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            return await _expenseService.GetExpenseAsync(id);
        }

        public async Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _expenseService.GetExpensesByDateRangeAsync(startDate, endDate);
        }

        public async Task<(bool Success, string Message)> AddExpenseAsync(Expense expense)
        {
            return await _expenseService.AddExpenseAsync(expense);
        }

        public async Task<(bool Success, string Message)> UpdateExpenseAsync(Expense expense)
        {
            return await _expenseService.UpdateExpenseAsync(expense);
        }

        public async Task<(bool Success, string Message)> DeleteExpenseAsync(int id)
        {
            return await _expenseService.DeleteExpenseAsync(id);
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

        public async Task<(bool Success, string Message)> AddCategoryAsync(Category category)
        {
            return await _categoryService.AddCategoryAsync(category);
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(Category category)
        {
            return await _categoryService.UpdateCategoryAsync(category);
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
        {
            return await _categoryService.DeleteCategoryAsync(id);
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
            var initService = new DatabaseInitializationService(_context, _categoryService);
            await initService.InitializeDatabaseAsync();
        }

        public async Task<(bool Success, string Message)> ClearAllDataAsync()
        {
            try
            {
                // Delete all expenses first
                var expenseResult = await _expenseService.DeleteAllExpensesAsync();
                if (!expenseResult.Success)
                {
                    return (false, $"Failed to delete expenses: {expenseResult.Message}");
                }

                // Delete all categories
                var categoryResult = await _categoryService.DeleteAllCategoriesAsync();
                if (!categoryResult.Success)
                {
                    return (false, $"Failed to delete categories: {categoryResult.Message}");
                }

                // Reinitialize default categories
                await _categoryService.InitializeDefaultCategoriesAsync();

                return (true, "All data cleared and default categories restored successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error clearing data: {ex.Message}");
            }
        }
    }
}
