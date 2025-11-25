using Microsoft.EntityFrameworkCore;
using CitaFacil.Data;
using CitaFacil.Data.Seed;
using CitaFacil.Services.Security;
using CitaFacil.Services.Usuarios;
using CitaFacil.Services.Notifications;
using CitaFacil.Services.DatabaseBackup;
using CitaFacil.Services.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IUsuarioPerfilService, UsuarioPerfilService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.Configure<DatabaseBackupOptions>(builder.Configuration.GetSection("DatabaseBackup"));
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
builder.Services.AddHostedService<DatabaseBackupHostedService>();

// --- Configuración de Autenticación por Cookies ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // La página a la que te redirige si no estás logueado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Tiempo de la sesión
        options.SlidingExpiration = true;
        options.AccessDeniedPath = "/Home/AccessDenied"; // Página opcional si no tienes el ROL
    });
// --- Fin del bloque de Autenticación ---

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        DataSeeder.SeedAsync(context, passwordHasher).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante la inicialización de datos.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
