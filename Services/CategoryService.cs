using Domain.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
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

        public async Task AddCategoryAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
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
    }
}
