using System.ComponentModel.DataAnnotations;

namespace mepa_servis.Entities
{
    public class ServiceItem
    {
        public int Id { get; set; }
        public int ServiceRecordId { get; set; }

        [Required, StringLength(150)]
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}