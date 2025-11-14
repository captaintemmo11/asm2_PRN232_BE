using asm2_PRN232_BE.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------
// ✅ 1️⃣ Logging - hiển thị log trong Render Dashboard
// -----------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// -----------------------------------------------------
// ✅ 2️⃣ Add Services
// -----------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------------------------------
// ✅ 3️⃣ Database Configuration (PostgreSQL - Render NeonDB)
// -----------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

// -----------------------------------------------------
// ✅ 4️⃣ JWT Authentication Config
// -----------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            Console.WriteLine("[JWT CONFIG WARNING] Jwt:Key is missing!");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? "FAKE_KEY"))
        };
    });

// -----------------------------------------------------
// ✅ 5️⃣ Environment Variables (Render sẽ map tự động)
// -----------------------------------------------------
builder.Configuration.AddEnvironmentVariables();

// -----------------------------------------------------
// ✅ 6️⃣ CORS - Cho phép Vercel + Localhost gọi API
// -----------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
            "https://asmprn.vercel.app", // FE deploy Vercel
            "http://localhost:3000"      // FE dev local
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// -----------------------------------------------------
// ✅ 7️⃣ Build App
// -----------------------------------------------------
var app = builder.Build();

// -----------------------------------------------------
// ✅ 8️⃣ Middleware Pipeline
// -----------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Quan trọng: đặt CORS trước Authentication
app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

// Route test nhanh API
app.MapGet("/", () => Results.Ok(new
{
    status = "ok",
    message = "🚀 API is running on Render successfully",
    environment = app.Environment.EnvironmentName
}));

app.MapControllers();

app.Run();
