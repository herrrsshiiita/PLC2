namespace MiniPM.DTOs
{
    public record TaskDto(int Id, string Title, DateTime? DueDate, bool IsCompleted, DateTime CreatedAt);
    public record CreateTaskDto(string Title, DateTime? DueDate);
    public record UpdateTaskDto(string? Title, DateTime? DueDate, bool? IsCompleted);
    public record ScheduledTaskDto(int TaskId, string Title, DateTime ScheduledDate);
    public record ScheduleRequest(DateTime? StartDate, int DaysPerTask);
}
