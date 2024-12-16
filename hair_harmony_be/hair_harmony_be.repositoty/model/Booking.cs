namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class Booking
    {
        public int Id { get; set; } 
        public Service Service { get; set; } 
        public DateTime StartTime { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime UpdatedOn { get; set; } = DateTime.Now; 
        public User CreatedBy { get; set; }
        public string Status { get; set; } = "booked";
        public string? Note { get; set; }

    }


    public class BookingCreateRequest
    {
        public string StartTime { get; set; }
        public int ServiceId { get; set; }
        public string? Note { get; set; }

    }

    public class BookingUpdateRequest
    {
        public int? ServiceId { get; set; }
        public string? StartTime { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }

    }
    public class BookingCreateForServiceRequest
    {
        public int ServiceId { get; set; } 
        public List<string> StartTimes { get; set; }
        public string? Note { get; set; } 
    }

}
