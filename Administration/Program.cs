using Administration.Data;
using Administration.Options;
using Administration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ─── MVC ─────────────────────────────────────
builder.Services.AddControllersWithViews();

// ─── DB ──────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── SESSION ─────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".AdminPro.Session";
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<CandidatMediaOptions>(
    builder.Configuration.GetSection(CandidatMediaOptions.SectionName));

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

// ─── ERROR HANDLING ─────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ─── STATIC FILES (wwwroot admin) ───────────
app.UseStaticFiles();

// ─── STATIC FILES (CVs from Candidat project) ───────────
var cvPath = Path.GetFullPath(
    Path.Combine(Directory.GetCurrentDirectory(), "..", "Candidat", "wwwroot", "uploads","cvs")
);

// ⚠️ DEBUG (optionnel mais utile)
// Console.WriteLine(cvPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(cvPath),
    RequestPath = "/uploads/cvs"
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

// ─── ROUTES ─────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();