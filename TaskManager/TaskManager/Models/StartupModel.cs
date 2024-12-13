namespace TaskManager.Models
{
    public record StartupModel
    {
        public string Name { get; init; }

        public string Status { get; init; }

        public string Publisher { get; init; }

        public string StartupImpact { get; init; }
    }
}
