namespace mepa_servis.Entities
{
    public class FinanceRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now; // İşlem Tarihi
        public string Description { get; set; } = string.Empty; // Açıklama (Örn: Elektrik Faturası, Parça Satışı)
        public string Type { get; set; } = "income"; // "income" (Gelir) veya "expense" (Gider)
        public decimal Amount { get; set; } // Tutar (₺)
    }
}