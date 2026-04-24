using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PosSystem.Helpers;
using PosSystem.Hubs;
using PosSystem.Jobs;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// ตั้งค่า Serilog สำหรับเก็บ Log
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// เพิ่มบริการต่างๆ ลงใน Container
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

// ตั้งค่า Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".PosSystem.Session";
});

// ตั้งค่าการยืนยันตัวตน (Authentication) ด้วย Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "PosSystemAuthV2";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("ManagerOrAbove", p => p.RequireRole("IT_ADMIN","EXECUTIVE","MANAGER"));
    options.AddPolicy("ItAdminOnly",    p => p.RequireRole("IT_ADMIN"));
    options.AddPolicy("CashierOrAbove", p => p.RequireRole("IT_ADMIN","MANAGER","CASHIER"));
    options.AddPolicy("StockAccess",    p => p.RequireRole("IT_ADMIN","MANAGER","STOCK_KEEPER"));
});

// ตั้งค่า SignalR สำหรับการสื่อสารแบบ Real-time
builder.Services.AddSignalR();

// ตั้งค่า Hangfire สำหรับการทำงานเบื้องหลัง (Background Jobs)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
       .UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings()
       .UseSqlServerStorage(connectionString);
});
builder.Services.AddHangfireServer();

// การทำ Dependency Injection (DI) - ลงทะเบียน Service และ Repository
builder.Services.AddScoped<ISqlHelper, SqlHelper>();
builder.Services.AddScoped<PosSystem.Services.Interfaces.IAuthService, PosSystem.Services.Implementations.AuthService>();
builder.Services.AddScoped<PosSystem.Repositories.Interfaces.IUserRepository, PosSystem.Repositories.Implementations.UserRepository>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Services.Interfaces.IStockService, PosSystem.Services.Implementations.DummyStockService>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Repositories.Interfaces.IProductRepository, PosSystem.Services.Implementations.DummyProductRepository>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Services.Interfaces.IReportService, PosSystem.Services.Implementations.DummyReportService>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Services.Interfaces.ITableService, PosSystem.Services.Implementations.DummyTableService>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Services.Interfaces.IPromotionService, PosSystem.Services.Implementations.DummyPromotionService>();
builder.Services.AddScoped<PosSystem.Models.ViewModels.PosSystem.Services.Interfaces.ICustomerService, PosSystem.Services.Implementations.DummyCustomerService>();

var app = builder.Build();

// กำหนดลำดับการทำงานของ HTTP Request (Middleware Pipeline)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// หน้า Dashboard ของ Hangfire (จำกัดการเข้าถึงในระบบจริง)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Configure authorization here
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
    
app.MapHub<PosHub>("/hubs/pos");

// ลงทะเบียนงานที่ต้องทำซ้ำตามเวลา (Recurring Jobs)
RecurringJob.AddOrUpdate<StockJobs>(
    "release-expired-reservations",
    j => j.ReleaseExpiredReservationsAsync(),
    "*/5 * * * *");

app.Run();
