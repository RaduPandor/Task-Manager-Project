using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Moq;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class DetailsViewModelFacts
    {
        private readonly Mock<PerformanceMetricsHelper> mockPerformanceMetricsHelper;

        public DetailsViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsHelper>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public void ProcessesShouldBeInitialized()
        {
            var viewModel = new DetailsViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotNull(viewModel.Processes);
            Assert.IsType<ObservableCollection<DetailsModel>>(viewModel.Processes);
        }

        [Fact]
        public void ProcessesViewShouldBeInitialized()
        {
            var viewModel = new DetailsViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotNull(viewModel.ProcessesView);
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

            viewModel.Processes.Add(new DetailsModel { Name = "TestProcess" });
            Assert.True(itemAdded);
        }
    }
}
