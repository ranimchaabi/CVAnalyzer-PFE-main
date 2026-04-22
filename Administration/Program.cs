using Administration.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ─── Entity Framework ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Session ──────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".AdminPro.Session";
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<Administration.Options.CandidatMediaOptions>(
    builder.Configuration.GetSection(Administration.Options.CandidatMediaOptions.SectionName));

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Sessions AVANT Authorization
app.UseSession();
app.UseAuthorization();

// ─── Routes ───────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();