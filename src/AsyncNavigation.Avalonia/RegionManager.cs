using AsyncNavigation.Abstractions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AsyncNavigation.Avalonia;

public sealed class RegionManager : RegionManagerBase
{
    #region RegionName
    public static readonly AttachedProperty<string> RegionNameProperty =
           AvaloniaProperty.RegisterAttached<RegionManager, AvaloniaObject, string>("RegionName");

    public static string GetRegionName(AvaloniaObject obj)
    {
        return obj.GetValue(RegionNameProperty);
    }

    public static void SetRegionName(AvaloniaObject obj, string value)
    {
        obj.SetValue(RegionNameProperty, value);
    }
    #endregion

    #region ServiceProvider
    public static readonly AttachedProperty<IServiceProvider?> ServiceProviderProperty =
           AvaloniaProperty.RegisterAttached<RegionManager, AvaloniaObject, IServiceProvider?>("ServiceProvider", defaultValue: null);

    public static IServiceProvider? GetServiceProvider(AvaloniaObject obj)
    {
        return obj.GetValue(ServiceProviderProperty);
    }

    public static void SetServiceProvider(AvaloniaObject obj, IServiceProvider? value)
    {
        obj.SetValue(ServiceProviderProperty, value);
    }
    #endregion

    #region PreferCache
    public static readonly AttachedProperty<bool?> PreferCacheProperty =
           AvaloniaProperty.RegisterAttached<RegionManager, AvaloniaObject, bool?>("PreferCache", null);

    public static bool? GetPreferCache(AvaloniaObject obj)
    {
        return obj.GetValue(PreferCacheProperty);
    }

    public static void SetPreferCache(AvaloniaObject obj, bool? value)
    {
        obj.SetValue(PreferCacheProperty, value);
    }
    #endregion

    static RegionManager()
    {
        RegionNameProperty
            .Changed
            .AddClassHandler<AvaloniaObject, string>((target, args) => 
            {
                var name = args.NewValue.GetValueOrDefault();
                var old = args.OldValue.GetValueOrDefault();

                if (name == old)
                    return;

                if (target is ContentControl control)
                {
                    control.AttachedToVisualTree -= OnRegionAttached;
                    if (!string.IsNullOrEmpty(name))
                        control.AttachedToVisualTree += OnRegionAttached;
                }
                if (!string.IsNullOrEmpty(old))
                {
                    var region = GetRegionCore(old);
                    if (region is not ContentRegion contentRegion ||
                        contentRegion.RegionControlAccessor.TryGet(out var owner) && ReferenceEquals(owner, target))
                        OnRemoveRegionNameCore(old);
                }
                if (!string.IsNullOrEmpty(name))
                    RegisterRegion(name, target);
            });
    }

    public RegionManager(IRegionFactory regionFactory, 
        IServiceProvider serviceProvider) : base(regionFactory, serviceProvider)
    {

    }

    private static void OnRegionAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ContentControl control)
        {
            var name = GetRegionName(control);
            if (!string.IsNullOrEmpty(name) && GetRegionCore(name) is ContentRegion)
                RegisterRegion(name, control);
        }
    }

    private static void RegisterRegion(string name, AvaloniaObject target)
    {
        if (target is ContentControl control && GetRegionCore(name) is ContentRegion region)
        {
            region.RegionControlAccessor.TryGet(out var previous);
            if (ReferenceEquals(previous, control) || !control.IsAttachedToVisualTree())
                return;
            if (previous is null || !previous.IsAttachedToVisualTree())
            {
                region.Reattach(control);
                return;
            }
        }
        var useCache = target.IsSet(PreferCacheProperty) ? GetPreferCache(target) : null;
        OnAddRegionNameCore(name, target, GetServiceProvider(target), useCache);
    }
}
