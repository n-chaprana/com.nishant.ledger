using Domain.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Services
{
    public class ExpenseService
    {
        private readonly LedgerContext _context;

        public ExpenseService(LedgerContext context)
        {
            _context = context;
        }

        public async Task AddExpenseAsync(Expense expense)
        {
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Expense>> GetExpensesAsync()
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<Expense?> GetExpenseAsync(int id)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<(bool Success, string Message)> DeleteExpenseAsync(int id)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(id);
                if (expense != null)
                {
                    _context.Expenses.Remove(expense);
                    await _context.SaveChangesAsync();
                    return (true, "Expense deleted successfully");
                }
                return (false, "Expense not found");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting expense: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateExpenseAsync(Expense expense)
        {
            try
            {
                _context.Entry(expense).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return (true, "Expense updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating expense: {ex.Message}");
            }
        }

        public async Task<decimal> GetTotalSpentAsync()
        {
            return await _context.Expenses.SumAsync(e => e.Amount);
        }

        public async Task<decimal> GetTotalSpentByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .SumAsync(e => e.Amount);
        }

        public async Task<List<CategorySummary>> GetCategorySummariesAsync(DateTime startDate, DateTime endDate)
        {
            var summaries = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .GroupBy(e => new { e.CategoryId, e.Category.Name })
                .Select(g => new CategorySummary
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    TotalAmount = g.Sum(e => e.Amount),
                    ExpenseCount = g.Count()
                })
                .ToListAsync();

            // Order on client side since SQLite doesn't support decimal ordering
            return summaries.OrderByDescending(s => s.TotalAmount).ToList();
        }

        public async Task<List<Expense>> GetExpensesByCategoryAsync(int categoryId)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.CategoryId == categoryId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task DeleteAllExpensesAsync()
        {
            _context.Expenses.RemoveRange(_context.Expenses);
            await _context.SaveChangesAsync();
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
