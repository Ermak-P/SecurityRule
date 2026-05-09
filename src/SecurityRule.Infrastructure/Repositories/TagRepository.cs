using Microsoft.EntityFrameworkCore;
using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;
using SecurityRule.Infrastructure.Data;

namespace SecurityRule.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly AppDbContext _context;

    public TagRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
        => await _context.Tags.OrderBy(t => t.Name).ToListAsync();

    public async Task<Tag> GetOrCreateAsync(string name)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name);
        if (tag is not null) return tag;

        tag = new Tag { Name = name };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }
}
