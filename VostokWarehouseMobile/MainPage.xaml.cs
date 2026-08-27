using System.Net;
using System.Security.Cryptography;
using System.Text;

#if ANDROID
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Media3.Common;
using AndroidX.Media3.Exoplayer;
using AndroidX.Media3.Exoplayer.Rtsp;
using AndroidX.Media3.Ui;
#endif

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION SETTINGS
    // ============================================================

    private const string HikvisionUserName = "admin";

    private const string HikvisionPassword =
        "Vos@3558817";


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


#if ANDROID

    // ============================================================
    // MEDIA3 PLAYER
    // ============================================================

    private ExoPlayer? _player;

    private PlayerView? _playerView;

    private MediaItem? _mediaItem;

#endif


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainPage()
    {
        InitializeComponent();

        try
        {
            CameraPicker.Items.Clear();

            foreach (string cameraName in cameras.Keys)
            {
                CameraPicker.Items.Add(cameraName);
            }

            CameraStatus.Text =
                "Select a camera";

            StatusLabel.Text =
                "Ready";

            VideoStatus.Text =
                "Select a camera";
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Startup error";

            StatusLabel.Text =
                ex.Message;

            VideoStatus.Text =
                "Application error";
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


            await StartCameraAsync(
                cameraName,
                ip);
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Camera error";

            StatusLabel.Text =
                ex.Message;
        }
    }


    // ============================================================
    // START RTSP CAMERA
    // ============================================================

    private async Task StartCameraAsync(
        string cameraName,
        string ip)
    {
#if ANDROID

        try
        {
            StopCamera();


            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            StatusLabel.Text =
                $"RTSP: {ip}";

            VideoStatus.Text =
                "Starting video...";


            // ====================================================
            // CREATE ANDROID CONTEXT
            // ====================================================

            Context? context =
                Android.App.Application.Context;

            if (context == null)
            {
                VideoStatus.Text =
                    "Android context unavailable.";

                return;
            }


            // ====================================================
            // CREATE EXOPLAYER
            // ====================================================

            _player =
                new ExoPlayer.Builder(
                    context)
                .Build();


            // ====================================================
            // CREATE PLAYER VIEW
            // ====================================================

            _playerView =
                new PlayerView(context);


            _playerView.UseController = false;

            _playerView.Player =
                _player;


            // ====================================================
            // ADD PLAYER VIEW TO MAUI GRID
            // ====================================================

            VideoContainer.Children.Clear();

            VideoContainer.Children.Add(
                _playerView);


            // ====================================================
            // HIKVISION RTSP URL
            // ====================================================

            string user =
                Uri.EscapeDataString(
                    HikvisionUserName);

            string password =
                Uri.EscapeDataString(
                    HikvisionPassword);


            string rtspUrl =
                $"rtsp://{user}:{password}" +
                $"@{ip}:554/Streaming/Channels/101";


            // ====================================================
            // MEDIA ITEM
            // ====================================================

            _mediaItem =
                MediaItem.FromUri(
                    Android.Net.Uri.Parse(
                        rtspUrl));


            // ====================================================
            // RTSP MEDIA SOURCE
            // ====================================================

            RtspMediaSource.Factory rtspFactory =
                new RtspMediaSource.Factory();


            // Force RTSP over TCP.
            rtspFactory.SetForceUseRtpTcp(true);


            MediaSource mediaSource =
                rtspFactory.CreateMediaSource(
                    _mediaItem);


            // ====================================================
            // LOAD STREAM
            // ====================================================

            _player.SetMediaSource(
                mediaSource);


            _player.Prepare();


            _player.PlayWhenReady =
                true;


            CameraStatus.Text =
                $"Live View: {cameraName}";

            StatusLabel.Text =
                $"Connecting - {ip}";

            VideoStatus.Text = "";


            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                $"RTSP Error";

            StatusLabel.Text =
                ex.Message;

            VideoStatus.Text =
                "Unable to play RTSP stream.";
        }

#else

        CameraStatus.Text =
            "Android RTSP only.";

        StatusLabel.Text =
            "This application is configured for Android.";

        VideoStatus.Text =
            "Not supported on this platform.";

        await Task.CompletedTask;

#endif
    }


    // ============================================================
    // STOP CAMERA
    // ============================================================

    private void StopCamera()
    {
#if ANDROID

        try
        {
            if (_player != null)
            {
                _player.Stop();
            }
        }
        catch
        {
        }


        try
        {
            if (_player != null)
            {
                _player.ClearMediaItems();
            }
        }
        catch
        {
        }


        try
        {
            if (_playerView != null)
            {
                _playerView.Player = null;
            }
        }
        catch
        {
        }


        try
        {
            if (_player != null)
            {
                _player.Release();
                _player.Dispose();
                _player = null;
            }
        }
        catch
        {
        }


        try
        {
            if (_playerView != null)
            {
                _playerView.Dispose();
                _playerView = null;
            }
        }
        catch
        {
        }


        _mediaItem = null;

#endif

        try
        {
            VideoContainer.Children.Clear();

            Label label =
                new Label
                {
                    Text = "Select a camera",
                    TextColor = Colors.White,
                    HorizontalOptions =
                        LayoutOptions.Center,
                    VerticalOptions =
                        LayoutOptions.Center
                };

            VideoContainer.Children.Add(label);
        }
        catch
        {
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
            string url =
                $"http://{ip}" +
                "/ISAPI/AccessControl/" +
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


            // ====================================================
            // FIRST REQUEST
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
            // SUCCESS
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
            // EXPECT DIGEST 401
            // ====================================================

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


            // ====================================================
            // GET DIGEST CHALLENGE
            // ====================================================

            string? authenticate =
                firstResponse.Headers
                    .WwwAuthenticate
                    .FirstOrDefault(
                        x =>
                            x.Scheme.Equals(
                                "Digest",
                                StringComparison
                                    .OrdinalIgnoreCase))
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


            // ====================================================
            // HA1
            // ====================================================

            string ha1 =
                Md5Hash(
                    $"{HikvisionUserName}:" +
                    $"{realm}:" +
                    $"{HikvisionPassword}");


            // ====================================================
            // URI
            // ====================================================

            string uri =
                new Uri(url)
                    .AbsolutePath;


            // ====================================================
            // HA2
            // ====================================================

            string ha2 =
                Md5Hash(
                    $"PUT:{uri}");


            string responseHash;

            string? selectedQop =
                null;


            // ====================================================
            // RESPONSE HASH
            // ====================================================

            if (!string.IsNullOrWhiteSpace(qop))
            {
                selectedQop =
                    qop.Split(',')
                       .Select(
                           x => x.Trim())
                       .FirstOrDefault(
                           x =>
                               x.Equals(
                                   "auth",
                                   StringComparison
                                       .OrdinalIgnoreCase));


                if (string.IsNullOrWhiteSpace(
                        selectedQop))
                {
                    selectedQop =
                        "auth";
                }


                responseHash =
                    Md5Hash(
                        $"{ha1}:{nonce}:{nc}:" +
                        $"{cnonce}:{selectedQop}:{ha2}");
            }
            else
            {
                responseHash =
                    Md5Hash(
                        $"{ha1}:{nonce}:{ha2}");
            }


            // ====================================================
            // AUTHORIZATION
            // ====================================================

            string authorization =
                BuildDigestAuthorization(
                    realm,
                    nonce,
                    selectedQop,
                    responseHash,
                    cnonce,
                    nc,
                    uri);


            // ====================================================
            // SECOND REQUEST
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
            // RESULT
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
                    $"HTTP {(int)secondResponse.StatusCode}" +
                    $"\n\n{responseBody}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Unable to open {doorName}." +
                $"\n\n{ex.Message}",
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
            string uri)
    {
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
            header.Append(
                $", qop={qop}");


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
    // CLEANUP
    // ============================================================

    protected override void OnDisappearing()
    {
        StopCamera();

        base.OnDisappearing();
    }
}
