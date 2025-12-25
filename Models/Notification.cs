using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Client? Client { get; set; }
        
        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

