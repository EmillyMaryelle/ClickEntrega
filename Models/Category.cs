using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public List<Category> SubCategories { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}

