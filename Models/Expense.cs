using System;

namespace Domain.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int CategoryId { get; set; }
        public string? Notes { get; set; }
        public Category? Category { get; set; }
    }
}
