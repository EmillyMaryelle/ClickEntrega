using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string? RejectionReason { get; set; }
        public string? Observations { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }
        
        public List<OrderItem> Items { get; set; } = new();
        public Payment? Payment { get; set; }
        public Delivery? Delivery { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }
        
        public Review? Review { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparation,
        ReadyForDelivery,
        OutForDelivery,
        Delivered,
        Cancelled,
        Problem
    }
}
