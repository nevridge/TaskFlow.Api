using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItem>> GetAllTasksAsync();
    Task<TaskItem?> GetTaskAsync(int id);
    Task<TaskItem> CreateTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(int id);
}
