using Domain.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Services
{
    public class DatabaseInitializationService
    {
        private readonly LedgerContext _context;
        private readonly CategoryService _categoryService;

        public DatabaseInitializationService(LedgerContext context, CategoryService categoryService)
        {
            _context = context;
            _categoryService = categoryService;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Initialize default categories if they don't exist
                await _categoryService.InitializeDefaultCategoriesAsync();

                // Perform any additional database setup if needed
                await SeedSampleDataAsync();
            }
            catch (Exception ex)
            {
                // Log the error in a real application
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private async Task SeedSampleDataAsync()
        {
            // Check if we already have sample data
            var expenseCount = await _context.Expenses.CountAsync();
            if (expenseCount > 0) return;

            // Add some sample expenses for demonstration
            var categories = await _context.Categories.ToListAsync();

            if (categories.Any())
            {
                var sampleExpenses = new[]
                {
                    new Expense { Amount = 15.50m, Date = DateTime.Today.AddDays(-1), CategoryId = categories.First().Id, Notes = "Lunch at cafe" },
                    new Expense { Amount = 45.00m, Date = DateTime.Today.AddDays(-2), CategoryId = categories.FirstOrDefault(c => c.Name?.Contains("Transportation") == true)?.Id ?? categories.First().Id, Notes = "Gas fill-up" },
                    new Expense { Amount = 12.99m, Date = DateTime.Today.AddDays(-3), CategoryId = categories.FirstOrDefault(c => c.Name?.Contains("Entertainment") == true)?.Id ?? categories.First().Id, Notes = "Movie ticket" }
                };

                await _context.Expenses.AddRangeAsync(sampleExpenses);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsDatabaseInitializedAsync()
        {
            try
            {
                // Check if database exists and has tables
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task ResetDatabaseAsync()
        {
            try
            {
                // Delete all data
                await _context.Expenses.ExecuteDeleteAsync();
                await _context.Categories.ExecuteDeleteAsync();

                // Reinitialize
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to reset database: {ex.Message}", ex);
            }
        }
    }
}
