using Domain.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Services
{
    public class CategoryService
    {
        private readonly LedgerContext _context;

        public CategoryService(LedgerContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<(bool Success, string Message)> AddCategoryAsync(Category category)
        {
            try
            {
                // Validate category name
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    return (false, "Category name cannot be empty");
                }

                // Check for duplicate category names
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                
                if (existingCategory != null)
                {
                    return (false, $"Category '{category.Name}' already exists");
                }

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return (true, "Category added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding category: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateCategoryAsync(Category category)
        {
            try
            {
                // Validate category name
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    return (false, "Category name cannot be empty");
                }

                // Check if category exists
                var existingCategory = await _context.Categories.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return (false, "Category not found");
                }

                // Check for duplicate names (excluding current category)
                var duplicateCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
                
                if (duplicateCategory != null)
                {
                    return (false, $"Category '{category.Name}' already exists");
                }

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return (true, "Category updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating category: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Expenses)
                    .FirstOrDefaultAsync(c => c.Id == id);
                
                if (category == null)
                {
                    return (false, "Category not found");
                }

                // Check if category has associated expenses
                if (category.Expenses?.Any() == true)
                {
                    return (false, $"Cannot delete category '{category.Name}' with {category.Expenses.Count} associated expenses. Please reassign or delete the expenses first.");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return (true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting category: {ex.Message}");
            }
        }

        public async Task InitializeDefaultCategoriesAsync()
        {
            var existingCategories = await GetCategoriesAsync();
            if (existingCategories.Any()) return;

            var defaultCategories = new List<Category>
            {
                new Category { Name = "Food & Dining" },
                new Category { Name = "Transportation" },
                new Category { Name = "Entertainment" },
                new Category { Name = "Bills & Utilities" },
                new Category { Name = "Shopping" },
                new Category { Name = "Healthcare" },
                new Category { Name = "Education" },
                new Category { Name = "Travel" },
                new Category { Name = "Other" }
            };

            foreach (var category in defaultCategories)
            {
                await AddCategoryAsync(category);
            }
        }

        public async Task<(bool Success, string Message)> DeleteAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories.Include(c => c.Expenses).ToListAsync();
                
                // Check if any category has expenses
                var categoriesWithExpenses = categories.Where(c => c.Expenses?.Any() == true).ToList();
                if (categoriesWithExpenses.Any())
                {
                    return (false, $"Cannot delete all categories. {categoriesWithExpenses.Count} categories have associated expenses.");
                }

                _context.Categories.RemoveRange(categories);
                await _context.SaveChangesAsync();
                return (true, "All categories deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting categories: {ex.Message}");
            }
        }
    }
}
