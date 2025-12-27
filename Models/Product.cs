using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
