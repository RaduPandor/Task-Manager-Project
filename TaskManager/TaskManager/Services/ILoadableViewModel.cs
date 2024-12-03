using System.Threading.Tasks;

namespace TaskManager.Services
{
    public interface ILoadableViewModel
    {
        Task OnNavigatedToAsync();

        void OnNavigatedFrom();
    }
}