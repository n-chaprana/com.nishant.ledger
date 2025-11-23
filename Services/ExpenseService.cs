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

        public async Task<(bool Success, string Message)> AddExpenseAsync(Expense expense)
        {
            try
            {
                // Validate amount
                if (expense.Amount <= 0)
                {
                    return (false, "Amount must be greater than zero");
                }

                // Validate date (not in the future)
                if (expense.Date.Date > DateTime.Today)
                {
                    return (false, "Cannot add expenses for future dates");
                }

                // Validate category exists
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == expense.CategoryId);
                if (!categoryExists)
                {
                    return (false, "Selected category does not exist");
                }

                // Sanitize notes
                if (!string.IsNullOrEmpty(expense.Notes))
                {
                    expense.Notes = expense.Notes.Trim();
                    if (expense.Notes.Length > 500)
                    {
                        expense.Notes = expense.Notes.Substring(0, 500);
                    }
                }

                await _context.Expenses.AddAsync(expense);
                await _context.SaveChangesAsync();
                return (true, "Expense added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding expense: {ex.Message}");
            }
        }

        public async Task<List<Expense>> GetExpensesAsync(int page = 1, int pageSize = 100)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetExpenseCountAsync()
        {
            return await _context.Expenses.CountAsync();
        }

        public async Task<List<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
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
                if (expense == null)
                {
                    return (false, "Expense not found");
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                return (true, "Expense deleted successfully");
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
                // Validate expense exists
                var existingExpense = await _context.Expenses.FindAsync(expense.Id);
                if (existingExpense == null)
                {
                    return (false, "Expense not found");
                }

                // Validate amount
                if (expense.Amount <= 0)
                {
                    return (false, "Amount must be greater than zero");
                }

                // Validate date (not in the future)
                if (expense.Date.Date > DateTime.Today)
                {
                    return (false, "Cannot set expense date to future dates");
                }

                // Validate category exists
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == expense.CategoryId);
                if (!categoryExists)
                {
                    return (false, "Selected category does not exist");
                }

                // Sanitize notes
                if (!string.IsNullOrEmpty(expense.Notes))
                {
                    expense.Notes = expense.Notes.Trim();
                    if (expense.Notes.Length > 500)
                    {
                        expense.Notes = expense.Notes.Substring(0, 500);
                    }
                }

                // Update the existing entity
                existingExpense.Amount = expense.Amount;
                existingExpense.Date = expense.Date;
                existingExpense.CategoryId = expense.CategoryId;
                existingExpense.Notes = expense.Notes;

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
                .GroupBy(e => new { e.CategoryId, CategoryName = e.Category!.Name })
                .Select(g => new CategorySummary
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
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
                .ThenByDescending(e => e.Id)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> DeleteAllExpensesAsync()
        {
            try
            {
                var expenses = await _context.Expenses.ToListAsync();
                if (!expenses.Any())
                {
                    return (true, "No expenses to delete");
                }

                _context.Expenses.RemoveRange(expenses);
                await _context.SaveChangesAsync();
                return (true, $"Successfully deleted {expenses.Count} expenses");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting expenses: {ex.Message}");
            }
        }
    }
}
