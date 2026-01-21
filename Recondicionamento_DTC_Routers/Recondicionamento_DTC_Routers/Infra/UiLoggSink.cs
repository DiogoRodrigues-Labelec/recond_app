using System.Threading.Tasks;
using Recondicionamento_DTC_Routers.Services;

namespace Recondicionamento_DTC_Routers.Infra
{
    public sealed class UiLoggerSink : ILogSink
    {
        private readonly UiLogger _logger;

        public UiLoggerSink(UiLogger logger) => _logger = logger;

        public Task LogAsync(string message) => _logger.LogAsync(message, true);
    }
}
