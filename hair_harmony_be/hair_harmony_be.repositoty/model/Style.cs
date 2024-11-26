namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class Style
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public double? Discount { get; set; }
        public double? Commission { get; set; }
        public User CreatedBy { get; set; }
        public User UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Status { get; set; }

    }

    public class StyleCreateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public double? Discount { get; set; }
        public double? Commission { get; set; }

    }

    public class StyleWithImages
    {
        public Style StyleEnity { get; set; }
        // Một Style có thể có nhiều Image
        public ICollection<Image> Images { get; set; }
    }
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
