using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncNavigation.Tests;

[Collection("RegionManagerCollection")]
public sealed class ViewManagerDisposalTests
{
    [Fact]
    public async Task Dispose_FromWorkerThread_ReleasesCacheWithoutAccessingViews()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var manager = new ViewManager(new NavigationOptions(), new ViewFactory(provider));
        var view = new ThreadAffineView();
        manager.AddView("view", view);

        await Task.Run(manager.Dispose, TestContext.Current.CancellationToken);
        manager.Dispose();

        Assert.Null(await manager.FindCachedViewAsync("view", _ => Task.FromResult(true)));
        Assert.False(view.Disposed);
    }

    private sealed class ThreadAffineView : IView, IDisposable
    {
        public object? DataContext
        {
            get => throw new InvalidOperationException("The view must not be accessed during cache disposal.");
            set => throw new InvalidOperationException("The view must not be accessed during cache disposal.");
        }

        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
