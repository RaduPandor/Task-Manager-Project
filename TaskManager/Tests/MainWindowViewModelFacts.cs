using System.Windows;
using TaskManager.ViewModels;

namespace Tests
{
    public class MainWindowViewModelFacts
    {
        [Fact]
        public void ToggleMenuCommandIsMenuVisible()
        {
            var viewModel = new MainWindowViewModel();
            bool initialVisibility = viewModel.IsMenuVisible;
            viewModel.ToggleMenuCommand.Execute(null);
            Assert.NotEqual(initialVisibility, viewModel.IsMenuVisible);
        }

        [Fact]
        public void MenuColumnWidthMenuIsVisible()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.IsMenuVisible = true;
            var width = viewModel.MenuColumnWidth;
            Assert.Equal(new GridLength(150), width);
        }

        [Fact]
        public void MenuColumnWidthMenuIsHidden()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.IsMenuVisible = false;
            var width = viewModel.MenuColumnWidth;
            Assert.Equal(new GridLength(0), width);
        }

        [Fact]
        public void ShowProcessesCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowProcesses.Execute(null);
            Assert.IsType<ProcessesViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowPerformanceCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowPerformance.Execute(null);
            Assert.IsType<PerformanceViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowDetailsCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowDetails.Execute(null);
            Assert.IsType<DetailsViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowUsersCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowUsers.Execute(null);
            Assert.IsType<UsersViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowAppHistoryCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowAppHistory.Execute(null);
            Assert.IsType<AppHistoryViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowStartupCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowStartup.Execute(null);
            Assert.IsType<StartupViewModel>(viewModel.CurrentView);
        }

        [Fact]
        public void ShowServicesCommand()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.ShowServices.Execute(null);
            Assert.IsType<ServicesViewModel>(viewModel.CurrentView);
        }


        [Fact]
        public void SettingCurrentView()
        {
            var viewModel = new MainWindowViewModel();
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.CurrentView))
                {
                    propertyChangedRaised = true;
                }
            };

            viewModel.ShowProcesses.Execute(null);
            Assert.True(propertyChangedRaised);
        }

        [Fact]
        public void ToggleMenuCommandPropertyChanged()
        {
            var viewModel = new MainWindowViewModel();
            bool isMenuVisibleChanged = false;
            bool menuColumnWidthChanged = false;

            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsMenuVisible))
                {
                    isMenuVisibleChanged = true;
                }
                if (args.PropertyName == nameof(viewModel.MenuColumnWidth))
                {
                    menuColumnWidthChanged = true;
                }
            };

            viewModel.ToggleMenuCommand.Execute(null);
            Assert.True(isMenuVisibleChanged);
            Assert.True(menuColumnWidthChanged);
        }
    }
}