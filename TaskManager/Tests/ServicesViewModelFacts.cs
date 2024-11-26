using TaskManager.ViewModels;

namespace Tests
{
    public class ServicesViewModelTests
    {
        [Fact]
        public async Task LoadServicesAsyncShouldAddServices()
        {
            var viewModel = new ServicesViewModel();
            Assert.NotNull(viewModel.Services);
        }
    }
}