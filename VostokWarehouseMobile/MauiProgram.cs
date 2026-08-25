using LibVLCSharp.Shared;
namespace VostokWarehouseMobile;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Core.Initialize();
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseLibVLCSharp();
        return builder.Build();
    }
}
