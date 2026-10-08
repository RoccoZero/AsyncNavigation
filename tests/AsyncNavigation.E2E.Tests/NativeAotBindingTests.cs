using AsyncNavigation.Abstractions;
using AsyncNavigation.Avalonia;
using AsyncNavigation.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace AsyncNavigation.E2E.Tests;

public sealed class NativeAotBindingTests
{
    [AvaloniaFact]
    public async Task TabbedPage_UsesTwoWaySelectedIndex()
    {
        using var provider = new ServiceCollection().AddNavigationSupport().BuildServiceProvider();
        var control = new TabbedPage();
        using var region = new TabbedPageRegion("pages", control, provider, false);
        var first = new NavigationContext { RegionName = "pages", ViewName = "first" };
        var second = new NavigationContext { RegionName = "pages", ViewName = "second" };
        await region.ProcessActivateAsync(first);
        await region.ProcessActivateAsync(second);
        Assert.Equal(1, control.SelectedIndex);
        control.SelectedIndex = 0;
        Assert.Same(first, region.Capture().Context);
    }

    [AvaloniaFact]
    public async Task ContentRegion_UsesTwoWaySelectionAndDisposesFromWorkerThread()
    {
        using var provider = new ServiceCollection().AddNavigationSupport().BuildServiceProvider();
        var control = new ContentControl();
        var region = new ContentRegion("content", control, provider, true);
        var first = new NavigationContext { RegionName = "content", ViewName = "first" };
        var second = new NavigationContext { RegionName = "content", ViewName = "second" };
        await region.ProcessActivateAsync(first);
        Assert.Same(first, control.Content);
        control.Content = second;
        Assert.Same(second, region.Capture().Context);

        await Task.Run(region.Dispose, TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(control.Content);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void IconFill_InheritsForegroundAndTracksChanges(bool resource)
    {
        const string geometry = "M0,0 L10,0 L10,10 Z";
        var application = Application.Current!;
        application.Resources["TestGeometry"] = StreamGeometry.Parse(geometry);
        var descriptor = resource ? IconDescriptor.FromResourceKey("TestGeometry") : IconDescriptor.FromPathData(geometry);
        var icon = Assert.IsType<ShapePath>(new IconResolver().Resolve(descriptor));
        var button = new Button { Foreground = Brushes.Red, Content = icon };
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(icon.Fill).Color);

        button.Foreground = Brushes.Green;
        Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(icon.Fill).Color);
        window.Close();
        application.Resources.Remove("TestGeometry");
    }
}
