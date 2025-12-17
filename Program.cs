using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using reCAPTCHA.AspNetCore;
using StackExchange.Redis;
using System.Reflection;
using Tech_Store.Helpers;
using Tech_Store.Hubs;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Services;
using Tech_Store.Services.Admin.BrandServices;
using Tech_Store.Services.Admin.CategoryServices;
using Tech_Store.Services.Admin.Interfaces;
using Tech_Store.Services.Admin.MomoServices;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Admin.VNPayServices;
using Tech_Store.Services.Admin.VoucherServices;
using static Org.BouncyCastle.Math.EC.ECCurve;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
// Configure Identity
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration;
    var redisHost = config["Redis:Host"];
    var redisPassword = config["Redis:Password"];

    var options = new ConfigurationOptions
    {
        EndPoints = { redisHost },
        Password = redisPassword,
        Ssl = true,
        AbortOnConnectFail = false
    };

    return ConnectionMultiplexer.Connect(options);
});
// Identity Configuration
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();

// VNPay and Momo Payment Services
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddSingleton<IMomoService, MomoService>();

// Email Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// Configure Form Options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// reCAPTCHA Configuration
builder.Services.AddRecaptcha(options =>
{
    options.SiteKey = builder.Configuration["reCAPTCHA:SiteKey"];
    options.SecretKey = builder.Configuration["reCAPTCHA:SecretKey"];
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Session Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication (Google, Facebook, Cookie)
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    option.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, option =>
{
    option.ClientId = builder.Configuration["GoogleKeys:ClientId"];
    option.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Facebook:AppId"];
    facebookOptions.AppSecret = builder.Configuration["Facebook:AppSecret"];
    facebookOptions.AccessDeniedPath = "/AccessDeniedPathInfo";
});

// SignalR for Realtime Notifications
builder.Services.AddScoped<SitemapService>();
builder.Services.AddScoped<NotificationService>();

//Redis cache
builder.Services.AddScoped<RedisService>();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/404");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/500");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🟢 Serve static files before routing
app.UseStaticFiles();

app.UseRouting();

// 🟢 Set max request body size safely
app.Use(async (context, next) =>
{
    var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (feature != null)
    {
        feature.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
    }
    await next();
});

// 🟢 Enable session first
app.UseSession();

// 🟢 Authentication & Authorization should be placed after session
app.UseAuthentication();
app.UseAuthorization();

// SignalR route
app.MapHub<NotificationHub>("/notificationHub");

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
