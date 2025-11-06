using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;

namespace TaskFlow.Api.Services
{
    public class StatusService(IStatusRepository repo) : IStatusService
    {
        private readonly IStatusRepository _repo = repo;

        public async Task<IEnumerable<Status>> GetAllStatusesAsync() =>
            await _repo.GetAllAsync();

        public async Task<Status?> GetStatusAsync(int id) =>
            await _repo.GetByIdAsync(id);

        public async Task<Status> CreateStatusAsync(Status status) =>
            await _repo.AddAsync(status);

        public async Task UpdateStatusAsync(Status status) =>
            await _repo.UpdateAsync(status);

        public async Task DeleteStatusAsync(int id) =>
            await _repo.DeleteAsync(id);
    }
}
