using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.ViewModels;

namespace TaskManager.Services
{
    public interface IServiceManager
    {
        Task<List<ServiceViewModel>> GetAllServicesAsync(CancellationToken token);

        Task<bool> StartServiceAsync(string serviceName);

        Task<bool> StopServiceAsync(string serviceName);

        Task<bool> RestartServiceAsync(string serviceName);
    }
}
