using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace DigitRecoUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                //Necesario para añadir el canvas
                 .UseMauiCommunityToolkit()

                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");

                    fonts.AddFont("LTSaeada-Light.otf", "Saeada");
                    fonts.AddFont("GalleroVintage-DemoVersion-Regular.otf", "GalleroVintage");

                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
