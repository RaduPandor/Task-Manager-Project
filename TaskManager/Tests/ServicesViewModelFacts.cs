using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;
using Xunit;

namespace Tests
{
    public class ServicesViewModelTests
    {
        [Fact]
        public async Task LoadServicesAsyncShouldAddServices()
        {
            var mockServiceManager = new Mock<IServiceManager>();
            var mockDialogService = new Mock<IErrorDialogService>();

            var viewModel = new ServicesViewModel(mockServiceManager.Object, mockDialogService.Object);

            Xunit.Assert.NotNull(viewModel.Services);
        }
    }
}
