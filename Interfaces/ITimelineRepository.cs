using RequisicionesApi.Models;

namespace RequisicionesApi.Interfaces
{
    public interface ITimelineRepository
    {
        Task<IReadOnlyList<TimelineEventDto>> GetAsync(TimelineQuery query, CancellationToken ct = default);
        Task<IReadOnlyList<TimelineEventDto>> GetByNumeroAsync(string numero, CancellationToken ct = default);
    }
}


 