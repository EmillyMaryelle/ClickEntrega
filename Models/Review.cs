namespace ClickEntrega.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        
        public int? OrderId { get; set; } // Linked Order
        public Order? Order { get; set; } // Navigation property

        
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
