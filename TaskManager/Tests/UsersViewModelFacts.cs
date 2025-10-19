using Moq;
using System.Diagnostics;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class UsersViewModelTests
    {
        [Fact]
        public async Task LoadUsersAsyncAddsUsersCorrectly()
        {
            var mockPerf = new Mock<IPerformanceMetricsService>();
            var mockProcessProvider = new Mock<IProcessProvider>();
            var realProcesses = Process.GetProcesses().Take(2).ToArray();

            mockProcessProvider.Setup(p => p.GetProcesses()).Returns(realProcesses);

            mockPerf.Setup(p => p.GetProcessOwner(It.IsAny<int>())).Returns("testuser");

            var vm = new UsersViewModel(mockPerf.Object, mockProcessProvider.Object);
            await vm.OnNavigatedToAsync(CancellationToken.None);

            Xunit.Assert.Equal(1, vm.Users.Count);
            Xunit.Assert.Equal("testuser", vm.Users[0].UserName);
            Xunit.Assert.Equal(2, vm.Users[0].Processes.Count);
        }
    }
}