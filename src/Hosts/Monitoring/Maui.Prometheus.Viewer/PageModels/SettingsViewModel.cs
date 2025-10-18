using CommunityToolkit.Mvvm.ComponentModel;

using Messaging.EventHub.Library;

using System.ComponentModel;

namespace Maui.Prometheus.Viewer.PageModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int themeIndex;

    /// <inheritdoc/>
    public SettingsViewModel(IEventHub eventHub) : base("Settings")
    {
        LoadThemePreference();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(ThemeIndex))
        {
            Preferences.Set("AppTheme", ThemeIndex);
            ApplyTheme();
        }
    }

    private void LoadThemePreference()
    {
        // Get current app theme
        var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

        // If unspecified, check saved preference or use system default
        if (currentTheme == AppTheme.Unspecified)
        {
            ThemeIndex = Preferences.Get("AppTheme", 0);
            //ApplyTheme();
        }
        else
        {
            // Map current theme to index
            ThemeIndex = currentTheme == AppTheme.Light ? 0 : 1;
        }
    }


    private void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = ThemeIndex switch
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