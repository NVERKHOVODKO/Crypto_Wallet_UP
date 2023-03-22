namespace UP.Models
{
    public class Withdrawal
    {
        public int Id { get; set; }
        public String Date { get; set; }
        public double Quantity { get; set; }
        public double Comission { get; set; }
        public int UserId { get; set; }
    }
}
