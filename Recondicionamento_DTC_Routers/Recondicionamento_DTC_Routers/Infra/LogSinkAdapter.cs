using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;

public sealed class LogSinkAdapter : ILogSink
{
    private readonly UiLogger _logger;

    public LogSinkAdapter(UiLogger logger) => _logger = logger;

 
    public Task LogAsync(string msg) => _logger.LogAsync(msg, toFile: true);
}