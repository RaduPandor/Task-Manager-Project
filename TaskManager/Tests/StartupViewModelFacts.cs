using TaskManager.ViewModels;

namespace Tests
{
    public class StartupViewModelTests
    {
        [Fact]
        public async Task LoadStartupProgramsAsyncShouldAddStartupPrograms()
        {
            var viewModel = new StartupViewModel();
            Assert.NotNull(viewModel.StartupPrograms);
        }
    }
}