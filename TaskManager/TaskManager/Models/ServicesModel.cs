namespace TaskManager.Models
{
    public record ServicesModel
    {
        public string Name { get; init; }

        public string Id { get; init; }

        public string Status { get; init; }

        public string Description { get; init; }

        public string GroupName { get; init; }
    }
}
