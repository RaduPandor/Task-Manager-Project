using Moq;
using System.Collections.Specialized;
using System.Diagnostics;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class ProcessesViewModelFacts
    {
        private readonly Mock<PerformanceMetricsService> mockPerformanceMetricsHelper;
        private readonly Mock<IProcessProvider> mockProcessProvider;

        public ProcessesViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsService>(MockBehavior.Strict, new NativeMethodsService());
            mockProcessProvider = new Mock<IProcessProvider>(MockBehavior.Strict);
        }

        [Fact]
        public void ProcessesCollectionShouldNotifyOnChange()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object, mockProcessProvider.Object);
            bool itemAdded = false;

            viewModel.Processes.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    itemAdded = true;
                }
            };

            viewModel.Processes.Add(new ProcessViewModel { Name = "TestProcess" });
            Xunit.Assert.True(itemAdded);
        }

        [Fact]
        public void ProcessesShouldReflectChangesCorrectly()
        {
            var viewModel = new ProcessesViewModel(mockPerformanceMetricsHelper.Object, mockProcessProvider.Object);
            var processModel = new ProcessViewModel { Name = "TestProcess" };

            viewModel.Processes.Add(processModel);
            var item = viewModel.Processes.FirstOrDefault(p => p.Name == "TestProcess");
            Xunit.Assert.NotNull(item);
        }
    }
}