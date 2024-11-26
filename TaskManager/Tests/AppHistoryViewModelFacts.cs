using Moq;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class AppHistoryViewModelFacts
    {
        private readonly Mock<PerformanceMetricsHelper> mockPerformanceMetricsHelper;

        public AppHistoryViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsHelper>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public void AppHistoryEmptyCollection()
        {
            var viewModel = new AppHistoryViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotNull(viewModel.AppHistory);
            Assert.Empty(viewModel.AppHistory);
        }
    }
}
