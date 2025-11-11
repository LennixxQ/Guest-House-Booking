using GuestHouseBookingCore;
using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using GuestHouseBookingCore.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. DB CONTEXT — SABSE PEHLE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. GENERIC REPOSITORY
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// 3. SPECIFIC REPOSITORIES
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRepository<LogTable>, Repository<LogTable>>();
builder.Services.AddScoped<IRepository<Rooms>, Repository<Rooms>>();
builder.Services.AddScoped<IRepository<Beds>, Repository<Beds>>();
builder.Services.AddScoped<IRepository<Bookings>, Repository<Bookings>>();
builder.Services.AddScoped<IRepository<LogTable>, Repository<LogTable>>();

// 4. SERVICES
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<GetAvailableBeds>();
builder.Services.AddScoped<GetCurrentAdmin>();
builder.Services.AddScoped<ILogService, LogService>();


// 5. JWT & AUTH
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtService>();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrGuest", policy =>
        policy.RequireRole("Admin", "Guest"));
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // YE PEHLE
app.UseAuthorization();   // YE BAAD MEIN

app.MapControllers();
app.Run();


