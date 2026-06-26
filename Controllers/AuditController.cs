using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCrm.Services;

namespace MiniCrm.Controllers;

[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // GET: /Audit
    public async Task<IActionResult> Index(int page = 1)
    {
        var result = await _auditService.GetPagedAsync(page, pageSize: 20);
        return View(result);
    }
}
