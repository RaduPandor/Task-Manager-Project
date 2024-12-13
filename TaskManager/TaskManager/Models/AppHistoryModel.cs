namespace TaskManager.Models
{
    public record AppHistoryModel
    {
        public string Name { get; init; }

        public string CPUTime { get; init; }

        public double NetworkUsage { get; init; }
    }
}
