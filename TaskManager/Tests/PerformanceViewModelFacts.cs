using TaskManager.ViewModels;

namespace Tests
{
    public class PerformanceViewModelTests
    {
        [Fact]
        public void LoadDiskDrivesShouldPopulateDisks()
        {
            var viewModel = new PerformanceViewModel();
            Xunit.Assert.NotEmpty(viewModel.Disks);
        }

        [Fact]
        public void LoadNetworkInterfacesShouldPopulateNetworkInterfaces()
        {
            var viewModel = new PerformanceViewModel();
            Xunit.Assert.NotEmpty(viewModel.NetworkInterfaces);
        }

        [Fact]
        public void LoadGPUShouldPopulateGPUList()
        {
            var viewModel = new PerformanceViewModel();
            Xunit.Assert.NotEmpty(viewModel.GPU);
        }
    }
}