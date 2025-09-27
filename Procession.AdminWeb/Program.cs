using Procession.Server.Common;
using Procession.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Procession.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Procession services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=procession_admin.db;Cache=Shared";

builder.Services.TryAddSingleton<IStorage>(provider => new StorageSqlite(connectionString));
builder.Services.TryAddSingleton<QueueOptions>(provider => new QueueOptions { DefaultTranactionTimeoutInMinutes = 10 });
builder.Services.AddScoped<AdminService>();

var app = builder.Build();

// Initialize storage
using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
    await storage.InitializeStorage(deleteExistingData: false);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
