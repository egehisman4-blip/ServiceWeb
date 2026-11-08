using System;
using System.Collections.Generic;

namespace mepa_servis.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Plate { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public int InitialKm { get; set; } // İlk Giriş KM
        public int CurrentKm { get; set; } // Güncel KM
        public DateTime LastKmUpdateDate { get; set; } = DateTime.Now; // Son Güncelleme Tarihi
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public int ShopId { get; set; }

        public List<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    }
}