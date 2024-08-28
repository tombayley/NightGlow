using System.Windows;
using System.Windows.Controls;

namespace NightGlow.Views.Controls;

public partial class PopupDisplayInfoView : UserControl
{

    public static readonly DependencyProperty SliderValueProperty = DependencyProperty.Register(nameof(SliderValue), typeof(double), typeof(PopupDisplayInfoView), null);

    public double SliderValue
    {
        get => (double)GetValue(SliderValueProperty);
        set => SetValue(SliderValueProperty, value);
    }

    public static readonly DependencyProperty SliderValueMinProperty = DependencyProperty.Register(nameof(SliderValueMin), typeof(double), typeof(PopupDisplayInfoView), null);

    public double SliderValueMin
    {
        get => (double)GetValue(SliderValueMinProperty);
        set => SetValue(SliderValueMinProperty, value);
    }

    public static readonly DependencyProperty SliderValueMaxProperty = DependencyProperty.Register(nameof(SliderValueMax), typeof(double), typeof(PopupDisplayInfoView), null);

    public double SliderValueMax
    {
        get => (double)GetValue(SliderValueMaxProperty);
        set => SetValue(SliderValueMaxProperty, value);
    }

    public string Icon { get; set; }

    public PopupDisplayInfoView()
    {
        InitializeComponent();
    }

}
