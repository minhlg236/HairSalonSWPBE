using static System.Runtime.InteropServices.JavaScript.JSType;

namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class Role
    {
        public int Id { get; set; } 
        public string Title { get; set; }
        public DateTime CreatedOn { get; set; } 
        public DateTime UpdatedOn { get; set; } 
        public bool Status { get; set; }

    }
}
