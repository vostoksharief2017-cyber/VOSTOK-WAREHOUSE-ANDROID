using CommunityToolkit.Maui;
using CommunityToolkit.Maui.MediaElement;

namespace VostokWarehouseMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement();

        return builder.Build();
    }
}
