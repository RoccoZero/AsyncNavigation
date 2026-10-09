using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PlatformRegionManager = AsyncNavigation.Avalonia.RegionManager;

namespace AsyncNavigation.E2E.Tests;

public sealed class RegionHotReloadTests
{
    [AvaloniaFact]
    public async Task Replacing_region_control_preserves_view_and_navigation_history()
    {
        var services = new ServiceCollection().AddNavigationSupport();
        services.RegisterView<TestView, Model>("A");
        services.RegisterView<TestView, Model>("B");
        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IRegionManager>();
        var original = new ContentControl();
        PlatformRegionManager.SetRegionName(original, "Main");
        var root = new Grid { Children = { original } };
        var window = new Window { Content = root };
        window.Show();
        try
        {
            var first = await manager.RequestNavigateAsync("Main", "A");
            var second = await manager.RequestNavigateAsync("Main", "B");
            Assert.True(first.IsSuccessful, first.Exception?.ToString());
            Assert.True(second.IsSuccessful, second.Exception?.ToString());
            var view = Assert.IsType<TestView>(second.NavigationContext!.Target.Value);
            ((TextBox)view.Content!).Text = "state survives hot reload";
            var selected = original.Content;
            var region = manager.Regions["Main"];
            var accessor = ((AsyncNavigation.Avalonia.ContentRegion)region).RegionControlAccessor;
            var current = original;
            for (var i = 0; i < 3; i++)
            {
                var replacement = new ContentControl();
                PlatformRegionManager.SetRegionName(replacement, "Main");
                Assert.Same(region, manager.Regions["Main"]);
                Assert.Same(selected, current.Content);
                root.Children.Clear();
                root.Children.Add(replacement);
                Dispatcher.UIThread.RunJobs();
                Assert.Same(region, manager.Regions["Main"]);
                Assert.Same(selected, replacement.Content);
                Assert.Null(current.Content);
                Assert.Same(accessor, ((AsyncNavigation.Avalonia.ContentRegion)region).RegionControlAccessor);
                Assert.Same(replacement, accessor.Ensure());
                current = replacement;
            }
            root.Children.Clear();
            root.Children.Add(original);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(selected, original.Content);
            Assert.Same(original, accessor.Ensure());
            root.Children.Clear();
            root.Children.Add(current);
            Dispatcher.UIThread.RunJobs();
            PlatformRegionManager.SetRegionName(original, "");
            Assert.Same(region, manager.Regions["Main"]);
            var back = await manager.GoBackAsync("Main");
            Assert.True(back.IsSuccessful, back.Exception?.ToString());
            Assert.Same(first.NavigationContext!.Target.Value, back.NavigationContext!.Target.Value);
            var forward = await manager.GoForwardAsync("Main");
            Assert.True(forward.IsSuccessful, forward.Exception?.ToString());
            Assert.Same(view, forward.NavigationContext!.Target.Value);
            Assert.Equal("state survives hot reload", ((TextBox)view.Content!).Text);
            Assert.Equal(1, ((Model)view.DataContext!).Initializations);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Clearing_a_pending_replacement_does_not_remove_the_active_region()
    {
        using var provider = new ServiceCollection().AddNavigationSupport().BuildServiceProvider();
        var manager = provider.GetRequiredService<IRegionManager>();
        var original = new ContentControl();
        PlatformRegionManager.SetRegionName(original, "Main");
        var region = manager.Regions["Main"];
        var pending = new ContentControl();
        PlatformRegionManager.SetRegionName(pending, "Main");
        PlatformRegionManager.SetRegionName(pending, "");
        Assert.Same(region, manager.Regions["Main"]);
        PlatformRegionManager.SetRegionName(original, "");
        Assert.False(manager.TryGetRegion("Main", out _));
    }

    [AvaloniaFact]
    public void Two_attached_controls_cannot_share_a_region_name()
    {
        using var provider = new ServiceCollection().AddNavigationSupport().BuildServiceProvider();
        var manager = provider.GetRequiredService<IRegionManager>();
        var original = new ContentControl();
        PlatformRegionManager.SetRegionName(original, "Main");
        var root = new Grid { Children = { original } };
        var window = new Window { Content = root };
        window.Show();
        try
        {
            var region = manager.Regions["Main"];
            var duplicate = new ContentControl();
            PlatformRegionManager.SetRegionName(duplicate, "Main");
            var exception = Assert.Throws<InvalidOperationException>(() => root.Children.Add(duplicate));
            Assert.Contains("Duplicated RegionName found: Main", exception.Message);
            Assert.Same(region, manager.Regions["Main"]);
            root.Children.Remove(duplicate);
        }
        finally
        {
            window.Close();
        }
    }

    public sealed class TestView : UserControl, IView
    {
        public TestView() => Content = new TextBox();
    }

    public sealed class Model : INavigationAware
    {
        public int Initializations { get; private set; }
        public event AsyncEventHandler<AsyncEventArgs>? AsyncRequestUnloadEvent;
        public Task InitializeAsync(NavigationContext context) { Initializations++; return Task.CompletedTask; }
        public Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
        public Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;
        public Task<bool> IsNavigationTargetAsync(NavigationContext context) => Task.FromResult(true);
        public Task OnUnloadAsync(CancellationToken token) => Task.CompletedTask;
    }
}
