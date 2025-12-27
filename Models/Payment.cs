namespace ClickEntrega.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        Pix,
        CashOnDelivery,
        CardOnDelivery
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}
