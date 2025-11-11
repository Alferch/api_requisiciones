using RequisicionesApi.Interfaces;
using RequisicionesApi.Models;

namespace RequisicionesApi.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly ITimelineRepository _repo;

        public TimelineService(ITimelineRepository repo)
        {
            _repo = repo;
        }

        public Task<IReadOnlyList<TimelineEventDto>> GetAsync(TimelineQuery query, CancellationToken ct = default)
            => _repo.GetAsync(query, ct);

        public Task<IReadOnlyList<TimelineEventDto>> GetByNumeroAsync(string numero, CancellationToken ct = default)
            => _repo.GetByNumeroAsync(numero, ct);
    }
}