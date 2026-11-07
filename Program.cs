using Microsoft.EntityFrameworkCore;
using mepa_servis.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller ve API Servislerini Ekle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. SQLite Veritabanı Yolu (Hem Yerel Hem Render Sunucu Uyumlu)
var folder = Environment.SpecialFolder.LocalApplicationData;
var path = Environment.GetFolderPath(folder);
var dbPath = System.IO.Path.Join(path, "mepa.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// 3. Veritabanını Otomatik Oluştur / Migration Uygula
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// 4. HTTP Pipeline Yapılandırması
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Statik Dosya (wwwroot / index.html / admin.html) Sunumu
app.UseDefaultFiles(); // index.html'i otomatik varsayılan yapar
app.UseStaticFiles();  // wwwroot içindeki admin.html vb. dosyaların doğrudan açılmasını sağlar

app.UseAuthorization();
app.MapControllers();

// 6. Doğrudan HTML dosyalarına erişim fallback tanımı
app.MapFallbackToFile("index.html");

app.Run();