using Reqnroll;
using TaskManager.Services;
using System.Diagnostics;

namespace AutomationTests
{
    [Binding]
    public class PerformanceSteps
    {
        private readonly PerformanceMetricsService service = new(new NativeMethodsService());
        private double cpuUsage;

        [Given(@"the Task Manager is running")]
        public void GivenTheTaskManagerIsRunning()
        {
        }

        [When(@"I request the CPU usage")]
        public async Task WhenIRequestTheCpuUsage()
        {
            var process = Process.GetCurrentProcess();
            cpuUsage = await service.GetCpuUsageAsync(process);
        }

        [Then(@"the value should be between 0 and 100")]
        public void ThenTheValueShouldBeBetween0And100()
        {
            Assert.IsTrue(cpuUsage >= 0 && cpuUsage <= 100,
                $"Expected CPU usage between 0 and 100, but got {cpuUsage}");
        }
    }
}
