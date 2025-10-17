// Controls/ThemeToggle.xaml.cs
namespace Maui.Prometheus.Viewer.Pages.Controls;

public partial class ThemeToggle : ContentView
{
    // BindableProperties
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(ThemeToggle),
            0,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedIndexChanged);

    public static readonly BindableProperty IconLightProperty =
        BindableProperty.Create(
            nameof(IconLight),
            typeof(ImageSource),
            typeof(ThemeToggle),
            null);

    public static readonly BindableProperty IconDarkProperty =
        BindableProperty.Create(
            nameof(IconDark),
            typeof(ImageSource),
            typeof(ThemeToggle),
            null);

    // Properties
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ImageSource? IconLight
    {
        get => (ImageSource?)GetValue(IconLightProperty);
        set => SetValue(IconLightProperty, value);
    }

    public ImageSource? IconDark
    {
        get => (ImageSource?)GetValue(IconDarkProperty);
        set => SetValue(IconDarkProperty, value);
    }

    // Event
    public event EventHandler<SelectedIndexChangedEventArgs>? SelectionChanged;

    public ThemeToggle()
    {
        InitializeComponent();
        UpdateSelection();
    }

    private void OnLightTapped(object? sender, EventArgs e)
    {
        if (SelectedIndex != 0)
        {
            SelectedIndex = 0;
        }
    }

    private void OnDarkTapped(object? sender, EventArgs e)
    {
        if (SelectedIndex != 1)
        {
            SelectedIndex = 1;
        }
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ThemeToggle control)
        {
            control.UpdateSelection();
            control.SelectionChanged?.Invoke(control, new SelectedIndexChangedEventArgs((int)newValue));
        }
    }

    private void UpdateSelection()
    {
        // Update visual state with smooth animation
        var selectedColor = Color.FromArgb("#2196F3");
        var unselectedColor = Colors.Transparent;
        if (SelectedIndex == 0)
        {
            // Light mode selected
            AnimateBackground(LightBorder, selectedColor);
            AnimateBackground(DarkBorder, unselectedColor);
        }
        else
        {
            // Dark mode selected
            AnimateBackground(LightBorder, unselectedColor);
            AnimateBackground(DarkBorder, selectedColor);
        }
    }

    private void AnimateBackground(Border border, Color targetColor)
    {
        // Cancel any existing animation
        border.AbortAnimation("BackgroundAnimation");

        // Get current color or use transparent as fallback
        var startColor = border.BackgroundColor ?? Colors.Transparent;

        // Animate color transition
        var animation = new Animation(v =>
        {
            border.BackgroundColor = Color.FromRgba(
                startColor.Red + (targetColor.Red - startColor.Red) * v,
                startColor.Green + (targetColor.Green - startColor.Green) * v,
                startColor.Blue + (targetColor.Blue - startColor.Blue) * v,
                startColor.Alpha + (targetColor.Alpha - startColor.Alpha) * v
            );
        }, 0, 1);

        animation.Commit(border, "BackgroundAnimation", 16, 200, Easing.CubicOut);
    }
}

// Event args for selection changed
public class SelectedIndexChangedEventArgs(int selectedIndex) : EventArgs
{
    public int SelectedIndex { get; } = selectedIndex;
}