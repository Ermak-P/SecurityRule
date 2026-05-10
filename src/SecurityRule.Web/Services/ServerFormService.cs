using SecurityRule.Domain.Interfaces;
using SecurityRule.Domain.Models;

namespace SecurityRule.Web.Services;

public sealed record ServerFormModel(
    Server Server,
    IReadOnlyCollection<AppService> AllServices,
    IReadOnlyCollection<string> OperatingSystems,
    IReadOnlyCollection<string> AllTagNames,
    IReadOnlyCollection<int> SelectedServiceIds,
    IReadOnlyCollection<string> SelectedTags);

public sealed class ServerFormService
{
    private readonly IServerRepository _serverRepository;
    private readonly IAppServiceRepository _appServiceRepository;
    private readonly IOperatingSystemRepository _operatingSystemRepository;
    private readonly ITagRepository _tagRepository;

    public ServerFormService(
        IServerRepository serverRepository,
        IAppServiceRepository appServiceRepository,
        IOperatingSystemRepository operatingSystemRepository,
        ITagRepository tagRepository)
    {
        _serverRepository = serverRepository;
        _appServiceRepository = appServiceRepository;
        _operatingSystemRepository = operatingSystemRepository;
        _tagRepository = tagRepository;
    }

    public async Task<ServerFormModel> GetCreateModelAsync(int? cloneFrom = null, CancellationToken cancellationToken = default)
    {
        var server = new Server();
        var selectedServiceIds = Array.Empty<int>();
        var selectedTags = Array.Empty<string>();

        if (cloneFrom.HasValue)
        {
            var source = await _serverRepository.GetByIdAsync(cloneFrom.Value, cancellationToken);
            if (source is not null)
            {
                server = new Server
                {
                    Name = source.Name,
                    IpAddress = source.IpAddress,
                    OperatingSystem = source.OperatingSystem,
                    Description = source.Description
                };
                selectedServiceIds = source.Services.Select(s => s.Id).ToArray();
                selectedTags = source.Tags.Select(t => t.Name).ToArray();
            }
        }

        return await BuildFormModelAsync(server, selectedServiceIds, selectedTags, cancellationToken);
    }

    public async Task<ServerFormModel?> GetEditModelAsync(int id, CancellationToken cancellationToken = default)
    {
        var server = await _serverRepository.GetByIdAsync(id, cancellationToken);
        if (server is null) return null;

        return await BuildFormModelAsync(
            server,
            server.Services.Select(s => s.Id).ToArray(),
            server.Tags.Select(t => t.Name).ToArray(),
            cancellationToken);
    }

    public async Task CreateAsync(
        Server server,
        IReadOnlyCollection<int> selectedServiceIds,
        IReadOnlyCollection<string> selectedTags,
        CancellationToken cancellationToken = default)
    {
        server.Services = selectedServiceIds.Select(id => new AppService { Id = id }).ToList();
        server.Tags = (await ResolveTagsAsync(selectedTags, cancellationToken))
            .Select(t => new Tag { Id = t.Id, Name = t.Name })
            .ToList();

        await _serverRepository.AddAsync(server, cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Server server,
        IReadOnlyCollection<int> selectedServiceIds,
        IReadOnlyCollection<string> selectedTags,
        CancellationToken cancellationToken = default)
    {
        var existing = await _serverRepository.GetByIdAsync(server.Id, cancellationToken);
        if (existing is null) return false;

        server.Services = selectedServiceIds.Select(id => new AppService { Id = id }).ToList();
        server.Tags = (await ResolveTagsAsync(selectedTags, cancellationToken))
            .Select(t => new Tag { Id = t.Id, Name = t.Name })
            .ToList();

        await _serverRepository.UpdateAsync(server, cancellationToken);
        return true;
    }

    private async Task<ServerFormModel> BuildFormModelAsync(
        Server server,
        IReadOnlyCollection<int> selectedServiceIds,
        IReadOnlyCollection<string> selectedTags,
        CancellationToken cancellationToken)
    {
        var allServices = (await _appServiceRepository.GetAllAsync(cancellationToken)).ToList();
        var osOptions = (await _operatingSystemRepository.GetAllAsync(cancellationToken))
            .Select(o => o.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var allTagNames = (await _tagRepository.GetAllAsync(cancellationToken))
            .Select(t => t.Name)
            .ToList();

        return new ServerFormModel(
            server,
            allServices,
            osOptions,
            allTagNames,
            selectedServiceIds,
            selectedTags);
    }

    private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> selectedTags, CancellationToken cancellationToken)
    {
        var result = new List<Tag>();
        foreach (var tagName in selectedTags.Distinct(StringComparer.OrdinalIgnoreCase))
            result.Add(await _tagRepository.GetOrCreateAsync(tagName, cancellationToken));
        return result;
    }
}
