using System.ComponentModel.DataAnnotations;

namespace mepa_servis.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required, StringLength(15)]
        public string Plate { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Model { get; set; } = string.Empty;

        public int Year { get; set; }
        public int CurrentKm { get; set; }

        [Required, StringLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string OwnerPhone { get; set; } = string.Empty;

        public List<ServiceRecord> ServiceRecords { get; set; } = new();
    }
}