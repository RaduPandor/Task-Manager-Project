using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models;

namespace TaskManager.Services
{
    public interface IServiceManager
    {
        Task<List<ServicesModel>> GetAllServicesAsync(CancellationToken token);

        Task<bool> StartServiceAsync(string serviceName);

        Task<bool> StopServiceAsync(string serviceName);

        Task<bool> RestartServiceAsync(string serviceName);
    }
}
