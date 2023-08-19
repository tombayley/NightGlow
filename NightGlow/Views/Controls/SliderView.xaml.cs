using System.Windows;
using System.Windows.Controls;

namespace NightGlow.Views.Controls;

public partial class SliderView : UserControl
{

    public static readonly DependencyProperty SliderValueProperty = DependencyProperty.Register(nameof(SliderValue), typeof(double), typeof(SliderView), null);

    public double SliderValue
    {
        get => (double)GetValue(SliderValueProperty);
        set
        {
            value = ValidateSliderValue(value);
            SetValue(SliderValueProperty, value);
        }
    }

    public double ValueMin { get; set; }

    public double ValueMax { get; set; }

    public double ValueStep { get; set; }

    public string Title { get; set; }

    public SliderView()
    {
        InitializeComponent();
    }

    private double ValidateSliderValue(double value)
    {
        if (value < ValueMin) return ValueMin;
        else if (value > ValueMax) return ValueMax;
        return value;
    }

    private void Plus_Click(object sender, RoutedEventArgs e)
    {
        SliderValue += ValueStep;
    }

    private void Minus_Click(object sender, RoutedEventArgs e)
    {
        SliderValue -= ValueStep;
    }

    private void PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !double.TryParse(e.Text, out _);
    }

    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (string.IsNullOrEmpty(textBox.Text)) return;

        if (!double.TryParse(textBox.Text, out double value)) value = ValueMax;

        textBox.Text = ValidateSliderValue(value).ToString();
    }

}
