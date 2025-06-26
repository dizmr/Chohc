namespace Server.Services
{
    public class PriceAnalyzer
    {
        public decimal GetPercentageChange(decimal oldPrice, decimal newPrice)
        {
            if (oldPrice == 0) return 0;
            return ((newPrice - oldPrice) / oldPrice) * 100;
        }
    }
}