using AutomationTests.Pages;
using Reqnroll;

namespace AutomationTests.Steps
{
    [Binding]
    public class CPUUsageSteps
    {
        private readonly CPUUsagePage page = new();
        private double cpuUsage;

        [Given(@"the Task Manager is running")]
        public void GivenTheTaskManagerIsRunning()
        {
        }

        [When(@"I request the CPU usage")]
        public async Task WhenIRequestTheCpuUsage()
        {
            cpuUsage = await page.GetCpuUsageAsync();
        }

        [Then(@"the value should be between 0 and 100")]
        public void ThenTheValueShouldBeBetween0And100()
        {
            Assert.IsTrue(cpuUsage >= 0 && cpuUsage <= 100,
                $"Expected CPU usage between 0 and 100, but got {cpuUsage}");
        }
    }
}
