using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using MudBlazor.Services;

namespace SecurityRule.BDD.Tests.ComponentTests;

/// <summary>
/// Base class for bUnit tests using NUnit.
/// Creates a fresh Bunit.BunitContext for each test and disposes it after.
/// Configures MudBlazor services with a stub IPopoverService so that
/// components render without requiring a real MudPopoverProvider.
/// </summary>
public abstract class BunitTestContext
{
    private BunitContext _ctx = null!;

    protected IServiceCollection Services => _ctx.Services;

    protected IRenderedComponent<TComponent> Render<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameterBuilder = null)
        where TComponent : IComponent
        => parameterBuilder is null
            ? _ctx.Render<TComponent>()
            : _ctx.Render<TComponent>(parameterBuilder);

    [SetUp]
    public void SetUpBunitContext()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Should be called at the end of the derived class's [SetUp] after adding
    /// domain services. Registers MudBlazor services with the stub popover service.
    /// </summary>
    protected void AddMudBlazorTestServices()
    {
        Services.AddMudServices();
        Services.Replace(ServiceDescriptor.Scoped<IPopoverService, StubPopoverService>());
    }

    [TearDown]
    public async Task TearDownBunitContext()
    {
        if (_ctx is not null)
            await _ctx.DisposeAsync();
    }

    /// <summary>Stub that satisfies MudBlazor's IPopoverService without a live provider.</summary>
    private sealed class StubPopoverService : IPopoverService, IAsyncDisposable
    {
        public bool ThrowOnDuplicateProvider => false;
        public bool IsInitialized => true;
        public PopoverOptions PopoverOptions { get; } = new();
        public IEnumerable<IMudPopoverHolder> ActivePopovers => [];

        public Task CreatePopoverAsync(IPopover popover) => Task.CompletedTask;
        public Task<bool> UpdatePopoverAsync(IPopover popover) => Task.FromResult(true);
        public Task<bool> DestroyPopoverAsync(IPopover popover) => Task.FromResult(false);
        public ValueTask<int> GetProviderCountAsync() => ValueTask.FromResult(1);
        public void Subscribe(IPopoverObserver observer) { }
        public void Unsubscribe(IPopoverObserver observer) { }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
