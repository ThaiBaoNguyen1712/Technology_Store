using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Reflection;
using Tech_Store.Helpers;
using Tech_Store.Hubs;
using Tech_Store.Middleware;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Services;
using Tech_Store.Services.Admin.BrandServices;
using Tech_Store.Services.Admin.BannerServices;
using Tech_Store.Services.Admin.CategoryServices;
using Tech_Store.Services.Admin.Interfaces;
using Tech_Store.Services.Admin.MomoServices;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Admin.ProductServices;
using Tech_Store.Services.Admin.SupplierServices;
using Tech_Store.Services.Admin.VNPayServices;
using Tech_Store.Services.Admin.VoucherServices;
using Tech_Store.Services.Auth;
using Tech_Store.Services.Client;
using Tech_Store.Services.Client.Storefront;
using Tech_Store.Services.Client.RecommendServices;
using Tech_Store.Services.Interfaces.Auth;
using Tech_Store.Services.Payment;
using Tech_Store.Services.Recommendation;
using static Org.BouncyCastle.Math.EC.ECCurve;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMemoryCache();

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
builder.Services.AddScoped<IBannerAdminService, BannerAdminService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IBannerQueryService, BannerQueryService>();
builder.Services.AddScoped<IPaymentGatewaySettingsService, PaymentGatewaySettingsService>();
builder.Services.AddScoped<IClientPaymentService, ClientPaymentService>();
builder.Services.AddScoped<IOnlinePaymentGatewayService, OnlinePaymentGatewayService>();
builder.Services.AddScoped<ISePayService, SePayService>();
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection("SePay"));
builder.Services.AddScoped<ICloudflareTurnstileService, CloudflareTurnstileService>();
builder.Services.Configure<CloudflareTurnstileOptions>(builder.Configuration.GetSection(CloudflareTurnstileOptions.SectionName));
builder.Services.AddScoped<IAuthenticationFlowService, AuthenticationFlowService>();
builder.Services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
builder.Services.AddScoped<IHomePageContentService, HomePageContentService>();

// VNPay and Momo Payment Services
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddSingleton<IMomoService, MomoService>();

// Email Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddScoped<ProductServices>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<RecommendServices>();


// Configure Form Options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
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
builder.Services.AddScoped<IUserActivityTrackingService, UserActivityTrackingService>();
builder.Services.AddSingleton<UserProductEventBackgroundQueue>();
builder.Services.AddSingleton<IUserProductEventQueue>(sp => sp.GetRequiredService<UserProductEventBackgroundQueue>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<UserProductEventBackgroundQueue>());
builder.Services.AddScoped<IUserProductEventTrackingService, UserProductEventTrackingService>();
builder.Services.AddHostedService<UserProductEventRetentionService>();

builder.Services.AddSignalR();

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var requestPath = httpContext.Request.Path;
    var statusCode = httpContext.Response.StatusCode;

    if (statusCode == StatusCodes.Status404NotFound)
    {
        var errorPath = requestPath.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
            ? "/Admin/Error/404"
            : "/404";

        httpContext.Response.Redirect(errorPath);
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/500");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files before routing

app.UseStaticFiles();
// Custom Middleware to manage guest sessions
app.UseMiddleware<GuestSessionMiddleware>();
// Custom Middleware to track product views
app.UseMiddleware<TrackProductViewMiddleware>();

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

//  Enable session first
app.UseSession();

//Authentication & Authorization should be placed after session
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TrackAuthenticatedUserActivityMiddleware>();

// SignalR route
app.MapHub<NotificationHub>("/notificationHub");

// Configure routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        await next();
    });
}

app.Run();
