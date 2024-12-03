using System.Diagnostics;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public interface IPerformanceMetricsService
    {
        Task<double> GetCpuUsageAsync(Process process);

        Task<double> GetDiskUsageAsync(Process process);

        Task<double> GetNetworkUsageAsync();

        string GetProcessStatus(Process process);

        Task<string> GetProcessOwnerAsync(int processId);

        string GetProcessOwner(int processId);
    }
}
