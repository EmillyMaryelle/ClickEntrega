using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}

