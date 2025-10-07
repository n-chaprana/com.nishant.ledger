using Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
    public class ExportImportService
    {
        private readonly ExpenseService _expenseService;
        private readonly CategoryService _categoryService;

        public ExportImportService(ExpenseService expenseService, CategoryService categoryService)
        {
            _expenseService = expenseService;
            _categoryService = categoryService;
        }

        public async Task<string> ExportToCsvAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.MinValue;
            var end = endDate ?? DateTime.MaxValue;

            var expenses = await _expenseService.GetExpensesByDateRangeAsync(start, end);

            var csv = new StringBuilder();
            csv.AppendLine("Date,Amount,Category,Notes");

            foreach (var expense in expenses)
            {
                csv.AppendLine($"{expense.Date:yyyy-MM-dd},{expense.Amount},{expense.Category?.Name ?? "Unknown"},{EscapeCsvField(expense.Notes ?? "")}");
            }

            return csv.ToString();
        }

        public async Task<(bool Success, string Message, int ImportedCount)> ImportFromCsvAsync(string csvContent)
        {
            try
            {
                var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2) return (false, "CSV file is empty or invalid", 0);

                var importedCount = 0;
                var categories = await _categoryService.GetCategoriesAsync();
                var categoryDict = categories.ToDictionary(c => c.Name.ToLower(), c => c.Id);

                for (int i = 1; i < lines.Length; i++) // Skip header
                {
                    var parts = ParseCsvLine(lines[i]);
                    if (parts.Length < 3) continue;

                    if (DateTime.TryParse(parts[0], out var date) &&
                        decimal.TryParse(parts[1], out var amount) &&
                        !string.IsNullOrWhiteSpace(parts[2]))
                    {
                        var categoryName = parts[2].Trim();
                        var notes = parts.Length > 3 ? parts[3] : "";

                        var categoryId = categoryDict.ContainsKey(categoryName.ToLower())
                            ? categoryDict[categoryName.ToLower()]
                            : categories.FirstOrDefault(c => string.Equals(c.Name, "Other", StringComparison.OrdinalIgnoreCase))?.Id ?? 1;

                        var expense = new Expense
                        {
                            Date = date,
                            Amount = amount,
                            CategoryId = categoryId,
                            Notes = notes
                        };

                        await _expenseService.AddExpenseAsync(expense);
                        importedCount++;
                    }
                }

                return (true, $"Successfully imported {importedCount} expenses", importedCount);
            }
            catch (Exception ex)
            {
                return (false, $"Error importing CSV: {ex.Message}", 0);
            }
        }

        public async Task<byte[]> ExportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // For now, return CSV as bytes since we don't have Excel libraries
            // In a real implementation, you would use a library like EPPlus or NPOI
            var csvContent = await ExportToCsvAsync(startDate, endDate);
            return Encoding.UTF8.GetBytes(csvContent);
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private string[] ParseCsvLine(string line)
        {
            var parts = new List<string>();
            var current = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            parts.Add(current);
            return parts.ToArray();
        }
    }
}
