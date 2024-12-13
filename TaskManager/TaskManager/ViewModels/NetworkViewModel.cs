using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Services;

namespace TaskManager.ViewModels
{
    public class NetworkViewModel : BaseViewModel, ICancellableViewModel, IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private PerformanceCounter networkSentCounter;
        private PerformanceCounter networkReceivedCounter;
        private ulong previousBytesSent;
        private ulong previousBytesReceived;

        public NetworkViewModel(string adapterName)
        {
            AdapterName = adapterName;
            NetworkModels = new ObservableCollection<NetworkInfoViewModel>();
            NetworkUsageSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<ObservableValue> { new ObservableValue(0) },
                    PointGeometry = null
                }
            };
            cancellationTokenSource = new CancellationTokenSource();
            LoadStaticNetworkMetrics();
            InitializePerformanceCounters();
            LoadDynamicNetworkMetricsAsync(cancellationTokenSource.Token);
        }

        public string AdapterName { get; }

        public ObservableCollection<NetworkInfoViewModel> NetworkModels { get; }

        public SeriesCollection NetworkUsageSeries { get; }

        public NetworkInfoViewModel LatestNetworkModel => NetworkModels.LastOrDefault();

        public void StopMonitoring()
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                StopMonitoring();
                networkSentCounter?.Dispose();
                networkReceivedCounter?.Dispose();
            }

            disposed = true;
        }

        private void InitializePerformanceCounters()
        {
            networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", AdapterName);
            networkReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", AdapterName);
        }

        private void LoadStaticNetworkMetrics()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = networkInterfaces.FirstOrDefault(nic => nic.Name == AdapterName);
            var networkMetrics = new NetworkInfoViewModel
            {
                DisplayName = AdapterName,
                Description = networkInterface.Description,
                IPv4Adress = GetIPv4Address(),
                IPv6Adress = GetIPv6Address(),
                ConnectionType = GetConnectionType()
            };

            App.Current.Dispatcher.Invoke(() =>
            {
                NetworkModels.Add(networkMetrics);
                OnPropertyChanged(nameof(LatestNetworkModel));
            });
        }

        private async Task LoadDynamicNetworkMetricsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                App.Current.Dispatcher.Invoke(UpdateNetworkMetrics);
                await Task.Delay(1000, token);
            }
        }

        private void UpdateNetworkMetrics()
        {
            var networkMetrics = LatestNetworkModel;
            if (networkMetrics == null)
            {
                return;
            }

            var networkInterface = GetNetworkInterface();
            if (networkInterface == null)
            {
                return;
            }

            UpdateMetrics(networkMetrics, networkInterface.GetIPv4Statistics());
        }

        private NetworkInterface GetNetworkInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic => nic.Name == AdapterName && nic.OperationalStatus == OperationalStatus.Up);
        }

        private void UpdateMetrics(NetworkInfoViewModel networkMetrics, IPv4InterfaceStatistics stats)
        {
            ulong currentBytesSent = (ulong)stats.BytesSent;
            ulong currentBytesReceived = (ulong)stats.BytesReceived;

            if (previousBytesSent > 0)
            {
                networkMetrics.Send = ((currentBytesSent - previousBytesSent) * 8) / 1000000;
            }

            if (previousBytesReceived > 0)
            {
                networkMetrics.Receive = ((currentBytesReceived - previousBytesReceived) * 8) / 1000000;
            }

            previousBytesSent = currentBytesSent;
            previousBytesReceived = currentBytesReceived;
            UpdateChartData(networkMetrics);
        }

        private void UpdateChartData(NetworkInfoViewModel networkMetrics)
        {
            double throughput = networkMetrics.Receive + networkMetrics.Send;
            NetworkUsageSeries[0].Values.Add(new ObservableValue(throughput));
            if (NetworkUsageSeries[0].Values.Count > 60)
            {
                NetworkUsageSeries[0].Values.RemoveAt(0);
            }

            OnPropertyChanged(nameof(LatestNetworkModel));
            OnPropertyChanged(nameof(NetworkUsageSeries));
        }

        private string GetIPv4Address()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = networkInterfaces.FirstOrDefault(nic => nic.Name == AdapterName);

            if (networkInterface != null)
            {
                var ip = networkInterface.GetIPProperties();
                var ipv4 = ip.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ipv4 != null ? ipv4.Address.ToString() : " ";
            }

            return " ";
        }

        private string GetIPv6Address()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = networkInterfaces.FirstOrDefault(nic => nic.Name == AdapterName);

            if (networkInterface != null)
            {
                var ip = networkInterface.GetIPProperties();
                var ipv6 = ip.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                return ipv6 != null ? ipv6.Address.ToString() : " ";
            }

            return " ";
        }

        private string GetConnectionType()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = networkInterfaces.FirstOrDefault(nic => nic.Name == AdapterName);

            if (networkInterface != null)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    return "Ethernet";
                }
                else if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    return "Wireless";
                }
            }

            return " ";
        }
    }
}
