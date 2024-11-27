using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly WindowCommands windowCommands;
        private readonly PerformanceMetricsHelper performanceMetricsHelper;
        private BaseViewModel currentView;
        private bool isMenuVisible = true;
        private bool isLoading;

        public MainWindowViewModel(IPerformanceMetricsHelper performanceMetricsHelper, IWindowCommands windowCommands)
        {
            this.performanceMetricsHelper = (PerformanceMetricsHelper)performanceMetricsHelper;
            this.windowCommands = (WindowCommands)windowCommands;
            MinimizeCommand = windowCommands.MinimizeCommand;
            MaximizeCommand = windowCommands.MaximizeCommand;
            CloseCommand = windowCommands.CloseCommand;
            DragMoveCommand = windowCommands.DragMoveCommand;
            ToggleMenuCommand = new RelayCommand<object>(param => ToggleMenu());
            ShowProcesses = Show(() => new ProcessesViewModel(this.performanceMetricsHelper));
            ShowPerformance = Show(() => new PerformanceViewModel());
            ShowDetails = Show(() => new DetailsViewModel(this.performanceMetricsHelper));
            ShowServices = Show(() => new ServicesViewModel());
            ShowStartup = Show(() => new StartupViewModel());
            ShowAppHistory = Show(() => new AppHistoryViewModel(this.performanceMetricsHelper));
            ShowUsers = Show(() => new UsersViewModel(this.performanceMetricsHelper));
        }

        public ICommand MinimizeCommand { get; }

        public ICommand MaximizeCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand DragMoveCommand { get; }

        public ICommand ToggleMenuCommand { get; }

        public ICommand ShowProcesses { get; }

        public ICommand ShowPerformance { get; }

        public ICommand ShowDetails { get; }

        public ICommand ShowServices { get; }

        public ICommand ShowStartup { get; }

        public ICommand ShowAppHistory { get; }

        public ICommand ShowUsers { get; }

        public BaseViewModel CurrentView
        {
            get => currentView;
            set
            {
                if (currentView is ICancellableViewModel cancellableViewModel)
                {
                    cancellableViewModel.StopMonitoring();
                }

                currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public bool IsMenuVisible
        {
            get => isMenuVisible;
            set
            {
                if (isMenuVisible == value)
                {
                    return;
                }

                isMenuVisible = value;
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(MenuColumnWidth));
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set
            {
                if (isLoading == value)
                {
                    return;
                }

                isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public GridLength MenuColumnWidth => IsMenuVisible ? new GridLength(150) : new GridLength(0);

        private void ToggleMenu()
        {
            IsMenuVisible = !IsMenuVisible;
        }

        private RelayCommand<object> Show(Func<BaseViewModel> createViewModel)
        {
            return new RelayCommand<object>(async _ => await ShowViewAsync(createViewModel));
        }

        private async Task ShowViewAsync(Func<BaseViewModel> createViewModel)
        {
            IsLoading = true;
            CurrentView = createViewModel();
            if (CurrentView is ILoadableViewModel loadableViewModel)
            {
                await loadableViewModel.LoadDataAsync();
            }

            IsLoading = false;
        }
    }
}
