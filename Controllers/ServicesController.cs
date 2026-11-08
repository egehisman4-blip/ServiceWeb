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

        [HttpGet("upcoming-maintenances")]
        public async Task<IActionResult> GetUpcomingMaintenances()
        {
            var vehicles = await _context.Vehicles.ToListAsync();
            return Ok(vehicles);
        }

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

        [HttpPost("create-vehicle")]
        public async Task<IActionResult> CreateVehicle([FromBody] Vehicle input)
        {
            if (input == null) return BadRequest("Geçersiz veri.");

            try
            {
                var cleanPlate = (input.Plate ?? "").Replace(" ", "").ToUpper();
                var vehicle = await _context.Vehicles
                    .Include(v => v.ServiceRecords)
                    .FirstOrDefaultAsync(v => v.Plate == cleanPlate);

                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        Plate = cleanPlate,
                        Brand = input.Brand ?? "",
                        Model = input.Model ?? "",
                        Year = input.Year > 0 ? input.Year : 2024,
                        InitialKm = input.CurrentKm > 0 ? input.CurrentKm : input.InitialKm,
                        CurrentKm = input.CurrentKm,
                        LastKmUpdateDate = DateTime.Now,
                        OwnerName = input.OwnerName ?? "",
                        OwnerPhone = input.OwnerPhone ?? "",
                        ServiceRecords = new List<ServiceRecord>()
                    };
                    _context.Vehicles.Add(vehicle);
                }
                else
                {
                    vehicle.Brand = input.Brand ?? vehicle.Brand;
                    vehicle.Model = input.Model ?? vehicle.Model;
                    vehicle.Year = input.Year > 0 ? input.Year : vehicle.Year;
                    
                    if (vehicle.InitialKm == 0)
                    {
                        vehicle.InitialKm = vehicle.CurrentKm > 0 ? vehicle.CurrentKm : input.CurrentKm;
                    }
                    
                    vehicle.CurrentKm = input.CurrentKm;
                    vehicle.LastKmUpdateDate = DateTime.Now;
                    vehicle.OwnerName = input.OwnerName ?? vehicle.OwnerName;
                    vehicle.OwnerPhone = input.OwnerPhone ?? vehicle.OwnerPhone;
                }

                // HEDEF BAKIM KM DİNAMİK HESAPLAMA (GÜNCEL KM ÜZERİNDEN SONRAKİ 10.000)
                int nextKm = Convert.ToInt32((Math.Floor((double)vehicle.CurrentKm / 10000) + 1) * 10000);
                string note = input.ServiceRecords?.FirstOrDefault()?.MechanicNote ?? "Servis kaydı açıldı.";

                var newRecord = new ServiceRecord
                {
                    EntryDate = DateTime.Now,
                    Status = ServiceStatus.Accepted,
                    TotalCost = 0,
                    MechanicNote = note,
                    NextMaintenanceKm = nextKm,
                    ServiceItems = new List<ServiceItem>()
                };

                vehicle.ServiceRecords.Add(newRecord);
                await _context.SaveChangesAsync();

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"SQLite Hatası: {inner}");
            }
        }

        [HttpPatch("update-km/{vehicleId}")]
        public async Task<IActionResult> UpdateVehicleKm(int vehicleId, [FromBody] int newKm)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null) return NotFound("Araç bulunamadı.");

            if (vehicle.InitialKm == 0)
            {
                vehicle.InitialKm = vehicle.CurrentKm;
            }

            vehicle.CurrentKm = newKm;
            vehicle.LastKmUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }

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

        [HttpDelete("delete-vehicle/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return Ok();
        }

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