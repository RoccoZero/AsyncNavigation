using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Path = Avalonia.Controls.Shapes.Path;

namespace AsyncNavigation.Avalonia;

public class IconResolver : IIconResolver<Control>
{
    private readonly Dictionary<string, Bitmap> _fileCache = [];

    public Control? Resolve(IconDescriptor descriptor, double size = 24)
    {
        return descriptor.Kind switch
        {
            IconKind.FilePath => ResolveFile(descriptor.Value, size),
            IconKind.PathData => ResolvePathData(descriptor.Value, size),
            IconKind.IconFont => ResolveIconFont(descriptor.Value, size),
            IconKind.ResourceKey => ResolveResourceKey(descriptor.Value, size),
            _ => null
        };
    }

    protected virtual Image? ResolveFile(string path, double size)
    {
        var fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath)) return null;

        if (!_fileCache.TryGetValue(fullPath, out var bitmap))
        {
            bitmap = new Bitmap(fullPath);
            _fileCache[fullPath] = bitmap;
        }

        return new Image
        {
            Source = bitmap,
            Width = size,
            Height = size
        };
    }

    protected virtual Path? ResolvePathData(string data, double size)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;

        return CreatePath(StreamGeometry.Parse(data), size);
    }

    private static Path CreatePath(StreamGeometry geometry, double size)
    {
        var path = new Path
        {
            Data = geometry,
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
        };

        path.Bind(Path.FillProperty, path.GetObservable(TextElement.ForegroundProperty));

        return path;
    }

    protected virtual TextBlock ResolveIconFont(string name, double size)
    {
        return new TextBlock
        {
            Text = name,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
    }

    protected virtual Control? ResolveResourceKey(string resourceKey, double size)
    {
        if (Application.Current is null) return null;

        if (!Application.Current.TryGetResource(resourceKey,
                Application.Current.ActualThemeVariant, out var resource))
            return null;

        return resource switch
        {
            StreamGeometry geometry => CreatePath(geometry, size),
            IImage image => new Image
            {
                Source = image,
                Width = size,
                Height = size
            },
            string pathData => ResolvePathData(pathData, size),
            _ => null
        };
    }
}
