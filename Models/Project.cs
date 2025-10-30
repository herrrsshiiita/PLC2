namespace MiniPM.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public int UserId { get; set; }
        public List<ProjectTask> Tasks { get; set; } = new();
    }
}
