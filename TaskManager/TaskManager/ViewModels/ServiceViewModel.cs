namespace TaskManager.ViewModels
{
    public class ServiceViewModel : BaseViewModel
    {
        private string status;

        public string Name { get; set; }

        public string Id { get; set; }

        public string Description { get; set; }

        public string GroupName { get; set; }

        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
    }
}