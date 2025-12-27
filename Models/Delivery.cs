using System.ComponentModel.DataAnnotations;

namespace ClickEntrega.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public int? CourierId { get; set; }
        public Courier? Courier { get; set; }
        
        public string Address { get; set; } = string.Empty;
        public string TrackingCode { get; set; } = string.Empty;
        public string TrackingLocation { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DeliveryStatus Status { get; set; }
    }

    public enum DeliveryStatus
    {
        Pending,
        PickedUp,
        OnTheWay,
        Delivered,
        Failed
    }
}
