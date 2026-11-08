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

        // 1. Dükkan Ayarları (Getir)
        [HttpGet("shop-settings")]
        public async Task<IActionResult> GetShopSettings()
        {
            var setting = await _context.ShopSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new ShopSetting { ShopName = "OtoServis Pro", Phone = "", Address = "", Iban = "", LogoUrl = "" };
            }
            return Ok(setting);
        }

        // 2. Dükkan Ayarları (Güncelle)
        [HttpPut("shop-settings")]
        public async Task<IActionResult> UpdateShopSettings([FromBody] ShopSetting setting)
        {
            var existing = await _context.ShopSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _context.ShopSettings.Add(setting);
            }
            else
            {
                existing.ShopName = setting.ShopName;
                existing.Phone = setting.Phone;
                existing.Address = setting.Address;
                existing.Iban = setting.Iban;
                existing.LogoUrl = setting.LogoUrl;
                _context.ShopSettings.Update(existing);
            }
            await _context.SaveChangesAsync();
            return Ok(setting);
        }

        // 3. Yaklaşan Bakımlar
        [HttpGet("upcoming-maintenances")]
        public async Task<IActionResult> GetUpcomingMaintenances()
        {
            var vehicles = await _context.Vehicles.ToListAsync();
            return Ok(vehicles);
        }

        // 4. Plaka İle Arama
        [HttpGet("search/{plaka}")]
        public async Task<IActionResult> SearchByPlaka(string plaka)
        {
            if (string.IsNullOrWhiteSpace(plaka)) return BadRequest(new { message = "Plaka giriniz." });

            var cleanPlaka = plaka.Replace(" ", "").ToUpper();
            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                .ThenInclude(r => r.ServiceItems)
                .FirstOrDefaultAsync(v => v.Plate.Replace(" ", "").ToUpper() == cleanPlaka);

            if (vehicle == null) return NotFound(new { message = "Araç bulunamadı." });

            return Ok(vehicle);
        }

        // 5. Araç / Servis Kaydı Açma
        [HttpPost("create-vehicle")]
        public async Task<IActionResult> CreateVehicle([FromBody] Vehicle vehicle)
        {
            if (vehicle == null) return BadRequest("Geçersiz veri.");

            try
            {
                vehicle.Plate = (vehicle.Plate ?? "").Replace(" ", "").ToUpper();

                var existingVehicle = await _context.Vehicles
                    .Include(v => v.ServiceRecords)
                    .FirstOrDefaultAsync(v => v.Plate == vehicle.Plate);

                if (existingVehicle != null)
                {
                    existingVehicle.Brand = vehicle.Brand;
                    existingVehicle.Model = vehicle.Model;
                    existingVehicle.Year = vehicle.Year;
                    existingVehicle.CurrentKm = vehicle.CurrentKm;
                    existingVehicle.OwnerName = vehicle.OwnerName;
                    existingVehicle.OwnerPhone = vehicle.OwnerPhone;

                    _context.Vehicles.Update(existingVehicle);
                }
                else
                {
                    _context.Vehicles.Add(vehicle);
                }

                await _context.SaveChangesAsync();
                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veritabanı Kayıt Hatası: {ex.Message}");
            }
        }

        // 6. Parça / İşçilik Ekleme
        [HttpPost("add-item/{recordId}")]
        public async Task<IActionResult> AddItem(int recordId, [FromBody] ServiceItem item)
        {
            var record = await _context.ServiceRecords
                .Include(r => r.ServiceItems)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null) return NotFound("Servis kaydı bulunamadı.");

            record.ServiceItems.Add(item);
            record.TotalCost += item.Price;

            await _context.SaveChangesAsync();
            return Ok(record);
        }

        // 7. Parça Silme
        [HttpDelete("delete-item/{itemId}")]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            var item = await _context.ServiceItems.FindAsync(itemId);
            if (item == null) return NotFound();

            var record = await _context.ServiceRecords.FindAsync(item.ServiceRecordId);
            if (record != null)
            {
                record.TotalCost -= item.Price;
            }

            _context.ServiceItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 8. Araç Silme
        [HttpDelete("delete-vehicle/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 9. Durum Güncelleme
        [HttpPatch("update-status/{recordId}")]
        public async Task<IActionResult> UpdateStatus(int recordId, [FromBody] int status)
        {
            var record = await _context.ServiceRecords.FindAsync(recordId);
            if (record == null) return NotFound();

            record.Status = (ServiceStatus)status;
            await _context.SaveChangesAsync();
            return Ok(record);
        }
    }
}