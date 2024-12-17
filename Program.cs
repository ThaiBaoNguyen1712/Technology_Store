using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using reCAPTCHA.AspNetCore;
using Tech_Store.Models;
using Tech_Store.Helpers;
using Tech_Store.Services;
using Tech_Store.Services.VNPayServices;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Services.MomoServices;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using Tech_Store.Hubs;
using Tech_Store.Services.NotificationServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
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
    options.SiteKey = builder.Configuration["ReCaptcha:SiteKey"];
    options.SecretKey = builder.Configuration["ReCaptcha:SecretKey"];
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Session Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout duration
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Config Login Google account & Authentication and Authorization
builder.Services.AddAuthentication(option =>
{
    // Đặt mặc định là Cookie
    option.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    option.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login"; // Đường dẫn khi chưa đăng nhập
    options.ExpireTimeSpan = TimeSpan.FromDays(1); // Thời gian hết hạn cookie
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, option =>
{
    option.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
    option.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration.GetSection("Facebook:AppId").Value;
    facebookOptions.AppSecret = builder.Configuration.GetSection("Facebook:AppSecret").Value;
    facebookOptions.AccessDeniedPath = "/AccessDeniedPathInfo";
}); ;
builder.Services.AddScoped<SitemapService>();
builder.Services.AddScoped<NotificationService>();
//Thêm SignalR để xử lý Realtime
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Set max request body size in middleware
app.Use(async (context, next) =>
{
    context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 50 * (1024 * 1024); // 50 MB
    await next();
});

// Enable Session and Authentication
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
