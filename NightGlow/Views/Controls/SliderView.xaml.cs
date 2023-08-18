using System.Windows;
using System.Windows.Controls;

namespace NightGlow.Views.Controls;

public partial class SliderView : UserControl
{

    public static readonly DependencyProperty SliderValueProperty = DependencyProperty.Register("SliderValue", typeof(int), typeof(SliderView), null);

    public int SliderValue
    {
        get { return (int)GetValue(SliderValueProperty); }
        set
        {
            value = ValidateSliderValue(value);
            SetValue(SliderValueProperty, value);
        }
    }

    public int ValueMin { get; set; }

    public int ValueMax { get; set; }

    public int ValueStep { get; set; }

    public string Title { get; set; }

    public SliderView()
    {
        InitializeComponent();
    }

    private int ValidateSliderValue(int value)
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
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (string.IsNullOrEmpty(textBox.Text)) return;

        if (!int.TryParse(textBox.Text, out int value)) value = ValueMax;

        textBox.Text = ValidateSliderValue(value).ToString();
    }

}
