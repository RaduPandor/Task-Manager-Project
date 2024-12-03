using Moq;
using System.Collections.Specialized;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class ProcessesViewModelFacts
    {
        private readonly Mock<PerformanceMetricsService> mockPerformanceMetricsHelper;

        public ProcessesViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsService>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public void ProcessesCollectionShouldNotifyOnChange()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object);
            bool itemAdded = false;

            viewModel.Processes.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    itemAdded = true;
                }
            };

            viewModel.Processes.Add(new ProcessModel { Name = "TestProcess" });
            Assert.True(itemAdded);
        }

        [Fact]
        public void ProcessesShouldReflectChangesCorrectly()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object);
            var processModel = new ProcessModel { Name = "TestProcess" };

            viewModel.Processes.Add(processModel);
            var item = viewModel.Processes.FirstOrDefault(p => p.Name == "TestProcess");
            Assert.NotNull(item);
        }
    }
}