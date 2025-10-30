using System.Collections.Generic;

namespace MiniPM.DTOs
{
    public record ProjectDto(int Id, string Title, string? Description, DateTime CreatedAt);
    public record ProjectWithTasksDto(int Id, string Title, string? Description, DateTime CreatedAt, IEnumerable<TaskDto> Tasks);
    public record CreateProjectDto(string Title, string? Description);
}
