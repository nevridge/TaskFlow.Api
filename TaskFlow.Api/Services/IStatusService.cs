using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public interface IStatusService
{
    Task<IEnumerable<Status>> GetAllStatusesAsync();
    Task<Status?> GetStatusAsync(int id);
    Task<Status> CreateStatusAsync(Status status);
    Task UpdateStatusAsync(Status status);
    Task DeleteStatusAsync(int id);
}
