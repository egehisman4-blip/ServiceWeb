namespace mepa_servis.Entities
{
    public class Appointment
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty; // Müşteri Ad Soyad
        public string Plate { get; set; } = string.Empty;        // Araç Plakası
        public DateTime AppointmentDate { get; set; }            // Randevu Tarih ve Saati
        public string Note { get; set; } = string.Empty;         // Şikayet / İşlem Notu
    }
}