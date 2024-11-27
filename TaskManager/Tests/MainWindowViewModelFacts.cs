using System.Windows;
using TaskManager.Services;
using TaskManager.ViewModels;
using Xunit;

namespace Tests
{
    public class MainWindowViewModelFacts
    {
        private readonly PerformanceMetricsHelper performanceMetricsHelper;
        private readonly WindowCommands windowCommands;
        private readonly MainWindowViewModel mainWindowViewModel;

        public MainWindowViewModelFacts()
        {
            performanceMetricsHelper = new PerformanceMetricsHelper(new NativeMethodsService());
            windowCommands = new WindowCommands();
            mainWindowViewModel = new MainWindowViewModel(performanceMetricsHelper, windowCommands);
        }

        [Fact]
        public void ToggleMenuCommandIsMenuVisible()
        {
            bool initialVisibility = mainWindowViewModel.IsMenuVisible;
            mainWindowViewModel.ToggleMenuCommand.Execute(null);
            Assert.NotEqual(initialVisibility, mainWindowViewModel.IsMenuVisible);
        }

        [Fact]
        public void MenuColumnWidthMenuIsVisible()
        {
            mainWindowViewModel.IsMenuVisible = true;
            var width = mainWindowViewModel.MenuColumnWidth;
            Assert.Equal(new GridLength(150), width);
        }

        [Fact]
        public void MenuColumnWidthMenuIsHidden()
        {
            mainWindowViewModel.IsMenuVisible = false;
            var width = mainWindowViewModel.MenuColumnWidth;
            Assert.Equal(new GridLength(0), width);
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
            Assert.True(isMenuVisibleChanged);
            Assert.True(menuColumnWidthChanged);
        }
    }
}
