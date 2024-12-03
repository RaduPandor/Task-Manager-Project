using Moq;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class UsersViewModelTests
    {
        private readonly Mock<PerformanceMetricsService> mockPerformanceMetricsHelper;

        public UsersViewModelTests()
        {
            mockPerformanceMetricsHelper = new Mock<PerformanceMetricsService>(MockBehavior.Strict, new NativeMethodsService());
        }

        [Fact]
        public async Task LoadUsersAsyncShouldAddUsers()
        {
            var viewModel = new UsersViewModel(mockPerformanceMetricsHelper.Object);
            Assert.NotEmpty(viewModel.Users);
        }
    }
}
