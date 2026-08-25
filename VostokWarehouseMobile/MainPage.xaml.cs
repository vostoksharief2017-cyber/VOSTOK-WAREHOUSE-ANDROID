using System.Net;
using System.Text;
using LibVLCSharp.Shared;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    private readonly LibVLC _libVLC;
    private MediaPlayer? _mediaPlayer;

    private const string UserName = "admin";

    private readonly Dictionary<string, string> cameras = new()
    {
        ["WareHouse-7"] = "rtsp://admin:PASSWORD@192.168.5.131:554/Streaming/Channels/101",
        ["WareHouse-9"] = "rtsp://admin:PASSWORD@192.168.5.133:554/Streaming/Channels/101",
        ["WareHouse-4"] = "rtsp://admin:PASSWORD@192.168.5.134:554/Streaming/Channels/101",
        ["WareHouse-5"] = "rtsp://admin:PASSWORD@192.168.5.132:554/Streaming/Channels/101"
    };

    private readonly Dictionary<string, string> doors = new()
    {
        ["Door 1 - WH7"] = "192.168.5.131",
        ["Door 2 - WH9"] = "192.168.5.133",
        ["Door 4 - WH4"] = "192.168.5.134",
        ["Door 5 - WH5"] = "192.168.5.132"
    };

    public MainPage()
    {
        InitializeComponent();

        _libVLC = new LibVLC();

        foreach (var name in cameras.Keys)
        {
            CameraPicker.Items.Add(name);
        }

        PasswordEntry.Text =
            Preferences.Default.Get("hik_password", "");
    }

    private void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem is not string name)
            return;

        if (!cameras.TryGetValue(name, out var template))
            return;

        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            CameraStatus.Text =
                "Enter Hikvision password first.";
            return;
        }

        try
        {
            var rtsp = template.Replace(
                "PASSWORD",
                Uri.EscapeDataString(password));

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();

            _mediaPlayer = new MediaPlayer(_libVLC);

            using var media =
                new Media(_libVLC, new Uri(rtsp));

            _mediaPlayer.Play(media);

            CameraStatus.Text =
                $"Playing: {name}";
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                $"Camera error: {ex.Message}";
        }
    }

    private async void Door1_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 1 - WH7");
    }

    private async void Door2_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 2 - WH9");
    }

    private async void Door4_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 4 - WH4");
    }

    private async void Door5_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 5 - WH5");
    }

    private async Task OpenDoorAsync(string doorName)
    {
        if (!doors.TryGetValue(doorName, out var ip))
            return;

        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
                "Password Required",
                "Enter the Hikvision password first.",
                "OK");

            return;
        }

        try
        {
            var handler = new HttpClientHandler
            {
                Credentials =
                    new NetworkCredential(
                        UserName,
                        password),

                PreAuthenticate = false
            };

            using var client =
                new HttpClient(handler)
                {
                    Timeout =
                        TimeSpan.FromSeconds(10)
                };

            var url =
                $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1";

            var xml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<RemoteControlDoor>" +
                "<cmd>open</cmd>" +
                "</RemoteControlDoor>";

            using var content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");

            using var response =
                await client.PutAsync(
                    url,
                    content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");
            }
            else
            {
                await DisplayAlert(
                    "Door Control",
                    $"Command failed. HTTP {(int)response.StatusCode}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Door Control",
                $"Unable to open {doorName}.\n\n{ex.Message}",
                "OK");
        }
    }

    private void SavePassword_Clicked(
        object sender,
        EventArgs e)
    {
        Preferences.Default.Set(
            "hik_password",
            PasswordEntry.Text ?? "");

        StatusLabel.Text =
            "Password saved on this phone.";
    }

    protected override void OnDisappearing()
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC.Dispose();

        base.OnDisappearing();
    }
}
