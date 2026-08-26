using System.Net;
using System.Security.Cryptography;
using System.Text;
using LibVLCSharp.Shared;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    private const string HikvisionUserName = "admin";
    private const string HikvisionPassword = "Vos@3558817";

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;

    private readonly Dictionary<string, string> cameras = new()
    {
        ["WareHouse-7"] = "192.168.5.131",
        ["WareHouse-9"] = "192.168.5.133",
        ["WareHouse-4"] = "192.168.5.134",
        ["WareHouse-5"] = "192.168.5.132"
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

        foreach (string cameraName in cameras.Keys)
        {
            CameraPicker.Items.Add(cameraName);
        }

        CameraStatus.Text = "Select a camera";
        StatusLabel.Text = "Starting...";

        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object? sender, EventArgs e)
    {
        try
        {
            Core.Initialize();

            _libVLC = new LibVLC();

            StatusLabel.Text = "Ready";
        }
        catch (Exception ex)
        {
            CameraStatus.Text = "LibVLC initialization failed";
            StatusLabel.Text = ex.Message;
        }
    }

    private async void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem is not string cameraName)
            return;

        if (!cameras.TryGetValue(cameraName, out string? ip))
            return;

        if (string.IsNullOrWhiteSpace(HikvisionPassword))
        {
            await DisplayAlertAsync(
                "Password Required",
                "Please configure the Hikvision password.",
                "OK");

            return;
        }

        await StartCameraAsync(cameraName, ip);
    }

    private async Task StartCameraAsync(
        string cameraName,
        string ip)
    {
        try
        {
            if (_libVLC == null)
            {
                CameraStatus.Text = "LibVLC is not initialized.";
                return;
            }

            StopCamera();

            string encodedPassword =
                Uri.EscapeDataString(HikvisionPassword);

            string rtspUrl =
                $"rtsp://{HikvisionUserName}:{encodedPassword}" +
                $"@{ip}:554/Streaming/Channels/101";

            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            _mediaPlayer = new MediaPlayer(_libVLC);

            VideoView.MediaPlayer = _mediaPlayer;

            _currentMedia =
                new Media(_libVLC, new Uri(rtspUrl));

            _currentMedia.AddOption(":network-caching=1000");
            _currentMedia.AddOption(":rtsp-tcp");

            bool started =
                _mediaPlayer.Play(_currentMedia);

            if (started)
            {
                CameraStatus.Text =
                    $"Live View: {cameraName}";

                StatusLabel.Text =
                    "Live View Connected";
            }
            else
            {
                CameraStatus.Text =
                    "Unable to start camera stream.";

                StatusLabel.Text =
                    "Stream Failed";
            }
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                $"Camera error: {ex.Message}";

            StatusLabel.Text =
                "Camera Error";
        }
    }

    private void StopCamera()
    {
        try
        {
            if (VideoView != null)
            {
                VideoView.MediaPlayer = null;
            }

            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            if (_currentMedia != null)
            {
                _currentMedia.Dispose();
                _currentMedia = null;
            }
        }
        catch
        {
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
        if (!doors.TryGetValue(doorName, out string? ip))
        {
            await DisplayAlertAsync(
                "Door Control",
                "Door IP not found.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(HikvisionPassword))
        {
            await DisplayAlertAsync(
                "Password Required",
                "Please configure the Hikvision password.",
                "OK");

            return;
        }

        try
        {
            string url =
                $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1";

            string xml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<RemoteControlDoor>" +
                "<cmd>open</cmd>" +
                "</RemoteControlDoor>";

            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            using HttpRequestMessage request =
                new(HttpMethod.Put, url);

            request.Content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");

            using HttpResponseMessage response =
                await client.SendAsync(request);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    "Hikvision Digest authentication is required.",
                    "OK");

                return;
            }

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");
            }
            else
            {
                string body =
                    await response.Content.ReadAsStringAsync();

                await DisplayAlertAsync(
                    "Door Control",
                    $"Command failed.\n\nHTTP {(int)response.StatusCode}\n\n{body}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Unable to open {doorName}.\n\n{ex.Message}",
                "OK");
        }
    }

    protected override void OnDisappearing()
    {
        StopCamera();

        try
        {
            _libVLC?.Dispose();
            _libVLC = null;
        }
        catch
        {
        }

        base.OnDisappearing();
    }
}
