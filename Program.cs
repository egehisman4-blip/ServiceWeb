using Microsoft.EntityFrameworkCore;
using mepa_servis.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller ve JSON Sonsuz Döngü Engelleyicisi
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Veritabanı Konfigürasyonu (Uygulama Klasörüne Sabitlendi)
// Bu sayede veritabanı her zaman uygulamanın olduğu yerde (mepa.db) kalır, asla uçmaz veya sıfırlanmaz.
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mepa.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 3. CORS Politikası
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// 4. Veritabanı ve Tabloları Otomatik Oluştur
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// 5. Middleware Konfigürasyonu
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();