using System.Diagnostics;
using TaskManager.Services;

namespace Tests
{
    [TestClass]
    public class PerformanceMetricsServiceTests
    {
        private readonly PerformanceMetricsService _service = new(new NativeMethodsService());

        [TestMethod]
        public async Task GetCpuUsageAsync_ShouldReturnValueBetween0And100()
        {
            var process = Process.GetCurrentProcess();
            var cpuUsage = await _service.GetCpuUsageAsync(process);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(cpuUsage >= 0 && cpuUsage <= 100,
                $"Expected CPU usage between 0 and 100, but got {cpuUsage}");
        }

        [TestMethod]
        public async Task GetNetworkUsageAsync_ShouldReturnNonNegativeValue()
        {
            var networkUsage = await _service.GetNetworkUsageAsync();

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(networkUsage >= 0, $"Expected non-negative network usage, got {networkUsage}");
        }

        [TestMethod]
        public async Task GetDiskUsageAsync_ShouldReturnNonNegativeValue()
        {
            var process = Process.GetCurrentProcess();
            var diskUsage = await _service.GetDiskUsageAsync(process);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(diskUsage >= 0, $"Expected non-negative disk usage, got {diskUsage}");
        }
    }
}
