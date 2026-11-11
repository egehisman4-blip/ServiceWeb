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

// 2. PostgreSQL (Neon.tech) Veritabanı Konfigürasyonu
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=ep-young-hall-bajhbrym-pooler.c-8.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_HjeSdJR6u1lB;Ssl Mode=Require;Pooling=true"));
// 3. CORS Politikası
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
});

var app = builder.Build();

// 4. Bulut Veritabanı Otomatik Migrate (Tabloları Oluşturma / Güncelleme)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
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