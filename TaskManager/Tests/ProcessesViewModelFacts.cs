using System.Collections.Specialized;
using Moq;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class ProcessesViewModelFacts
    {
        private readonly Mock<PerformanceMetricsHelper> mockPerformanceMetricsHelper;

        public ProcessesViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsHelper>(MockBehavior.Strict, new NativeMethodsService());
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
        public void ProcessesViewIsNotNull()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotNull(viewModel.ProcessesView);
        }

        [Fact]
        public void ProcessesViewShouldReflectChangesInProcesses()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object);
            var processModel = new ProcessModel { Name = "TestProcess" };
            viewModel.Processes.Add(processModel);
            var item = viewModel.ProcessesView.Cast<ProcessModel>().FirstOrDefault(p => p.Name == "TestProcess");
            Assert.NotNull(item);
        }
    }
}
