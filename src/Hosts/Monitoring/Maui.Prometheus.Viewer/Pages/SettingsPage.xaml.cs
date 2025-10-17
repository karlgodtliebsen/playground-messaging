namespace Maui.Prometheus.Viewer.Pages;

public partial class SettingsPage : ContentPage
{
    public int ThemeIndex { get; set; }

    public SettingsPage()
    {
        InitializeComponent();
        LoadThemePreference();
        BindingContext = this;
    }
    private void LoadThemePreference()
    {
        // Get current app theme
        var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

        // If unspecified, check saved preference or use system default
        if (currentTheme == AppTheme.Unspecified)
        {
            ThemeIndex = Preferences.Get("AppTheme", 0);
            ApplyTheme(ThemeIndex);
        }
        else
        {
            // Map current theme to index
            ThemeIndex = currentTheme == AppTheme.Light ? 0 : 1;
        }
    }

    private void OnThemeSelectionChanged(object? sender, Controls.SelectedIndexChangedEventArgs e)
    {
        // Save preference
        Preferences.Set("AppTheme", e.SelectedIndex);

        // Apply theme
        ApplyTheme(e.SelectedIndex);
    }

    private void ApplyTheme(int themeIndex)
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = themeIndex switch
            {
                0 => AppTheme.Light,
                1 => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }

    private void OnRefreshSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        var value = (int)e.NewValue;
        //RefreshLabel.Text = $"{value} second{(value == 1 ? "" : "s")}";

        // Save preference
        Preferences.Set("RefreshInterval", value);
    }
}