using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string? RejectionReason { get; set; }
        public string? Observations { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }

        public Guid ClientId { get; set; }
        public Client? Client { get; set; }
        
        public List<OrderItem> Items { get; set; } = new();
        public Payment? Payment { get; set; }
        public Delivery? Delivery { get; set; }

        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
        
        public Review? Review { get; set; } // One-to-One (optional)
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

