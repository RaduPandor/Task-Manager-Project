using Moq;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class AppHistoryViewModelFacts
    {
        private readonly Mock<PerformanceMetricsService> mockPerformanceMetricsHelper;

        public AppHistoryViewModelFacts()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsService>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public void AppHistoryEmptyCollection()
        {
            var viewModel = new AppHistoryViewModel(mockPerformanceMetricsHelper.Object);
            Xunit.Assert.NotNull(viewModel.AppHistory);
            Xunit.Assert.Empty(viewModel.AppHistory);
        }
    }
}
