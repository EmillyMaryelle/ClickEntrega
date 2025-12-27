using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models.DTOs
{
    public class CreateCourierDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        [Required]
        public int CompanyId { get; set; }
    }
}
