using Reqnroll;
using AutomationTests.Pages;

namespace AutomationTests.Steps
{
    [Binding]
    public class ShowCPUSteps
    {
        private readonly ShowCPUPage page = new();

        [Given(@"the Task Manager is open")]
        public void GivenTheTaskManagerIsOpen() => page.LaunchApp();

        [When(@"I navigate to the Performance tab and select CPU")]
        public void WhenINavigateToThePerformanceTabAndSelectCPU()
        {
            page.ClickPerformanceButton();
            page.ClickCpuButton();
        }

        [Then(@"the displayed CPU usage should be between 0 and 100")]
        public void ThenTheDisplayedCPUUsageShouldBeBetween0And100()
        {
            double cpuUsage = page.GetCpuUsage();
            Assert.IsTrue(cpuUsage >= 0 && cpuUsage <= 100,
                $"Expected CPU usage between 0 and 100, but got {cpuUsage}");
        }
    }

}
