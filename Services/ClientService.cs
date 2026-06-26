using Microsoft.EntityFrameworkCore;
using MiniCrm.Data;
using MiniCrm.Models;

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

    public async Task<Client?> GetByIdAsync(int id)
    {
        return await _context.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task CreateAsync(Client client)
    {
        client.CreatedAt = DateTime.UtcNow;
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
    }

    public async Task<string?> UpdateAsync(Client client)
    {
        var existing = await _context.Clients.FirstOrDefaultAsync(c => c.Id == client.Id);
        if (existing == null) return null;

        var changes = new List<string>();
        if (existing.Name != client.Name)
            changes.Add($"Nom : « {existing.Name} » → « {client.Name} »");
        if (existing.Email != client.Email)
            changes.Add($"Email : « {existing.Email} » → « {client.Email} »");
        if (existing.Sector != client.Sector)
            changes.Add($"Secteur : « {existing.Sector ?? "—"} » → « {client.Sector ?? "—"} »");
        if (existing.Status != client.Status)
            changes.Add($"Statut : « {existing.Status} » → « {client.Status} »");

        existing.Name = client.Name;
        existing.Email = client.Email;
        existing.Sector = client.Sector;
        existing.Status = client.Status;

        await _context.SaveChangesAsync();

        return changes.Count > 0 ? string.Join(" ; ", changes) : "Aucun changement";
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
