namespace mepa_servis.Entities
{
    public class DamageRecord
    {
        public int Id { get; set; }
        public string Plate { get; set; } = string.Empty;       // Araç Plakası
        public string Region { get; set; } = string.Empty;      // Hasarlı Bölge (Örn: Ön Tampon, Sol Kapı)
        public string DamageType { get; set; } = string.Empty;  // Hasar Türü (Örn: Çizik, Göçük, Kırık)
        public string Description { get; set; } = string.Empty; // Ek Açıklama
        public DateTime Date { get; set; } = DateTime.Now; 
        public int ShopId { get; set; }     // Kayıt Tarihi
    }
}