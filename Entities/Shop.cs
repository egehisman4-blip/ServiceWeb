namespace mepa_servis.Entities
{
    public class Shop
    {
        public int Id { get; set; }
        public string ShopCode { get; set; } = string.Empty;     // Benzersiz dükkan kodu (Örn: "mepa", "yildizoto")
        public string ShopName { get; set; } = string.Empty;     // Dükkan Görünen Adı
        public string PasswordHash { get; set; } = string.Empty; // Dükkan Şifresi
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Iban { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
    }
}