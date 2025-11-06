using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Services;

public class TaskService(ITaskRepository repo) : ITaskService
{
    private readonly ITaskRepository _repo = repo;

    public async Task<IEnumerable<TaskItem>> GetAllTasksAsync() =>
        await _repo.GetAllAsync();
    public async Task<TaskItem?> GetTaskAsync(int id) =>
        await _repo.GetByIdAsync(id);
    public async Task<TaskItem> CreateTaskAsync(TaskItem task) =>
        await _repo.AddAsync(task);
    public async Task UpdateTaskAsync(TaskItem task) =>
        await _repo.UpdateAsync(task);
    public async Task DeleteTaskAsync(int id) =>
        await _repo.DeleteAsync(id);
}