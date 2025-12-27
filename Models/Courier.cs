using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Courier
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        
        public List<Delivery> Deliveries { get; set; } = new();

        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
