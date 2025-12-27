using System;
using ClickEntrega.Models;

namespace ClickEntrega.Models.DTOs
{
    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
    }
}
