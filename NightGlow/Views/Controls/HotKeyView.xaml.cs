using NightGlow.Models;
using System.Windows;
using System.Windows.Controls;

namespace NightGlow.Views.Controls;

public partial class HotKeyView : UserControl
{

    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(nameof(HotKey), typeof(HotKey), typeof(HotKeyView), null);

    public HotKey HotKey
    {
        get => (HotKey)GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }

    public string Title { get; set; }

    public HotKeyView()
    {
        InitializeComponent();
    }
}
