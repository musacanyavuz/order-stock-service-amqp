using System.Threading.Tasks;

namespace Shared.Logging
{
    public interface IMongoLogService
    {
        Task AddLog(string requestId, string service, string message, string type = "Info");
    }
}
