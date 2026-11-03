using SalesPulse.Business.Services.Dashboard;
using SalesPulse.Business.Services.Order;
using SalesPulse.Business.Services.Sales;
using SalesPulse.DataAccess.Connection;
using SalesPulse.DataAccess.Repositories.Dashboard;
using SalesPulse.DataAccess.Repositories.Order;
using SalesPulse.DataAccess.Repositories.Sales;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ---- Veri erişimi (DataAccess) ----
var connectionString = builder.Configuration.GetConnectionString("SqlConnection")
    ?? throw new InvalidOperationException("appsettings.json içinde 'SqlConnection' bulunamadı.");

builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISalesRepository, SalesRepository>();

// ---- İş katmanı (Business) ----
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISalesService, SalesService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Uygulama açılınca doğrudan Dashboard gelsin
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
