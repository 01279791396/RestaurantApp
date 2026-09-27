using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services Configuration ---

// 1. DbContext Setup
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity Setup
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Cookie Setup
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// 4. MVC Controllers & Views
builder.Services.AddControllersWithViews();

// 5. External Services
builder.Services.AddHttpClient<IWhatsAppNotifier, WhatsAppNotifier>();

// 6. Caching & Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- Database Seeding (Roles + Default Admin) ---
// لازم يتنفذ مرة واحدة أول ما التطبيق يشتغل عشان يعمل الـ Roles وحساب الأدمن الافتراضي
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services, builder.Configuration);
        // لو الميثود بتاعتك مش static أو اسمها مختلف، بدّل السطر ده بالشكل اللي كنت مستخدمه قبل كده
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// --- HTTP Request Pipeline Configuration ---

// تفعيل شاشة الأخطاء للتطوير لعرض السبب الحقيقي إن وجد بدلاً من إغلاق السيرفر
// ملحوظة مهمة: لازم يتشال السطر ده بعد ما تحل مشكلة النشر، قبل ما الموقع يبقى نهائي
app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Map Area Routes (Admin, Delivery)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Map Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();