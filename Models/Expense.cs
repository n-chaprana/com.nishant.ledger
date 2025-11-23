using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Expense
    {
        public int Id { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public Category? Category { get; set; }
    }
}
