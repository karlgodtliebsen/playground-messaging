using CommunityToolkit.Maui;

using Maui.Prometheus.Viewer.Configuration;

using Microcharts.Maui;

using Microsoft.Extensions.Logging;


namespace Maui.Prometheus.Viewer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMicrocharts()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

            //Microsoft.Extensions.Logging.LoggingBuilderExtensions.
            builder.Logging.ClearProviders();
            //builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());

#endif

            // Register Services
            builder.Services.AddPrometheusServices(builder.Configuration);


            return builder.Build();
        }
    }
}
