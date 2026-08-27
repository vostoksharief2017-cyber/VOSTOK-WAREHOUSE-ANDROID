using System.Net;
using System.Security.Cryptography;
using System.Text;

#if ANDROID
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Ui;
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

    private PlayerView? _playerView;

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

            CameraStatus.Text =
                "Select a camera";

            StatusLabel.Text =
                "Ready";

#if ANDROID
            CreateVideoPlayer();
#endif
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Initialization error";

            StatusLabel.Text =
                ex.Message;
        }
    }


#if ANDROID

    // ============================================================
    // CREATE NATIVE PLAYER VIEW
    // ============================================================

    private void CreateVideoPlayer()
    {
        try
        {
            Context context =
                Android.App.Application.Context;

            _playerView =
                new PlayerView(context);

            _playerView.LayoutParameters =
                new Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);

            _playerView.UseController = false;

            _playerView.SetBackgroundColor(
                Android.Graphics.Color.Black);

            VideoContainer.HandlerChanged +=
                VideoContainer_HandlerChanged;

            TryAttachPlayerView();
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Video initialization failed: {ex.Message}";
        }
    }


    // ============================================================
    // ATTACH NATIVE VIEW
    // ============================================================

    private void VideoContainer_HandlerChanged(
        object? sender,
        EventArgs e)
    {
        TryAttachPlayerView();
    }


    private void TryAttachPlayerView()
    {
        try
        {
            if (_playerView == null)
                return;

            if (VideoContainer.Handler?.PlatformView
                is Android.Views.ViewGroup nativeParent)
            {
                Android.Views.View nativeView =
                    _playerView;

                if (nativeView.Parent is Android.Views.ViewGroup oldParent)
                {
                    oldParent.RemoveView(nativeView);
                }

                if (nativeView.Parent == null)
                {
                    nativeParent.AddView(nativeView);

                    nativeView.LayoutParameters =
                        new Android.Views.ViewGroup.LayoutParams(
                            Android.Views.ViewGroup.LayoutParams.MatchParent,
                            Android.Views.ViewGroup.LayoutParams.MatchParent);
                }
            }
        }
        catch
        {
            // Native view may not be ready yet.
        }
    }

#endif


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
                "Camera not found.";

            return;
        }

        await StartCameraAsync(
            cameraName,
            ip);
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

            TryAttachPlayerView();

            if (_playerView == null)
            {
                CameraStatus.Text =
                    "Video player is not initialized.";

                return;
            }


            // ====================================================
            // RTSP URL
            // ====================================================

            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);

            string rtspUrl =
                $"rtsp://{HikvisionUserName}:{encodedPassword}" +
                $"@{ip}:554/Streaming/Channels/101";


            CameraStatus.Text =
                $"Connecting to {cameraName}...";

            StatusLabel.Text =
                $"RTSP: {ip}:554";


            VideoPlaceholder.IsVisible =
                false;


            // ====================================================
            // CREATE EXOPLAYER
            // ====================================================

            Context context =
                Android.App.Application.Context;


            _player =
                new ExoPlayer.Builder(
                    context)
                    .Build();


            // ====================================================
            // CONNECT PLAYER TO PLAYER VIEW
            // ====================================================

            _playerView.Player =
                _player;


            // ====================================================
            // RTSP MEDIA ITEM
            // ====================================================

            MediaItem mediaItem =
                MediaItem.FromUri(
                    Android.Net.Uri.Parse(
                        rtspUrl));


            _player.SetMediaItem(
                mediaItem);


            // ====================================================
            // PREPARE
            // ====================================================

            _player.Prepare();


            // ====================================================
            // START PLAYBACK
            // ====================================================

            _player.PlayWhenReady =
                true;


            CameraStatus.Text =
                $"Live View: {cameraName}";

            StatusLabel.Text =
                "Connecting to camera...";


            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            VideoPlaceholder.IsVisible =
                true;

            VideoPlaceholder.Text =
                "Unable to start video";

            CameraStatus.Text =
                $"Camera error: {ex.Message}";

            StatusLabel.Text =
                "RTSP connection failed.";
        }

#else

        await DisplayAlertAsync(
            "RTSP",
            "RTSP live view is supported on Android.",
            "OK");

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
                _player.PlayWhenReady =
                    false;

                _player.Stop();

                _player.Release();

                _player.Dispose();

                _player = null;
            }

            if (_playerView != null)
            {
                _playerView.Player =
                    null;
            }

            if (VideoPlaceholder != null)
            {
                VideoPlaceholder.IsVisible =
                    true;

                VideoPlaceholder.Text =
                    "Select a camera";
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }

#endif
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


        if (string.IsNullOrWhiteSpace(
                HikvisionPassword))
        {
            await DisplayAlertAsync(
                "Door Control",
                "Hikvision password is not configured.",
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
            // GET DIGEST CHALLENGE
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
                    "Hikvision did not provide a Digest challenge.",
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

            string uri =
                new Uri(url).AbsolutePath;


            string ha2 =
                Md5Hash(
                    $"PUT:{uri}");


            // ====================================================
            // RESPONSE HASH
            // ====================================================

            string responseHash;


            if (!string.IsNullOrWhiteSpace(
                    qop))
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
    // PAGE DISAPPEARING
    // ============================================================

    protected override void OnDisappearing()
    {
        StopCamera();

        base.OnDisappearing();
    }
}
