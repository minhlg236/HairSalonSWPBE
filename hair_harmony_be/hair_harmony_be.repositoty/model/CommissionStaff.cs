namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class CommissionStaff
    {
        public int Id { get; set; }
        public string Note { get; set; }
        public int StaffId { get; set; }
        public int StyleId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public User Staff { get; set; }
        public Style Style { get; set; }
        public bool Status { get; set; }

    }
}
