using System.Net;
using System.Security.Cryptography;
using System.Text;
using LibVLCSharp.Shared;

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

    private bool _libVLCInitialized;


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

        try
        {
            foreach (string cameraName in cameras.Keys)
            {
                CameraPicker.Items.Add(cameraName);
            }

            CameraStatus.Text = "Select a camera";

            StatusLabel.Text = "Ready";
        }
        catch (Exception ex)
        {
            CameraStatus.Text = "Startup Error";

            StatusLabel.Text = ex.Message;
        }
    }


    // ============================================================
    // INITIALIZE LIBVLC
    // ============================================================

    private bool InitializeLibVLC()
    {
        try
        {
            if (_libVLCInitialized &&
                _libVLC != null)
            {
                return true;
            }

            StatusLabel.Text =
                "Initializing video engine...";

            /*
             * LibVLCSharp documentation supports Core.Initialize()
             * for loading the native LibVLC libraries.
             */

            Core.Initialize();

            _libVLC =
                new LibVLC(
                    "--network-caching=1000",
                    "--rtsp-tcp");

            _libVLCInitialized = true;

            StatusLabel.Text =
                "Video engine ready";

            return true;
        }
        catch (Exception ex)
        {
            _libVLCInitialized = false;

            _libVLC = null;

            StatusLabel.Text =
                $"LibVLC Error: {ex.Message}";

            return false;
        }
    }


    // ============================================================
    // CAMERA SELECTION
    // ============================================================

    private async void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        try
        {
            if (CameraPicker.SelectedItem
                is not string cameraName)
            {
                return;
            }

            if (!cameras.TryGetValue(
                    cameraName,
                    out string? ip))
            {
                CameraStatus.Text =
                    "Camera IP not found.";

                return;
            }

            CameraStatus.Text =
                $"Selected: {cameraName}";

            StatusLabel.Text =
                $"Camera IP: {ip}";

            bool initialized =
                InitializeLibVLC();

            if (!initialized)
            {
                await DisplayAlertAsync(
                    "Live View Error",
                    "LibVLC could not be initialized.\n\n" +
                    "Please check the LibVLC Android package.",
                    "OK");

                return;
            }

            await StartCameraAsync(
                cameraName,
                ip);
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Camera selection error.";

            StatusLabel.Text =
                ex.Message;
        }
    }


    // ============================================================
    // START CAMERA
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


            // ----------------------------------------------------
            // STOP PREVIOUS CAMERA
            // ----------------------------------------------------

            StopCamera();


            // ----------------------------------------------------
            // HIKVISION RTSP PASSWORD
            // ----------------------------------------------------

            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);


            // ----------------------------------------------------
            // HIKVISION RTSP URL
            // ----------------------------------------------------

            string rtspUrl =
                $"rtsp://{HikvisionUserName}:" +
                $"{encodedPassword}@" +
                $"{ip}:554/" +
                "Streaming/Channels/101";


            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            StatusLabel.Text =
                "Connecting to RTSP stream...";


            // ----------------------------------------------------
            // CREATE MEDIAPLAYER
            // ----------------------------------------------------

            _mediaPlayer =
                new MediaPlayer(
                    _libVLC);


            // ----------------------------------------------------
            // CONNECT MEDIAPLAYER TO VIDEOVIEW
            // ----------------------------------------------------

            VideoView.MediaPlayer =
                _mediaPlayer;


            // ----------------------------------------------------
            // CREATE MEDIA
            // ----------------------------------------------------

            _currentMedia =
                new Media(
                    _libVLC,
                    new Uri(rtspUrl));


            // ----------------------------------------------------
            // RTSP OPTIONS
            // ----------------------------------------------------

            _currentMedia.AddOption(
                ":rtsp-tcp");

            _currentMedia.AddOption(
                ":network-caching=1000");

            _currentMedia.AddOption(
                ":live-caching=1000");

            _currentMedia.AddOption(
                ":file-caching=1000");


            // ----------------------------------------------------
            // PLAY
            // ----------------------------------------------------

            bool started =
                _mediaPlayer.Play(
                    _currentMedia);


            if (started)
            {
                CameraStatus.Text =
                    $"Live View: {cameraName}";

                StatusLabel.Text =
                    $"Connected - {ip}";
            }
            else
            {
                CameraStatus.Text =
                    "Unable to start camera stream.";

                StatusLabel.Text =
                    "RTSP playback failed.";

                await DisplayAlertAsync(
                    "Live View",
                    "VLC could not start the Hikvision RTSP stream.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Camera Error";

            StatusLabel.Text =
                ex.Message;

            await DisplayAlertAsync(
                "Camera Error",
                $"Unable to connect to {cameraName}.\n\n" +
                $"IP: {ip}\n\n" +
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // STOP CAMERA
    // ============================================================

    private void StopCamera()
    {
        try
        {
            // ----------------------------------------------------
            // DETACH VIDEOVIEW
            // ----------------------------------------------------

            if (VideoView != null)
            {
                VideoView.MediaPlayer = null;
            }


            // ----------------------------------------------------
            // STOP MEDIAPLAYER
            // ----------------------------------------------------

            if (_mediaPlayer != null)
            {
                try
                {
                    if (_mediaPlayer.IsPlaying)
                    {
                        _mediaPlayer.Stop();
                    }
                }
                catch
                {
                }

                _mediaPlayer.Dispose();

                _mediaPlayer = null;
            }


            // ----------------------------------------------------
            // DISPOSE MEDIA
            // ----------------------------------------------------

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


        try
        {
            // ----------------------------------------------------
            // HIKVISION DOOR API
            // ----------------------------------------------------

            string url =
                $"http://{ip}/ISAPI/" +
                "AccessControl/" +
                "RemoteControl/door/1";


            // ----------------------------------------------------
            // XML COMMAND
            // ----------------------------------------------------

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
            // GET DIGEST CHALLENGE
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


            // ----------------------------------------------------
            // IF CAMERA ACCEPTS WITHOUT AUTH
            // ----------------------------------------------------

            if (firstResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");

                return;
            }


            // ----------------------------------------------------
            // EXPECT 401
            // ----------------------------------------------------

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
            // GET DIGEST HEADER
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


            // ----------------------------------------------------
            // PARSE DIGEST
            // ----------------------------------------------------

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


            // ----------------------------------------------------
            // CNONCE
            // ----------------------------------------------------

            string cnonce =
                CreateRandomHex(16);


            // ----------------------------------------------------
            // NONCE COUNT
            // ----------------------------------------------------

            string nc =
                "00000001";


            // ----------------------------------------------------
            // HA1
            // ----------------------------------------------------

            string ha1 =
                Md5Hash(
                    $"{HikvisionUserName}:" +
                    $"{realm}:" +
                    $"{HikvisionPassword}");


            // ----------------------------------------------------
            // URI
            // ----------------------------------------------------

            string uri =
                new Uri(url).AbsolutePath;


            // ----------------------------------------------------
            // HA2
            // ----------------------------------------------------

            string ha2 =
                Md5Hash(
                    $"PUT:{uri}");


            // ----------------------------------------------------
            // RESPONSE HASH
            // ----------------------------------------------------

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


            // ----------------------------------------------------
            // BUILD AUTHORIZATION
            // ----------------------------------------------------

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


            // ----------------------------------------------------
            // SUCCESS
            // ----------------------------------------------------

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
                item[..equals]
                    .Trim();


            string value =
                item[(equals + 1)..]
                    .Trim();


            value =
                value.Trim('"');


            result[key] = value;
        }


        return result;
    }


    // ============================================================
    // BUILD DIGEST AUTHORIZATION
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


        header.Append(
            "Digest ");


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
            StopCamera();


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
