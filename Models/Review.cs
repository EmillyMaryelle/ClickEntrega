namespace ClickEntrega.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        
        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
