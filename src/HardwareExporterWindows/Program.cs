using Prometheus;
using HardwareExporterWindows.Configuration;
using HardwareExporterWindows.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<HardwareMonitorOptions>(
    builder.Configuration.GetSection(HardwareMonitorOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register hardware monitor as singleton hosted service
builder.Services.AddSingleton<HardwareMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HardwareMonitorService>());

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
