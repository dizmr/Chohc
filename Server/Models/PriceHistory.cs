using System;

namespace Server.Models
{
    public class PriceHistory
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public DateTime CheckedAt { get; set; }
        public DateTime Timestamp { get; set; }
    }
}