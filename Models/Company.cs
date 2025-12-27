using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Company
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        public string Type { get; set; } = string.Empty;

        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Courier> Couriers { get; set; } = new();
    }
}
