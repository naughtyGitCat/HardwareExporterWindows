using Prometheus;
using HardwareExporterWindows.Configuration;
using HardwareExporterWindows.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<HardwareMonitorOptions>(
    builder.Configuration.GetSection(HardwareMonitorOptions.SectionName));
builder.Services.Configure<SmartMonitorOptions>(
    builder.Configuration.GetSection(SmartMonitorOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register hardware monitor as singleton hosted service
builder.Services.AddSingleton<HardwareMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HardwareMonitorService>());

// SMART monitor: collects via smartctl subprocess (covers SAS HBA-attached SATA disks
// that LibreHardwareMonitor cannot reach via ATA pass-through).
builder.Services.AddSingleton<SmartctlInvoker>();
builder.Services.AddSingleton<SmartMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SmartMonitorService>());

// Configure Windows Service
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "HardwareExporter";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection if configured
if (builder.Configuration.GetValue<bool>("UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
// Note: Prometheus .NET metrics are now included in the /metrics endpoint via MetricsController

app.Run();
