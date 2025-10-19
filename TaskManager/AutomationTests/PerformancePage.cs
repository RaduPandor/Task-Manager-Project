using System.Diagnostics;
using TaskManager.Services;

namespace AutomationTests
{
    public class PerformancePage
    {
        private readonly PerformanceMetricsService service = new(new NativeMethodsService());

        public async Task<double> GetCpuUsageAsync()
        {
            var process = Process.GetCurrentProcess();
            return await service.GetCpuUsageAsync(process);
        }
    }

}
