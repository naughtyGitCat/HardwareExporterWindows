// // 张锐志 2023-10-11
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
namespace HardwareExporterWeb.Tests;

public class XunitLogger<T> : ILogger<T>, IDisposable
{
    private readonly ITestOutputHelper _output;

    public XunitLogger(ITestOutputHelper output)
    {
        _output = output;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine(state is not null? state.ToString() : "") ;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose()
    {
    }
}