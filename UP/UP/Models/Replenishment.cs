namespace UP.Models
{
    public class Replenishment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double Quantity { get; set; }
        public double Commission { get; set; }
        public int UserId { get; set; }
    }
}
