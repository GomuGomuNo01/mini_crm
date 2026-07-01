using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;
using MiniCrm.ViewModels;

namespace MiniCrm.Services;

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;

    public ClientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Client>> GetAllAsync(string? search = null)
    {
        var query = _context.Clients
            .Include(c => c.Contracts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(search) ||
                c.Email.Contains(search) ||
                (c.Sector != null && c.Sector.Contains(search)));
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<PagedResult<Client>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 8)
    {
        if (page < 1) page = 1;

        var query = _context.Clients
            .Include(c => c.Contracts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(search) ||
                c.Email.Contains(search) ||
                (c.Sector != null && c.Sector.Contains(search)));
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Client>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<IReadOnlyList<string>> GetSectorsAsync()
    {
        // Liste proposée dans le formulaire = catalogue géré par l'admin.
        return await _context.Sectors
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _context.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task CreateAsync(Client client)
    {
        client.CreatedAt = DateTime.UtcNow;
        await EnsureSectorExistsAsync(client.Sector);
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        var existing = await _context.Clients.FirstOrDefaultAsync(c => c.Id == client.Id);
        if (existing == null) return;

        await EnsureSectorExistsAsync(client.Sector);

        existing.Name = client.Name;
        existing.Email = client.Email;
        existing.Sector = client.Sector;
        existing.Status = client.Status;

        await _context.SaveChangesAsync();
    }

    // Si le secteur saisi (ex. via l'option « Autre » du formulaire) n'existe pas
    // encore dans le catalogue, on l'y ajoute automatiquement.
    private async Task EnsureSectorExistsAsync(string? sectorName)
    {
        if (string.IsNullOrWhiteSpace(sectorName)) return;

        var name = sectorName.Trim();
        var exists = await _context.Sectors.AnyAsync(s => s.Name.ToLower() == name.ToLower());
        if (!exists)
            _context.Sectors.Add(new Sector { Name = name });
    }

    public async Task DeleteAsync(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }
    }
}
