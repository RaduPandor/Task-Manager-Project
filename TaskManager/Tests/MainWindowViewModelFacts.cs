using Moq;
using System.Windows;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace Tests
{
    public class MainWindowViewModelFacts
    {
        private readonly PerformanceMetricsService performanceMetricsHelper;
        private readonly ProcessProvider processProvider;
        private readonly Mock<IServiceManager> mockServiceManager;
        private readonly Mock<IErrorDialogService> mockErrorDialogService;
        private readonly MainWindowViewModel mainWindowViewModel;

        public MainWindowViewModelFacts()
        {
            performanceMetricsHelper = new PerformanceMetricsService(new NativeMethodsService());
            processProvider = new ProcessProvider();
            mockServiceManager = new Mock<IServiceManager>();
            mockErrorDialogService = new Mock<IErrorDialogService>();

            mainWindowViewModel = new MainWindowViewModel(
                performanceMetricsHelper,
                processProvider,
                mockServiceManager.Object,
                mockErrorDialogService.Object
            );
        }

        [Fact]
        public void ToggleMenuCommandIsMenuVisible()
        {
            bool initialVisibility = mainWindowViewModel.IsMenuVisible;
            mainWindowViewModel.ToggleMenuCommand.Execute(null);
            Xunit.Assert.NotEqual(initialVisibility, mainWindowViewModel.IsMenuVisible);
        }

        [Fact]
        public void MenuColumnWidthMenuIsVisible()
        {
            mainWindowViewModel.IsMenuVisible = true;
            var width = mainWindowViewModel.MenuColumnWidth;
            Xunit.Assert.Equal(new GridLength(150), width);
        }

        [Fact]
        public void MenuColumnWidthMenuIsHidden()
        {
            mainWindowViewModel.IsMenuVisible = false;
            var width = mainWindowViewModel.MenuColumnWidth;
            Xunit.Assert.Equal(new GridLength(0), width);
        }

        [Fact]
        public void ToggleMenuCommandPropertyChanged()
        {
            bool isMenuVisibleChanged = false;
            bool menuColumnWidthChanged = false;

            mainWindowViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(mainWindowViewModel.IsMenuVisible))
                {
                    isMenuVisibleChanged = true;
                }
                if (args.PropertyName == nameof(mainWindowViewModel.MenuColumnWidth))
                {
                    menuColumnWidthChanged = true;
                }
            };

            mainWindowViewModel.ToggleMenuCommand.Execute(null);
            Xunit.Assert.True(isMenuVisibleChanged);
            Xunit.Assert.True(menuColumnWidthChanged);
        }
    }
}
