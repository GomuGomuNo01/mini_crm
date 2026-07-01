using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, int? entityId, string? entityLabel, string? details = null);
    Task<PagedResult<AuditLog>> GetPagedAsync(int page = 1, int pageSize = 20);
}
