using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using YumQuick.Api.Hubs;
using YumQuick.Api.Services;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Core.Interfaces;
using YumQuick.Data;
using Stripe;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
{
    {
        new OpenApiSecuritySchemeReference(
            referenceId: "Bearer",
            hostDocument: doc,
            externalResource: null
        ),
        new List<string>()
    }
});

});

// 1. Setup Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Setup Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Setup JWT Authentication
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var firebaseConfigPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-config.json");
GoogleCredential googleCredential;
using (var stream = System.IO.File.OpenRead(firebaseConfigPath))
{
    googleCredential = GoogleCredential.FromStream(stream);
}
var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = googleCredential
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IImageService, CloudinaryImageService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();


var app = builder.Build();

// Seed Default Roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DefaultRolesSeeder.SeedAsync(roleManager);
}

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors("AllowAll");

// 3. Authentication & Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();