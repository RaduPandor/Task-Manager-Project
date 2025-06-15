using Moq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class DetailsViewModelFacts
    {
        private readonly Mock<PerformanceMetricsService> mockPerformanceMetricsHelper;

        public DetailsViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsService>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public void ProcessesShouldBeInitialized()
        {
            var viewModel = new DetailsViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotNull(viewModel.Processes);
            Assert.IsType<ObservableCollection<DetailsInfoViewModel>>(viewModel.Processes);
        }

        [Fact]
        public void ProcessesCollectionShouldNotifyOnChange()
        {
            var viewModel = new DetailsViewModel(mockPerformanceMetricsHelper.Object);
            bool itemAdded = false;

            viewModel.Processes.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    itemAdded = true;
                }
            };

            viewModel.Processes.Add(new DetailsInfoViewModel { Name = "TestProcess" });
            Assert.True(itemAdded);
        }
    }
}
