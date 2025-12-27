using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public List<Category> SubCategories { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
