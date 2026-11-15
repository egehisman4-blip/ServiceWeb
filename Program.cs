using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using mepa_servis.Data;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Controller ve JSON Sonsuz Döngü Engelleyicisi
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- JWT KİMLİK DOĞRULAMA (AUTHENTICATION) AYARLARI --- //
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MepaServisSuperSecretKeyThatIsLongEnough2026!";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "mepa-servis",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "mepa-servis-users",
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization();

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

// JWT için Authentication ve Authorization sıralaması kritik!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();