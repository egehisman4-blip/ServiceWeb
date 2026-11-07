using System.ComponentModel.DataAnnotations;

namespace mepa_servis.Entities
{
    public enum ServiceStatus
    {
        Accepted = 1,      // Kabul Edildi
        Diagnosing = 2,    // Teşhis Yapılıyor
        WaitingParts = 3,  // Parça Bekliyor
        InProcess = 4,     // İşlemde
        TestDrive = 5,     // Test Sürüşünde
        Ready = 6          // Hazır / Teslim Edildi
    }

    public class ServiceRecord
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }

        public DateTime EntryDate { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? ExitDate { get; set; }

        public ServiceStatus Status { get; set; } = ServiceStatus.Accepted;
        public decimal TotalCost { get; set; }

        [StringLength(500)]
        public string MechanicNote { get; set; } = string.Empty;

        public int NextMaintenanceKm { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }

        public Vehicle? Vehicle { get; set; }
        public List<ServiceItem> ServiceItems { get; set; } = new();
    }
}