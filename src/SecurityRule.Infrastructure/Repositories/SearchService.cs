using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class SearchService : ISearchService
{
    private readonly AppDbContext _context;

    public SearchService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var results = new List<SearchResult>();

        var servers = await _context.Servers
            .Where(s => s.Name.Contains(query) || s.IpAddress.Contains(query) ||
                        s.OperatingSystem.Contains(query) ||
                        (s.Description != null && s.Description.Contains(query)))
            .Take(50)
            .ToListAsync();

        foreach (var s in servers)
        {
            if (s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервер", s.Id, "Название", s.Name, $"/servers/{s.Id}"));
            if (s.IpAddress.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервер", s.Id, "IP адрес", s.IpAddress, $"/servers/{s.Id}"));
            if (s.OperatingSystem.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервер", s.Id, "Операционная система", s.OperatingSystem, $"/servers/{s.Id}"));
            if (!string.IsNullOrEmpty(s.Description) && s.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервер", s.Id, "Описание", s.Description, $"/servers/{s.Id}"));
        }

        var services = await _context.AppServices
            .Where(s => s.Name.Contains(query) || s.UserName.Contains(query))
            .Take(50)
            .ToListAsync();

        foreach (var s in services)
        {
            if (s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервис", s.Id, "Название", s.Name, $"/services/{s.Id}"));
            if (!string.IsNullOrEmpty(s.UserName) && s.UserName.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сервис", s.Id, "AD учётная запись", s.UserName, $"/services/{s.Id}"));
        }

        var users = await _context.Users
            .Where(u => u.Name.Contains(query) || u.Description.Contains(query))
            .Take(50)
            .ToListAsync();

        foreach (var u in users)
        {
            if (u.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Пользователь", u.Id, "Название", u.Name, $"/users/{u.Id}"));
            if (!string.IsNullOrEmpty(u.Description) && u.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Пользователь", u.Id, "Описание", u.Description, $"/users/{u.Id}"));
        }

        var groups = await _context.Groups
            .Where(g => g.Name.Contains(query) || g.Description.Contains(query))
            .Take(50)
            .ToListAsync();

        foreach (var g in groups)
        {
            if (g.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Группа", g.Id, "Название", g.Name, $"/groups/{g.Id}"));
            if (!string.IsNullOrEmpty(g.Description) && g.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Группа", g.Id, "Описание", g.Description, $"/groups/{g.Id}"));
        }

        var certs = await _context.Certificates
            .Where(c => c.Description.Contains(query))
            .Take(50)
            .ToListAsync();

        foreach (var c in certs)
        {
            if (c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Сертификат", c.Id, "Описание", c.Description, $"/certificates/edit/{c.Id}"));
        }

        var rules = await _context.FirewallRules
            .Where(r => r.SourceIp.Contains(query) || r.DestinationIp.Contains(query) ||
                        r.Description.Contains(query))
            .Take(50)
            .ToListAsync();

        foreach (var r in rules)
        {
            if (r.SourceIp.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Правило фаервола", r.Id, "IP источника", r.SourceIp, $"/firewall-rules/edit/{r.Id}"));
            if (r.DestinationIp.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Правило фаервола", r.Id, "IP назначения", r.DestinationIp, $"/firewall-rules/edit/{r.Id}"));
            if (!string.IsNullOrEmpty(r.Description) && r.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult("Правило фаервола", r.Id, "Описание", r.Description, $"/firewall-rules/edit/{r.Id}"));
        }

        return results;
    }
}
