namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public string Note { get; set; }
        public User Stylist { get; set; }
        public Booking Booking { get; set; }
        public User CreatedBy { get; set; }
        public User UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public bool Status { get; set; } = true;
    }

    public class CreatePaymentTransactionRequest
    {
        public string Note { get; set; }
        public int StylistId { get; set; }
        public int BookingId { get; set; }
    }

    public class UpdatePaymentTransactionRequest
    {
        public string? Note { get; set; }
        public int? StylistId { get; set; }
        public bool? Status { get; set; }
    }

    public class PaymentTransactionDTO
    {
        public int Id { get; set; }
        public string Note { get; set; }
        public User Stylist { get; set; }
        public Booking Booking { get; set; }
        public Service Service { get; set; }
        public double TotalPrice { get; set; }
    }

    public class UpdatePaymentStatusRequest
    {
        public bool? Status { get; set; }
    }

    public class AvailableTimeRequest
    {
        public List<String> ListTime { get; set; } 
        public DateTime Date { get; set; } 
    }


}
