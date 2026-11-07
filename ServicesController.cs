using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mepa_servis.Data;
using mepa_servis.Entities;

namespace mepa_servis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Plaka ile Araç & Servis Geçmişi Getir
        [HttpGet("search/{plate}")]
        public async Task<IActionResult> GetByPlate(string plate)
        {
            var formattedPlate = plate.Replace(" ", "").ToUpper();

            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                    .ThenInclude(sr => sr.ServiceItems)
                .FirstOrDefaultAsync(v => v.Plate == formattedPlate);

            if (vehicle == null)
                return NotFound(new { message = "Bu plakaya ait araç kaydı bulunamadı." });

            return Ok(vehicle);
        }

        // 2. Yeni Araç / Servis Kaydı Aç
        [HttpPost("create-vehicle")]
        public async Task<IActionResult> CreateVehicle([FromBody] Vehicle vehicle)
        {
            vehicle.Plate = vehicle.Plate.Replace(" ", "").ToUpper();

            // İlk kayıt açılırken hedef bakımı en yakın 10.000 katına yuvarlar
            int targetKm = ((vehicle.CurrentKm / 10000) + 1) * 10000;
            if (vehicle.ServiceRecords != null && vehicle.ServiceRecords.Any())
            {
                foreach (var rec in vehicle.ServiceRecords)
                {
                    rec.NextMaintenanceKm = targetKm;
                }
            }

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(vehicle);
        }

        // 3. Aracın Canlı Durumunu Güncelle
        [HttpPatch("update-status/{recordId}")]
        public async Task<IActionResult> UpdateStatus(int recordId, [FromBody] ServiceStatus status, [FromQuery] string mechanicNote = "")
        {
            var record = await _context.ServiceRecords.FindAsync(recordId);
            if (record == null) return NotFound("İş emri bulunamadı.");

            record.Status = status;
            if (!string.IsNullOrEmpty(mechanicNote))
            {
                record.MechanicNote = mechanicNote;
            }

            if (status == ServiceStatus.Ready)
            {
                record.ExitDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Durum başarıyla güncellendi.", currentStatus = record.Status });
        }

        // 4. Servis Kaydına Değişen Parça / İşçilik Ekle
        [HttpPost("add-item/{recordId}")]
        public async Task<IActionResult> AddServiceItem(int recordId, [FromBody] ServiceItem item)
        {
            var record = await _context.ServiceRecords
                .Include(r => r.ServiceItems)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null) return NotFound("Servis kaydı bulunamadı.");

            item.ServiceRecordId = recordId;
            _context.ServiceItems.Add(item);

            record.TotalCost += item.Price;

            await _context.SaveChangesAsync();
            return Ok(record);
        }

        // 5. Parça Kalemini Sil
        [HttpDelete("delete-item/{itemId}")]
        public async Task<IActionResult> DeleteServiceItem(int itemId)
        {
            var item = await _context.ServiceItems.FindAsync(itemId);
            if (item == null) return NotFound("Parça bulunamadı.");

            var record = await _context.ServiceRecords.FindAsync(item.ServiceRecordId);
            if (record != null)
            {
                record.TotalCost -= item.Price;
                if (record.TotalCost < 0) record.TotalCost = 0;
            }

            _context.ServiceItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Parça silindi." });
        }

        // 6. Araç ve Servis Kaydını Tamamen Sil
        [HttpDelete("delete-vehicle/{vehicleId}")]
        public async Task<IActionResult> DeleteVehicle(int vehicleId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                    .ThenInclude(sr => sr.ServiceItems)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (vehicle == null) return NotFound("Araç kaydı bulunamadı.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Araç ve tüm servis geçmişi silindi." });
        }

        // 7. Araç ve Müşteri Bilgilerini Düzenle (KM Değiştiğinde Bakımı Mantıklı Katına Yuvarlar)
        [HttpPut("update-vehicle/{vehicleId}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] Vehicle updatedVehicle)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (vehicle == null) return NotFound("Araç bulunamadı.");

            vehicle.Brand = updatedVehicle.Brand;
            vehicle.Model = updatedVehicle.Model;
            vehicle.Year = updatedVehicle.Year;
            vehicle.CurrentKm = updatedVehicle.CurrentKm;
            vehicle.OwnerName = updatedVehicle.OwnerName;
            vehicle.OwnerPhone = updatedVehicle.OwnerPhone;

            // Mantıklı KM Hesabı: 65.000 KM yapıldıysa bir sonraki bakım hedefi 70.000 KM olur
            int calculatedNextKm = ((updatedVehicle.CurrentKm / 10000) + 1) * 10000;

            var lastRecord = vehicle.ServiceRecords.OrderByDescending(r => r.EntryDate).FirstOrDefault();
            if (lastRecord != null)
            {
                lastRecord.NextMaintenanceKm = calculatedNextKm;
            }

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }

        // 8. Periyodik Bakım Takip Listesi
        [HttpGet("upcoming-maintenances")]
        public async Task<IActionResult> GetUpcomingMaintenances()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                .ToListAsync();

            var upcomingList = new List<object>();

            foreach (var v in vehicles)
            {
                var lastRecord = v.ServiceRecords.OrderByDescending(r => r.EntryDate).FirstOrDefault();
                
                int nextKm = (lastRecord != null && lastRecord.NextMaintenanceKm > 0) 
                    ? lastRecord.NextMaintenanceKm 
                    : (((v.CurrentKm / 10000) + 1) * 10000);

                upcomingList.Add(new {
                    id = v.Id,
                    plate = v.Plate,
                    brand = v.Brand,
                    model = v.Model,
                    ownerName = v.OwnerName,
                    ownerPhone = v.OwnerPhone,
                    currentKm = v.CurrentKm,
                    nextKm = nextKm,
                    lastServiceDate = lastRecord != null ? lastRecord.EntryDate : DateTime.UtcNow
                });
            }

            return Ok(upcomingList);
        }

        // 9. Dükkan Ayarlarını Getir
        [HttpGet("shop-settings")]
        public async Task<IActionResult> GetShopSettings()
        {
            try
            {
                var setting = await _context.ShopSettings.FirstOrDefaultAsync();
                if (setting == null)
                {
                    setting = new ShopSetting { ShopName = "OtoServis Pro", Phone = "", Address = "", Iban = "", LogoUrl = "" };
                    _context.ShopSettings.Add(setting);
                    await _context.SaveChangesAsync();
                }
                return Ok(setting);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Veritabanı hatası", error = ex.Message });
            }
        }

        // 10. Dükkan Ayarlarını Güncelle
        [HttpPut("shop-settings")]
        public async Task<IActionResult> UpdateShopSettings([FromBody] ShopSetting updated)
        {
            try
            {
                var setting = await _context.ShopSettings.FirstOrDefaultAsync();
                if (setting == null)
                {
                    setting = new ShopSetting
                    {
                        ShopName = updated.ShopName ?? "OtoServis Pro",
                        Phone = updated.Phone ?? "",
                        Address = updated.Address ?? "",
                        Iban = updated.Iban ?? "",
                        LogoUrl = updated.LogoUrl ?? ""
                    };
                    _context.ShopSettings.Add(setting);
                }
                else
                {
                    setting.ShopName = string.IsNullOrWhiteSpace(updated.ShopName) ? "OtoServis Pro" : updated.ShopName;
                    setting.Phone = updated.Phone ?? "";
                    setting.Address = updated.Address ?? "";
                    setting.Iban = updated.Iban ?? "";
                    setting.LogoUrl = updated.LogoUrl ?? "";
                }

                await _context.SaveChangesAsync();
                return Ok(setting);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ayarlar güncellenirken sunucu hatası oluştu.", error = ex.Message });
            }
        }
    }
}