using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AsyncNavigation.Core;

public sealed class RegionContext : INotifyPropertyChanged
{
    public ObservableCollection<NavigationContext> Items { get; } = [];

    public RegionContext()
    {
        Items.CollectionChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
    }

    public int SelectedIndex
    {
        get => _selected is null ? -1 : Items.IndexOf(_selected);
        set
        {
            var items = Items;
            Selected = value >= 0 && value < items.Count ? items[value] : null;
        }
    }

    private NavigationContext? _selected;
    public NavigationContext? Selected
    {
        get => _selected;
        set
        {
            if (!ReferenceEquals(_selected, value))
            {
                _selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Clear()
    {
        Items.Clear();
        Selected = null;
    }
}

