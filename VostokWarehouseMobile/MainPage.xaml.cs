#if ANDROID

using System.Net;
using System.Security.Cryptography;
using System.Text;

using Android.Content;
using Android.Views;
using Android.Widget;

using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Rtsp;

#endif

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION SETTINGS
    // ============================================================

    private const string HikvisionUserName = "admin";

    private const string HikvisionPassword = "Vos@3558817";


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
    // ANDROID MEDIA3
    // ============================================================

    private ExoPlayer? _player;

    private SurfaceView? _surfaceView;

#endif


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
            VideoStatus.Text = "Select a camera";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Initialization error: {ex.Message}";
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
            return;
        }

        await StartCameraAsync(
            cameraName,
            ip);
    }


    // ============================================================
    // START CAMERA
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
                "Starting RTSP decoder...";

            VideoStatus.Text =
                "Connecting to RTSP...";


            // ====================================================
            // IMPORTANT
            //
            // Hikvision:
            //
            // 101 = Main stream
            // 102 = Sub stream
            //
            // Your 102 stream works in VLC.
            // ====================================================

            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);

            string rtspUrl =
                $"rtsp://{HikvisionUserName}:{encodedPassword}" +
                $"@{ip}:554/Streaming/Channels/102";


            // ====================================================
            // CREATE NATIVE ANDROID SURFACE
            // ====================================================

            _surfaceView =
                new SurfaceView(
                    Android.App.Application.Context);


            _surfaceView.LayoutParameters =
                new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);


            // ====================================================
            // ADD SURFACE TO MAUI CONTAINER
            // ====================================================

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                VideoContainer.Children.Clear();

                VideoContainer.Children.Add(
                    new Android.Views.View
                    {
                    });

                VideoStatus.Text =
                    "Connecting...";
            });


            // ====================================================
            // CREATE MEDIA3 EXOPLAYER
            // ====================================================

            _player =
                new ExoPlayer.Builder(
                    Android.App.Application.Context)
                .Build();


            // ====================================================
            // CREATE RTSP MEDIA SOURCE
            // ====================================================

            var mediaItem =
                MediaItem.FromUri(
                    Android.Net.Uri.Parse(
                        rtspUrl));


            var rtspFactory =
                new RtspMediaSource.Factory();


            // ====================================================
            // VERY IMPORTANT
            //
            // FORCE RTP OVER TCP
            //
            // Hikvision RTSP is known to work better with
            // TCP in some LAN/network configurations.
            // ====================================================

            rtspFactory.SetForceUseRtpTcp(
                true);


            RtspMediaSource mediaSource =
                rtspFactory.CreateMediaSource(
                    mediaItem);


            // ====================================================
            // ATTACH SOURCE
            // ====================================================

            _player.SetMediaSource(
                mediaSource);


            // ====================================================
            // PREPARE
            // ====================================================

            _player.Prepare();


            // ====================================================
            // START PLAYBACK
            // ====================================================

            _player.PlayWhenReady = true;


            CameraStatus.Text =
                $"Live View: {cameraName}";

            StatusLabel.Text =
                "RTSP connected";

            VideoStatus.Text =
                "Playing";


            // ====================================================
            // NOTE
            //
            // Native video surface attachment is handled below.
            // ====================================================

            AttachVideoSurface();

        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                $"Camera error: {ex.Message}";

            StatusLabel.Text =
                "RTSP error";

            VideoStatus.Text =
                ex.Message;
        }

#else

        await DisplayAlertAsync(
            "RTSP",
            "Native Android RTSP playback is available only on Android.",
            "OK");

#endif
    }


#if ANDROID

    // ============================================================
    // ATTACH VIDEO SURFACE
    // ============================================================

    private void AttachVideoSurface()
    {
        try
        {
            if (_player == null)
                return;

            if (_surfaceView == null)
                return;

            // ====================================================
            // Media3 Player accepts Android Surface.
            // ====================================================

            _player.SetVideoSurfaceView(
                _surfaceView);

        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Video surface error: {ex.Message}";
        }
    }


    // ============================================================
    // STOP CAMERA
    // ============================================================

    private void StopCamera()
    {
        try
        {
            if (_player != null)
            {
                _player.Stop();

                _player.ClearVideoSurface();

                _player.Release();

                _player.Dispose();

                _player = null;
            }

            _surfaceView = null;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                VideoContainer.Children.Clear();

                VideoStatus.Text =
                    "Select a camera";
            });
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

#endif


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
                $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1";


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
            // GET DIGEST CHALLENGE
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
                    $"Command failed.\nHTTP {(int)firstResponse.StatusCode}",
                    "OK");

                return;
            }


            // ====================================================
            // GET DIGEST HEADER
            // ====================================================

            string? authenticate =
                firstResponse.Headers.WwwAuthenticate
                    .FirstOrDefault(
                        x =>
                            x.Scheme.Equals(
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


            // ====================================================
            // HA1
            // ====================================================

            string ha1 =
                Md5Hash(
                    $"{HikvisionUserName}:{realm}:{HikvisionPassword}");


            // ====================================================
            // HA2
            // ====================================================

            string ha2 =
                Md5Hash(
                    $"PUT:{new Uri(url).AbsolutePath}");


            string responseHash;


            if (!string.IsNullOrWhiteSpace(qop))
            {
                string selectedQop =
                    qop.Split(',')
                       .Select(
                           x => x.Trim())
                       .FirstOrDefault(
                           x =>
                               x.Equals(
                                   "auth",
                                   StringComparison.OrdinalIgnoreCase))
                    ?? "auth";


                responseHash =
                    Md5Hash(
                        $"{ha1}:{nonce}:{nc}:{cnonce}:{selectedQop}:{ha2}");
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
                    qop,
                    responseHash,
                    cnonce,
                    nc,
                    url);


            // ====================================================
            // SECOND REQUEST
            // ====================================================

            using HttpRequestMessage secondRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);


            secondRequest.Headers.TryAddWithoutValidation(
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
                $"Unable to open {doorName}.\n\n{ex.Message}",
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


            result[key] =
                value;
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
                       x =>
                           x.Equals(
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
    // CLEANUP
    // ============================================================

    protected override void OnDisappearing()
    {
#if ANDROID
        StopCamera();
#endif

        base.OnDisappearing();
    }
}
