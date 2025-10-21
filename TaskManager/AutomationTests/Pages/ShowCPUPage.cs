using System.Diagnostics;
using System.Windows.Automation;

namespace AutomationTests.Pages
{
    public class ShowCPUPage
    {
        private Process? appProcess;
        private AutomationElement? mainWindow;

        public void LaunchApp()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"E:\Git Projects\TaskManager\TaskManager\TaskManager\bin\Debug\net6.0-windows7.0\TaskManager.exe",
                UseShellExecute = true
            };

            appProcess = Process.Start(startInfo);
            Thread.Sleep(2000);

            mainWindow = AutomationElement.FromHandle(appProcess.MainWindowHandle);
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Failed to find the Task Manager main window.");
            }
        }

        public void ClickPerformanceButton() => ClickButtonByAutomationId("PerformanceButton", "PerformanceButton");

        public void ClickCpuButton() => ClickButtonByAutomationId("CPUButton", "CPUButton");

        private void ClickButtonByAutomationId(string automationId, string friendlyName)
        {
            var element = mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId))
                ?? throw new InvalidOperationException($"{friendlyName} not found.");

            ClickElement(element);
            Thread.Sleep(2000);
        }


        private void ClickElement(AutomationElement element)
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
            {
                ((InvokePattern)pattern).Invoke();
            }
            else
            {
                throw new InvalidOperationException("Element does not support InvokePattern.");
            }
        }

        public double GetCpuUsage()
        {
            var cpuLabel = mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "CpuUsageLabel"))
                ?? throw new InvalidOperationException("CPU usage label not found.");

            var text = cpuLabel.Current.Name.Replace("%", "").Trim();
            if (double.TryParse(text, out double value) && value >= 0 && value <= 100)
            {
                return value;
            }

            throw new InvalidOperationException($"CPU usage invalid value: '{text}'");
        }

    }
}