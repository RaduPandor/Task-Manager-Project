using System;

namespace TaskManager.Models
{
    public class NetworkModel : BaseViewModel
    {
        private double send;
        private double receive;

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string Model { get; set; }

        public string ConnectionType { get; set; }

        public string IPv4Adress { get; set; }

        public string IPv6Adress { get; set; }

        public double Send
        {
            get => send;
            set
            {
                if (Math.Abs(send - value) < 0.1)
                {
                    return;
                }

                send = value;
                OnPropertyChanged(nameof(Send));
            }
        }

        public double Receive
        {
            get => receive;
            set
            {
                if (Math.Abs(receive - value) < 0.1)
                {
                    return;
                }

                receive = value;
                OnPropertyChanged(nameof(Receive));
            }
        }
    }
}
