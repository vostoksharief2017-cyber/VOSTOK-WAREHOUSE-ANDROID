using System.Net;
using System.Security.Cryptography;
using System.Text;
using LibVLCSharp.Shared;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION
    // ============================================================

    private const string HikvisionUserName = "admin";

    private const string HikvisionPassword = "Vos@3558817";


    // ============================================================
    // LIBVLC
    // ============================================================

    private LibVLC? _libVLC;

    private MediaPlayer? _mediaPlayer;

    private bool _libVLCInitialized;


    // ============================================================
    // CAMERAS
    // ============================================================

    private readonly Dictionary<string, string> cameras = new()
    {
        ["WareHouse-7"] = "192.168.5.131",
        ["WareHouse-9"] = "192.168.5.133",
        ["WareHouse-4"] = "192.168.5.134",
        ["WareHouse-5"] = "192.168.5.132"
    };


    // ============================================================
    // DOORS
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

        foreach (string cameraName in cameras.Keys)
        {
            CameraPicker.Items.Add(cameraName);
        }

        CameraStatus.Text = "Select a camera";
        StatusLabel.Text = "Ready";

        VideoStatus.Text = "Select a camera";
    }


    // ============================================================
    // PAGE APPEARED
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_libVLCInitialized)
            return;

        await Task.Delay(500);

        await InitializeLibVLCAsync();
    }


    // ============================================================
    // INITIALIZE LIBVLC
    // ============================================================

    private async Task InitializeLibVLCAsync()
    {
        try
        {
            StatusLabel.Text =
                "Starting video engine...";

            await Task.Run(() =>
            {
                Core.Initialize();

                _libVLC =
                    new LibVLC(
                        "--network-caching=1000",
                        "--rtsp-tcp");
            });

            _mediaPlayer =
                new MediaPlayer(_libVLC);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                VideoView.MediaPlayer =
                    _mediaPlayer;
            });

            _libVLCInitialized = true;

            StatusLabel.Text =
                "Video engine ready";

            VideoStatus.Text =
                "Select a camera";
        }
        catch (Exception ex)
        {
            _libVLCInitialized = false;

            StatusLabel.Text =
                "Video engine failed";

            VideoStatus.Text =
                "Live View unavailable";

            await DisplayAlertAsync(
                "Live View Error",
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // CAMERA SELECTED
    // ============================================================

    private async void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem
            is not string cameraName)
            return;

        if (!cameras.TryGetValue(
                cameraName,
                out string? ip))
        {
            CameraStatus.Text =
                "Camera IP not found.";

            return;
        }

        CameraStatus.Text =
            $"Connecting: {cameraName}";

        StatusLabel.Text =
            ip;

        if (!_libVLCInitialized ||
            _libVLC == null ||
            _mediaPlayer == null)
        {
            VideoStatus.Text =
                "Video engine is not ready";

            await DisplayAlertAsync(
                "Live View",
                "Video engine is still starting. Please select the camera again.",
                "OK");

            return;
        }

        await StartCameraAsync(
            cameraName,
            ip);
    }


    // ============================================================
    // START RTSP
    // ============================================================

    private async Task StartCameraAsync(
        string cameraName,
        string ip)
    {
        try
        {
            if (_libVLC == null ||
                _mediaPlayer == null)
                return;


            // ----------------------------------------------------
            // STOP PREVIOUS CAMERA
            // ----------------------------------------------------

            _mediaPlayer.Stop();


            // ----------------------------------------------------
            // RTSP URL
            // ----------------------------------------------------

            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);


            string rtspUrl =
                $"rtsp://{HikvisionUserName}:" +
                $"{encodedPassword}@" +
                $"{ip}:554/" +
                "Streaming/Channels/101";


            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            VideoStatus.Text =
                $"Opening {cameraName}...";


            // ----------------------------------------------------
            // CREATE MEDIA
            // ----------------------------------------------------

            using Media media =
                new Media(
                    _libVLC,
                    new Uri(rtspUrl));


            media.AddOption(
                ":network-caching=1000");

            media.AddOption(
                ":rtsp-tcp");


            // ----------------------------------------------------
            // PLAY
            // ----------------------------------------------------

            bool started =
                _mediaPlayer.Play(media);


            if (started)
            {
                CameraStatus.Text =
                    $"Live View: {cameraName}";

                StatusLabel.Text =
                    $"RTSP: {ip}";

                VideoStatus.IsVisible =
                    false;
            }
            else
            {
                CameraStatus.Text =
                    "Unable to start stream.";

                VideoStatus.Text =
                    "RTSP stream could not be started";

                VideoStatus.IsVisible =
                    true;
            }
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Camera error";

            VideoStatus.Text =
                "Live View Error";

            VideoStatus.IsVisible =
                true;

            await DisplayAlertAsync(
                "RTSP Error",
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // DOOR 1
    // ============================================================

    private async void Door1_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 1 - WH7");
    }


    // ============================================================
    // DOOR 2
    // ============================================================

    private async void Door2_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 2 - WH9");
    }


    // ============================================================
    // DOOR 4
    // ============================================================

    private async void Door4_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 4 - WH4");
    }


    // ============================================================
    // DOOR 5
    // ============================================================

    private async void Door5_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync(
            "Door 5 - WH5");
    }


    // ============================================================
    // HIKVISION DOOR OPEN
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


        try
        {
            string url =
                $"http://{ip}/ISAPI/" +
                "AccessControl/" +
                "RemoteControl/door/1";


            string xml =
                "<?xml version=\"1.0\" " +
                "encoding=\"UTF-8\"?>" +
                "<RemoteControlDoor>" +
                "<cmd>open</cmd>" +
                "</RemoteControlDoor>";


            using HttpClient client =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(10)
                };


            // ----------------------------------------------------
            // FIRST REQUEST
            // ----------------------------------------------------

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


            if (firstResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");

                return;
            }


            if (firstResponse.StatusCode !=
                HttpStatusCode.Unauthorized)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"Command failed.\n\n" +
                    $"HTTP {(int)firstResponse.StatusCode}",
                    "OK");

                return;
            }


            // ----------------------------------------------------
            // DIGEST CHALLENGE
            // ----------------------------------------------------

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


            string cnonce =
                CreateRandomHex(16);


            string nc =
                "00000001";


            string ha1 =
                Md5Hash(
                    $"{HikvisionUserName}:" +
                    $"{realm}:" +
                    $"{HikvisionPassword}");


            string uri =
                new Uri(url).AbsolutePath;


            string ha2 =
                Md5Hash(
                    $"PUT:{uri}");


            string responseHash;


            if (!string.IsNullOrWhiteSpace(qop))
            {
                string selectedQop =
                    qop.Split(',')
                       .Select(
                           x => x.Trim())
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


            string authorization =
                BuildDigestAuthorization(
                    realm,
                    nonce,
                    qop,
                    responseHash,
                    cnonce,
                    nc,
                    url);


            // ----------------------------------------------------
            // SECOND REQUEST
            // ----------------------------------------------------

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
    // DIGEST PARSER
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
                   .Select(
                       x => x.Trim())
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
            Encoding.UTF8.GetBytes(
                input);


        byte[] hash =
            MD5.HashData(
                bytes);


        return Convert.ToHexString(
                hash)
            .ToLowerInvariant();
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
                bytes)
            .ToLowerInvariant();
    }


    // ============================================================
    // PAGE DISAPPEARING
    // ============================================================

    protected override void OnDisappearing()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();

                _mediaPlayer.Dispose();

                _mediaPlayer = null;
            }

            if (_libVLC != null)
            {
                _libVLC.Dispose();

                _libVLC = null;
            }

            _libVLCInitialized = false;
        }
        catch
        {
            // Ignore cleanup errors
        }

        base.OnDisappearing();
    }
}
