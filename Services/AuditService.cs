using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, string? entityLabel, string? details = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var userName = user?.Identity?.Name ?? "Système";
        string? role = null;
        if (user is not null)
        {
            if (user.IsInRole("Admin")) role = "Admin";
            else if (user.IsInRole("User")) role = "User";
        }

        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserName = userName,
            UserRole = role,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityLabel = entityLabel,
            Details = details is { Length: > 500 } ? details[..497] + "…" : details
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;

        var query = _context.AuditLogs.OrderByDescending(a => a.Timestamp);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuditLog>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}
