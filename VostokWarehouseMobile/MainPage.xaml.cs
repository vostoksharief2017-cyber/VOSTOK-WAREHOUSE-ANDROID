using System.Net;
using System.Security.Cryptography;
using System.Text;
using LibVLCSharp.Shared;
using LibVLCSharp.Maui;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION SETTINGS
    // ============================================================

    private const string HikvisionUserName = "admin";
    private const string HikvisionPassword = "Vos@3558817";

    // ============================================================
    // LIBVLC
    // ============================================================

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private VideoView? _videoView;

    // ============================================================
    // CAMERA LIST
    // ============================================================

    private readonly Dictionary<string, string> cameras = new()
    {
        ["WareHouse-7"] = "192.168.5.131",
        ["WareHouse-9"] = "192.168.5.133",
        ["WareHouse-4"] = "192.168.5.134",
        ["WareHouse-5"] = "192.168.5.132"
    };

    // ============================================================
    // DOOR LIST
    // ============================================================

    private readonly Dictionary<string, string> doors = new()
    {
        ["Door 1 - WH7"] = "192.168.5.131",
        ["Door 2 - WH9"] = "192.168.5.133",
        ["Door 4 - WH4"] = "192.168.5.134",
        ["Door 5 - WH5"] = "192.168.5.132"
    };

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainPage()
    {
        InitializeComponent();

        // Add camera names to Picker
        foreach (string cameraName in cameras.Keys)
        {
            CameraPicker.Items.Add(cameraName);
        }

        CameraStatus.Text = "Select a camera";
        StatusLabel.Text = "Starting...";

        // Create LibVLC video control dynamically
        try
        {
            _videoView = new VideoView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            VideoContainer.Children.Add(_videoView);
        }
        catch (Exception ex)
        {
            CameraStatus.Text = "Video control error";
            StatusLabel.Text = ex.Message;
        }

        // Initialize LibVLC after page is loaded
        Loaded += MainPage_Loaded;
    }

    // ============================================================
    // LIBVLC INITIALIZATION
    // ============================================================

    private void MainPage_Loaded(object? sender, EventArgs e)
    {
        try
        {
            if (_libVLC != null)
                return;

            Core.Initialize();

            _libVLC = new LibVLC();

            StatusLabel.Text = "Ready";
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "LibVLC initialization failed";

            StatusLabel.Text =
                ex.Message;
        }
    }

    // ============================================================
    // CAMERA SELECTION
    // ============================================================

    private async void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem is not string cameraName)
            return;

        if (!cameras.TryGetValue(
                cameraName,
                out string? ip))
        {
            CameraStatus.Text =
                "Camera IP not found.";

            return;
        }

        if (string.IsNullOrWhiteSpace(
                HikvisionPassword))
        {
            await DisplayAlertAsync(
                "Password Required",
                "Please configure the Hikvision password.",
                "OK");

            return;
        }

        await StartCameraAsync(
            cameraName,
            ip);
    }

    // ============================================================
    // START LIVE VIEW
    // ============================================================

    private async Task StartCameraAsync(
        string cameraName,
        string ip)
    {
        try
        {
            if (_libVLC == null)
            {
                CameraStatus.Text =
                    "LibVLC is not initialized.";

                return;
            }

            if (_videoView == null)
            {
                CameraStatus.Text =
                    "Video control is not initialized.";

                return;
            }

            StopCamera();

            // Encode password for RTSP URL
            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);

            // Hikvision Main Stream
            string rtspUrl =
                $"rtsp://{HikvisionUserName}:{encodedPassword}" +
                $"@{ip}:554/Streaming/Channels/101";

            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            StatusLabel.Text =
                "Connecting...";

            // Create MediaPlayer
            _mediaPlayer =
                new MediaPlayer(_libVLC);

            // Attach MediaPlayer to VideoView
            _videoView.MediaPlayer =
                _mediaPlayer;

            // Dispose previous media
            _currentMedia?.Dispose();

            // Create new Media
            _currentMedia =
                new Media(
                    _libVLC,
                    new Uri(rtspUrl));

            // RTSP settings
            _currentMedia.AddOption(
                ":network-caching=1000");

            _currentMedia.AddOption(
                ":rtsp-tcp");

            // Start stream
            bool started =
                _mediaPlayer.Play(
                    _currentMedia);

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

    // ============================================================
    // STOP CAMERA
    // ============================================================

    private void StopCamera()
    {
        try
        {
            // Detach MediaPlayer from VideoView
            if (_videoView != null)
            {
                _videoView.MediaPlayer = null;
            }

            // Stop and dispose MediaPlayer
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }

                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            // Dispose Media
            if (_currentMedia != null)
            {
                _currentMedia.Dispose();
                _currentMedia = null;
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    // ============================================================
    // DOOR BUTTONS
    // ============================================================

    private async void Door1_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 1 - WH7");
    }

    private async void Door2_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 2 - WH9");
    }

    private async void Door4_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 4 - WH4");
    }

    private async void Door5_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 5 - WH5");
    }

    // ============================================================
    // HIKVISION DOOR OPEN
    // DIGEST AUTHENTICATION
    // ============================================================

    private async Task OpenDoorAsync(
        string doorName)
    {
        if (!doors.TryGetValue(
                doorName,
                out string? ip))
        {
            await DisplayAlertAsync(
                "Door Control",
                "Door IP not found.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(
                HikvisionPassword))
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
                $"http://{ip}" +
                "/ISAPI/AccessControl/" +
                "RemoteControl/door/1";

            string xml =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<RemoteControlDoor>" +
                "<cmd>open</cmd>" +
                "</RemoteControlDoor>";

            using HttpClient client =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(10)
                };

            // ====================================================
            // FIRST REQUEST
            // Get Digest challenge
            // ====================================================

            using HttpRequestMessage firstRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);

            firstRequest.Content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");

            using HttpResponseMessage firstResponse =
                await client.SendAsync(
                    firstRequest);

            // ====================================================
            // IF DEVICE ACCEPTS WITHOUT DIGEST
            // ====================================================

            if (firstResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");

                return;
            }

            // ====================================================
            // CHECK UNAUTHORIZED
            // ====================================================

            if (firstResponse.StatusCode !=
                HttpStatusCode.Unauthorized)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"Command failed. HTTP " +
                    $"{(int)firstResponse.StatusCode}",
                    "OK");

                return;
            }

            // ====================================================
            // GET DIGEST CHALLENGE
            // ====================================================

            string? authenticate =
                firstResponse.Headers
                    .WwwAuthenticate
                    .FirstOrDefault(
                        x => x.Scheme.Equals(
                            "Digest",
                            StringComparison.OrdinalIgnoreCase))
                    ?.Parameter;

            if (string.IsNullOrWhiteSpace(
                    authenticate))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Hikvision did not provide a Digest authentication challenge.",
                    "OK");

                return;
            }

            Dictionary<string, string> digest =
                ParseDigestChallenge(
                    authenticate);

            if (!digest.TryGetValue(
                    "realm",
                    out string? realm) ||
                !digest.TryGetValue(
                    "nonce",
                    out string? nonce))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Invalid Hikvision Digest challenge.",
                    "OK");

                return;
            }

            digest.TryGetValue(
                "qop",
                out string? qop);

            // ====================================================
            // CNONCE / NONCE COUNT
            // ====================================================

            string cnonce =
                CreateRandomHex(16);

            string nc =
                "00000001";

            // ====================================================
            // HA1
            // ====================================================

            string ha1 =
                Md5Hash(
                    $"{HikvisionUserName}:" +
                    $"{realm}:" +
                    $"{HikvisionPassword}");

            // ====================================================
            // HA2
            // ====================================================

            string ha2 =
                Md5Hash(
                    $"PUT:{new Uri(url).AbsolutePath}");

            // ====================================================
            // RESPONSE HASH
            // ====================================================

            string responseHash;

            string selectedQop =
                "auth";

            if (!string.IsNullOrWhiteSpace(
                    qop))
            {
                selectedQop =
                    qop.Split(',')
                       .Select(x => x.Trim())
                       .FirstOrDefault(
                           x => x.Equals(
                               "auth",
                               StringComparison.OrdinalIgnoreCase))
                    ?? "auth";

                responseHash =
                    Md5Hash(
                        $"{ha1}:" +
                        $"{nonce}:" +
                        $"{nc}:" +
                        $"{cnonce}:" +
                        $"{selectedQop}:" +
                        $"{ha2}");
            }
            else
            {
                responseHash =
                    Md5Hash(
                        $"{ha1}:" +
                        $"{nonce}:" +
                        $"{ha2}");
            }

            // ====================================================
            // BUILD AUTHORIZATION
            // ====================================================

            string authorization =
                BuildDigestAuthorization(
                    realm,
                    nonce,
                    qop,
                    responseHash,
                    cnonce,
                    nc,
                    url);

            // ====================================================
            // SECOND AUTHENTICATED REQUEST
            // ====================================================

            using HttpRequestMessage secondRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);

            secondRequest.Headers
                .TryAddWithoutValidation(
                    "Authorization",
                    authorization);

            secondRequest.Content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");

            using HttpResponseMessage secondResponse =
                await client.SendAsync(
                    secondRequest);

            // ====================================================
            // SUCCESS
            // ====================================================

            if (secondResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");
            }
            else
            {
                string responseBody =
                    await secondResponse.Content
                        .ReadAsStringAsync();

                await DisplayAlertAsync(
                    "Door Control",
                    $"Command failed.\n\n" +
                    $"HTTP {(int)secondResponse.StatusCode}\n\n" +
                    responseBody,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Unable to open {doorName}.\n\n" +
                ex.Message,
                "OK");
        }
    }

    // ============================================================
    // DIGEST CHALLENGE PARSER
    // ============================================================

    private static Dictionary<string, string>
        ParseDigestChallenge(
            string header)
    {
        Dictionary<string, string> result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        string[] parts =
            header.Split(',');

        foreach (string part in parts)
        {
            string item =
                part.Trim();

            int equals =
                item.IndexOf('=');

            if (equals <= 0)
                continue;

            string key =
                item[..equals].Trim();

            string value =
                item[(equals + 1)..].Trim();

            value =
                value.Trim('"');

            result[key] = value;
        }

        return result;
    }

    // ============================================================
    // DIGEST AUTHORIZATION
    // ============================================================

    private static string
        BuildDigestAuthorization(
            string realm,
            string nonce,
            string? qop,
            string responseHash,
            string cnonce,
            string nc,
            string url)
    {
        string uri =
            new Uri(url).AbsolutePath;

        StringBuilder header =
            new StringBuilder();

        header.Append("Digest ");

        header.Append(
            $"username=\"{HikvisionUserName}\", ");

        header.Append(
            $"realm=\"{realm}\", ");

        header.Append(
            $"nonce=\"{nonce}\", ");

        header.Append(
            $"uri=\"{uri}\", ");

        header.Append(
            $"response=\"{responseHash}\"");

        if (!string.IsNullOrWhiteSpace(qop))
        {
            string selectedQop =
                qop.Split(',')
                   .Select(x => x.Trim())
                   .FirstOrDefault(
                       x => x.Equals(
                           "auth",
                           StringComparison.OrdinalIgnoreCase))
                ?? "auth";

            header.Append(
                $", qop={selectedQop}");

            header.Append(
                $", nc={nc}");

            header.Append(
                $", cnonce=\"{cnonce}\"");
        }

        return header.ToString();
    }

    // ============================================================
    // MD5
    // ============================================================

    private static string Md5Hash(
        string input)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(input);

        byte[] hash =
            MD5.HashData(bytes);

        return Convert.ToHexString(
            hash).ToLowerInvariant();
    }

    // ============================================================
    // RANDOM CNONCE
    // ============================================================

    private static string CreateRandomHex(
        int byteCount)
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(
                byteCount);

        return Convert.ToHexString(
            bytes).ToLowerInvariant();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    protected override void OnDisappearing()
    {
        StopCamera();

        try
        {
            if (_videoView != null)
            {
                _videoView.MediaPlayer = null;
                _videoView.Dispose();
                _videoView = null;
            }
        }
        catch
        {
        }

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
