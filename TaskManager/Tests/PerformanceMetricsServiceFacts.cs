using System.Diagnostics;
using TaskManager.Services;

namespace Tests
{
    public class PerformanceMetricsHelperTests
    {
        private readonly PerformanceMetricsService performanceMetricsHelper = new(new NativeMethodsService());
        [Fact]
        public async Task GetCpuUsageAsyncReturnsExpectedValue()
        {
            var process = Process.GetCurrentProcess();
            var cpuUsage = await performanceMetricsHelper.GetCpuUsageAsync(process);
            Assert.InRange(cpuUsage, 0, 100);
        }

        [Fact]
        public async Task GetNetworkUsageAsyncReturnsExpectedValue()
        {
            var networkUsage = await performanceMetricsHelper.GetNetworkUsageAsync();
            Assert.InRange(networkUsage, 0, double.MaxValue);
        }

        [Fact]
        public async Task GetDiskUsageAsyncReturnsExpectedValue()
        {
            var performanceMetricsHelper = new PerformanceMetricsService(new NativeMethodsService());
            var process = Process.GetCurrentProcess();
            var diskUsage = await performanceMetricsHelper.GetDiskUsageAsync(process);
            Assert.InRange(diskUsage, 0, double.MaxValue);
        }
    }
}
