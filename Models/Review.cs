namespace ClickEntrega.Models
{
    public class Review
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public Client? Client { get; set; }
        
        public Guid? OrderId { get; set; } // Linked Order
        
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
    }
}

