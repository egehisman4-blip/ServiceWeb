namespace mepa_servis.Entities
{
    public class ShopSetting
    {
        public int Id { get; set; }
        public string ShopName { get; set; } = "OtoServis Pro";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Iban { get; set; } = "";
        public string LogoUrl { get; set; } = "";
    }
}