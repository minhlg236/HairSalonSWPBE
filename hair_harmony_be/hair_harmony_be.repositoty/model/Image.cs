﻿namespace hair_harmony_be.hair_harmony_be.repositoty.model
{
    public class Image
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public Service ServiceEntity { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool Status { get; set; }
    }

    public class ImageAddDTO
    {
        public string Url { get; set; }
        public int serviceId { get; set; }
    }
   

}
