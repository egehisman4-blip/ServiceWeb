using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mepa_servis.Data;
using mepa_servis.Entities;

namespace mepa_servis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. DÜKKAN GİRİŞİ (LOGIN) --- //
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (string.IsNullOrWhiteSpace(model.ShopCode) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Dükkan kodu ve şifre zorunludur." });
            }

            var cleanCode = model.ShopCode.Trim().ToLower();
            var shop = await _context.Shops
                .FirstOrDefaultAsync(s => s.ShopCode.ToLower() == cleanCode && s.PasswordHash == model.Password);

            if (shop == null)
            {
                return Unauthorized(new { message = "Hatalı dükkan kodu veya şifre!" });
            }

            return Ok(new { message = "Giriş başarılı", shopId = shop.Id, shopName = shop.ShopName });
        }

        // --- 2. YENİ DÜKKAN KAYIT (SÜPER ADMIN) --- //
        [HttpPost("register-shop")]
        public async Task<IActionResult> RegisterShop([FromBody] Shop newShop)
        {
            if (string.IsNullOrWhiteSpace(newShop.ShopCode) || string.IsNullOrWhiteSpace(newShop.PasswordHash))
            {
                return BadRequest("Dükkan kodu ve şifre zorunludur.");
            }

            var cleanCode = newShop.ShopCode.Trim().ToLower();
            var existing = await _context.Shops.FirstOrDefaultAsync(s => s.ShopCode.ToLower() == cleanCode);
            if (existing != null)
            {
                return BadRequest(new { message = "Bu dükkan kodu zaten kullanımda!" });
            }

            newShop.ShopCode = cleanCode;
            _context.Shops.Add(newShop);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Dükkan başarıyla oluşturuldu!", shopId = newShop.Id });
        }

        // --- 3. DÜKKAN AYARLARI GETİR & GÜNCELLE --- //
        [HttpGet("shop-settings")]
        public async Task<IActionResult> GetShopSettings([FromQuery] int shopId)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null) return NotFound("Dükkan bulunamadı.");
            return Ok(shop);
        }

        [HttpPut("shop-settings")]
        public async Task<IActionResult> UpdateShopSettings([FromBody] Shop updatedShop)
        {
            var shop = await _context.Shops.FindAsync(updatedShop.Id);
            if (shop == null) return NotFound("Dükkan bulunamadı.");

            shop.ShopName = updatedShop.ShopName;
            shop.Phone = updatedShop.Phone;
            shop.Address = updatedShop.Address;
            shop.Iban = updatedShop.Iban;
            shop.LogoUrl = updatedShop.LogoUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ayarlar güncellendi." });
        }

        // --- 4. ARAÇ & SERVİS İŞLEMLERİ (DÜKKAN İZOLELİ) --- //

        [HttpPost("create-vehicle")]
        public async Task<IActionResult> CreateVehicle([FromBody] Vehicle vehicle)
        {
            if (vehicle.ShopId <= 0) return BadRequest("Geçersiz dükkan ID.");

            vehicle.Plate = vehicle.Plate?.Trim().ToUpper() ?? "";
            vehicle.CurrentKm = vehicle.CurrentKm > 0 ? vehicle.CurrentKm : 0;
            vehicle.LastKmUpdateDate = DateTime.Now;

            // İlk servis kaydını otomatik aç
            var initialRecord = new ServiceRecord
            {
                EntryDate = DateTime.Now,
                MechanicNote = "Araç kabul edildi.",
                Status = (ServiceStatus)1
            };

            vehicle.ServiceRecords = new List<ServiceRecord> { initialRecord };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Araç başarıyla kaydedildi.", vehicleId = vehicle.Id });
        }

        [HttpGet("search/{plate}")]
        public async Task<IActionResult> SearchVehicle(string plate, [FromQuery] int shopId)
        {
            var cleanPlate = plate.Trim().ToUpper();
            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                    .ThenInclude(sr => sr.ServiceItems)
                .FirstOrDefaultAsync(v => v.Plate.ToUpper() == cleanPlate && v.ShopId == shopId);

            if (vehicle == null)
            {
                return NotFound(new { message = "Bu plaka bu dükkanda bulunamadı." });
            }

            return Ok(vehicle);
        }

        [HttpGet("upcoming-maintenances")]
        public async Task<IActionResult> GetUpcomingMaintenances([FromQuery] int shopId)
        {
            var list = await _context.Vehicles
                .Where(v => v.ShopId == shopId)
                .Include(v => v.ServiceRecords)
                .OrderByDescending(v => v.Id)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPatch("update-km/{id}")]
        public async Task<IActionResult> UpdateKm(int id, [FromBody] int newKm)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound("Araç bulunamadı.");

            vehicle.CurrentKm = newKm;
            vehicle.LastKmUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Kilometre güncellendi." });
        }

        [HttpPatch("update-status/{recordId}")]
        public async Task<IActionResult> UpdateStatus(int recordId, [FromBody] int status)
        {
            var record = await _context.ServiceRecords.FindAsync(recordId);
            if (record == null) return NotFound("Servis kaydı bulunamadı.");

            record.Status = (ServiceStatus)status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Durum güncellendi." });
        }

        [HttpPost("add-item/{recordId}")]
        public async Task<IActionResult> AddServiceItem(int recordId, [FromBody] ServiceItem item)
        {
            var record = await _context.ServiceRecords
                .Include(r => r.ServiceItems)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (record == null) return NotFound("Servis kaydı bulunamadı.");

            item.ServiceRecordId = recordId;
            _context.ServiceItems.Add(item);
            await _context.SaveChangesAsync();

            record.TotalCost = record.ServiceItems.Sum(i => i.Price);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpDelete("item/{id}")]
        public async Task<IActionResult> DeleteServiceItem(int id)
        {
            var item = await _context.ServiceItems.FindAsync(id);
            if (item == null) return NotFound();

            // Kalıcı silmek yerine arşivliyoruz:
            item.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "İşlem başarıyla arşive kaldırıldı." });
        }

        [HttpDelete("delete-vehicle/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.ServiceRecords)
                    .ThenInclude(sr => sr.ServiceItems)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null) return NotFound("Araç bulunamadı.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Araç ve tüm geçmişi silindi." });
        }


        // --- 5. STOK YÖNETİMİ (DÜKKAN İZOLELİ) --- //

        [HttpGet("stock")]
        public async Task<IActionResult> GetStock([FromQuery] int shopId)
        {
            var items = await _context.StockItems
                .Where(s => s.ShopId == shopId)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            return Ok(items);
        }

        [HttpPost("stock")]
        public async Task<IActionResult> AddStock([FromBody] StockItem item)
        {
            if (item.ShopId <= 0) return BadRequest("Geçersiz dükkan ID.");
            
            _context.StockItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("stock/{id}")]
        public async Task<IActionResult> DeleteStock(int id)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return NotFound("Parça bulunamadı.");

            _context.StockItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Stok silindi." });
        }


        // --- 6. FİNANS & CİRO YÖNETİMİ (DÜKKAN İZOLELİ) --- //

        [HttpGet("finance")]
        public async Task<IActionResult> GetFinance([FromQuery] int shopId)
        {
            var records = await _context.FinanceRecords
                .Where(f => f.ShopId == shopId)
                .OrderByDescending(f => f.Date)
                .ToListAsync();
            return Ok(records);
        }

        [HttpPost("finance")]
        public async Task<IActionResult> AddFinance([FromBody] FinanceRecord record)
        {
            if (record.ShopId <= 0) return BadRequest("Geçersiz dükkan ID.");

            record.Date = DateTime.Now;
            _context.FinanceRecords.Add(record);
            await _context.SaveChangesAsync();
            return Ok(record);
        }


        // --- 7. RANDEVU YÖNETİMİ (DÜKKAN İZOLELİ) --- //

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments([FromQuery] int shopId)
        {
            var list = await _context.Appointments
                .Where(a => a.ShopId == shopId)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> AddAppointment([FromBody] Appointment appointment)
        {
            if (appointment.ShopId <= 0) return BadRequest("Geçersiz dükkan ID.");

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return Ok(appointment);
        }

        [HttpDelete("appointments/{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var app = await _context.Appointments.FindAsync(id);
            if (app == null) return NotFound("Randevu bulunamadı.");

            _context.Appointments.Remove(app);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Randevu silindi." });
        }


        // --- 8. ARAÇ KABUL & ÇİZİK HARİTASI (DÜKKAN İZOLELİ) --- //

        [HttpGet("damages/{plate}")]
        public async Task<IActionResult> GetDamages(string plate, [FromQuery] int shopId)
        {
            var cleanPlate = plate.Trim().ToUpper();
            var list = await _context.DamageRecords
                .Where(d => d.Plate.ToUpper() == cleanPlate && d.ShopId == shopId)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost("damages")]
        public async Task<IActionResult> AddDamage([FromBody] DamageRecord damage)
        {
            if (damage.ShopId <= 0) return BadRequest("Geçersiz dükkan ID.");

            damage.Plate = damage.Plate.Trim().ToUpper();
            damage.Date = DateTime.Now;

            _context.DamageRecords.Add(damage);
            await _context.SaveChangesAsync();
            return Ok(damage);
        }

        [HttpDelete("damages/{id}")]
        public async Task<IActionResult> DeleteDamage(int id)
        {
            var damage = await _context.DamageRecords.FindAsync(id);
            if (damage == null) return NotFound("Hasar kaydı bulunamadı.");

            _context.DamageRecords.Remove(damage);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Hasar silindi." });
        }
    }

    public class LoginDto
    {
        public string ShopCode { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}