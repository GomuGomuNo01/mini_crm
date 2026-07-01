using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public class SectorService : ISectorService
{
    private readonly ApplicationDbContext _context;

    public SectorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Sector>> GetAllAsync() =>
        await _context.Sectors.OrderBy(s => s.Name).ToListAsync();

    public async Task<PagedResult<Sector>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 8)
    {
        if (page < 1) page = 1;

        var query = _context.Sectors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(s => s.Name.Contains(search));
        }

        query = query.OrderBy(s => s.Name);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Sector>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<string>> GetNamesAsync() =>
        await _context.Sectors.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();

    public async Task<Sector?> GetByIdAsync(int id) =>
        await _context.Sectors.FindAsync(id);

    public async Task<int> GetClientCountAsync(string sectorName) =>
        await _context.Clients.CountAsync(c => c.Sector == sectorName);

    public async Task<(bool Ok, string? Error)> CreateAsync(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Le nom du secteur est obligatoire.");

        var exists = await _context.Sectors.AnyAsync(s => s.Name.ToLower() == name.ToLower());
        if (exists)
            return (false, "Ce secteur existe déjà.");

        _context.Sectors.Add(new Sector { Name = name });
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, string? OldName)> UpdateAsync(int id, string name)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            return (false, "Le nom du secteur est obligatoire.", null);

        var sector = await _context.Sectors.FindAsync(id);
        if (sector == null)
            return (false, "Secteur introuvable.", null);

        var duplicate = await _context.Sectors.AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower());
        if (duplicate)
            return (false, "Un autre secteur porte déjà ce nom.", null);

        var oldName = sector.Name;
        if (oldName != name)
        {
            // Propager le renommage aux clients utilisant l'ancien nom.
            var clients = await _context.Clients.Where(c => c.Sector == oldName).ToListAsync();
            foreach (var c in clients) c.Sector = name;

            sector.Name = name;
            await _context.SaveChangesAsync();
        }

        return (true, null, oldName);
    }

    public async Task<string?> DeleteAsync(int id)
    {
        var sector = await _context.Sectors.FindAsync(id);
        if (sector == null) return null;

        var name = sector.Name;
        _context.Sectors.Remove(sector);
        await _context.SaveChangesAsync();
        return name;
    }
}
