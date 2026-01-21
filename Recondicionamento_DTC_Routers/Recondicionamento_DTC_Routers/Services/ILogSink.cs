using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.Services
{
    public interface ILogSink
    {
        Task LogAsync(string message);
    }
}
