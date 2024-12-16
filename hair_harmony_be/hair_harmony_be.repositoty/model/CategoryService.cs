namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class CategoryService
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public User CreatedBy { get; set; }
        public User UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Status { get; set; }
    }

    public class CategoryServiceAddDTO
    {
        public string Title { get; set; }
        public string? Description { get; set; }
    }

}
