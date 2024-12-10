using System.Threading;
using System.Threading.Tasks;

namespace TaskManager.Services
{
    public interface ILoadableViewModel
    {
        Task OnNavigatedToAsync(CancellationToken rootToken);

        void OnNavigatedFrom();
    }
}