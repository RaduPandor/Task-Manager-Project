using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class ServicesViewModel : BaseViewModel, ILoadableViewModel
    {
        private readonly IServiceManager serviceManager;
        private readonly IErrorDialogService dialogService;

        private CancellationTokenSource linkedTokenSource;
        private Task runningTask;
        private ServiceViewModel selectedService;

        public ServicesViewModel(IServiceManager serviceManager, IErrorDialogService dialogService)
        {
            this.serviceManager = serviceManager;
            this.dialogService = dialogService;

            Services = new ObservableCollection<ServiceViewModel>();

            StartServiceCommand = new RelayCommand<object>(_ => StartServiceAsync(), _ => CanStartService());
            StopServiceCommand = new RelayCommand<object>(_ => StopServiceAsync(), _ => CanStopService());
            RestartServiceCommand = new RelayCommand<object>(_ => RestartServiceAsync(), _ => CanRestartService());
        }

        public ObservableCollection<ServiceViewModel> Services { get; }

        public ServiceViewModel SelectedService
        {
            get => selectedService;
            set
            {
                selectedService = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand StartServiceCommand { get; }

        public ICommand StopServiceCommand { get; }

        public ICommand RestartServiceCommand { get; }

        public async Task OnNavigatedToAsync(CancellationToken rootToken)
        {
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(rootToken);
            var token = linkedTokenSource.Token;
            runningTask = LoadDataAsync(token);
            await runningTask;
        }

        public void OnNavigatedFrom()
        {
            linkedTokenSource?.Cancel();
            linkedTokenSource?.Dispose();
            linkedTokenSource = null;

            Services.Clear();
            runningTask = null;
        }

        private async Task LoadDataAsync(CancellationToken token)
        {
            var serviceList = await serviceManager.GetAllServicesAsync(token);

            App.Current.Dispatcher.Invoke(() =>
            {
                Services.Clear();
                foreach (var service in serviceList)
                {
                    Services.Add(service);
                }
            });
        }

        private bool CanStartService() => SelectedService?.Status == "Stopped";

        private bool CanStopService() => SelectedService?.Status == "Running";

        private bool CanRestartService() => SelectedService?.Status == "Running";

        private async Task StartServiceAsync()
        {
            if (SelectedService == null)
            {
                return;
            }

            var result = await serviceManager.StartServiceAsync(SelectedService.Name);
            if (result)
            {
                UpdateServiceStatus(SelectedService.Name, "Running");
            }
            else
            {
                dialogService.ShowError("Failed to start service. Administrator required.");
            }
        }

        private async Task StopServiceAsync()
        {
            if (SelectedService == null)
            {
                return;
            }

            var result = await serviceManager.StopServiceAsync(SelectedService.Name);
            if (result)
            {
                UpdateServiceStatus(SelectedService.Name, "Stopped");
            }
            else
            {
                dialogService.ShowError("Failed to stop service. Administrator required.");
            }
        }

        private async Task RestartServiceAsync()
        {
            if (SelectedService == null)
            {
                return;
            }

            var result = await serviceManager.RestartServiceAsync(SelectedService.Name);
            if (result)
            {
                UpdateServiceStatus(SelectedService.Name, "Running");
            }
            else
            {
                dialogService.ShowError("Failed to restart service. Administrator required.");
            }
        }

        private void UpdateServiceStatus(string serviceName, string newStatus)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var service = Services.FirstOrDefault(s => s.Name == serviceName);
                if (service == null)
                {
                    return;
                }

                service.Status = newStatus;
                CommandManager.InvalidateRequerySuggested();
            });
        }
    }
}
