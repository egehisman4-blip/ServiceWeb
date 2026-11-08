namespace mepa_servis.Entities
{
    public class StockItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Parça Adı (Örn: 5W-30 Motor Yağı)
        public string Sku { get; set; } = string.Empty;  // Stok Kodu
        public decimal PurchasePrice { get; set; }       // Alış Fiyatı
        public decimal SalePrice { get; set; }           // Satış Fiyatı
        public int Quantity { get; set; }                // Mevcut Adet
    }
}