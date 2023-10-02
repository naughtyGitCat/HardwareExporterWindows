using Prometheus;
using HardwareExporterWindows;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// https://stackoverflow.com/questions/7764088/net-console-application-as-windows-service
builder.Host.UseWindowsService(c=>c.ServiceName="HardwareExporter");
//builder.Services.AddHostedService<Y>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

app.Run();
