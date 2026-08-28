using System.Net;
using System.Text;

#if ANDROID
using Android.Views;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
#endif

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION DEVICE
    // ============================================================

    private const string HikvisionIp = "192.168.5.131";
    private const string HikvisionUsername = "admin";
    private const string HikvisionPassword = "Vos@3558817";

    // Hikvision sub-stream
    private const string HikvisionRtsp =
        "rtsp://admin:Vos@3558817@192.168.5.131:554/Streaming/Channels/102";


    // ============================================================
    // CAMERA LIST
    // ============================================================

    private readonly Dictionary<string, string> Cameras =
        new()
        {
            {
                "Hikvision Camera",
                HikvisionRtsp
            }
        };


#if ANDROID

    // ============================================================
    // MEDIA3 EXOPLAYER
    // ============================================================

    private IExoPlayer? _player;

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
            foreach (string cameraName in Cameras.Keys)
            {
                CameraPicker.Items.Add(cameraName);
            }

            if (CameraPicker.Items.Count > 0)
            {
                CameraPicker.SelectedIndex = 0;
            }

            StatusLabel.Text = "Ready";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "CONSTRUCTOR ERROR: " + ex);
        }
    }


    // ============================================================
    // PAGE APPEARING
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID

        try
        {
            StatusLabel.Text = "Starting video...";

            await Task.Delay(500);

            CreateVideoSurface();

            await Task.Delay(500);

            StartRtsp();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Video initialization failed";

            System.Diagnostics.Debug.WriteLine(
                "ON APPEARING ERROR: " + ex);
        }

#else

        StatusLabel.Text = "Android device required";

#endif
    }


#if ANDROID

    // ============================================================
    // CREATE ANDROID VIDEO SURFACE
    // ============================================================

    private void CreateVideoSurface()
    {
        try
        {
            if (_surfaceView != null)
                return;

            var context =
                Android.App.Application.Context;

            _surfaceView =
                new SurfaceView(context);

            _surfaceView.LayoutParameters =
                new Android.Views.ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);

            AddSurfaceToPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "CREATE SURFACE ERROR: " + ex);

            StatusLabel.Text =
                "Video surface error";
        }
    }


    // ============================================================
    // ADD SURFACE TO MAUI VIDEO CONTAINER
    // ============================================================

    private void AddSurfaceToPage()
    {
        try
        {
            if (_surfaceView == null)
                return;

            if (VideoContainer.Handler?.PlatformView
                is Android.Views.ViewGroup parent)
            {
                if (_surfaceView.Parent
                    is Android.Views.ViewGroup oldParent)
                {
                    oldParent.RemoveView(_surfaceView);
                }

                parent.AddView(_surfaceView);

                _surfaceView.LayoutParameters =
                    new Android.Views.ViewGroup.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.MatchParent);

                StatusLabel.Text =
                    "Video surface ready";

                return;
            }

            VideoContainer.HandlerChanged -=
                VideoContainer_HandlerChanged;

            VideoContainer.HandlerChanged +=
                VideoContainer_HandlerChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "ADD SURFACE ERROR: " + ex);
        }
    }


    // ============================================================
    // MAUI HANDLER READY
    // ============================================================

    private void VideoContainer_HandlerChanged(
        object? sender,
        EventArgs e)
    {
        VideoContainer.HandlerChanged -=
            VideoContainer_HandlerChanged;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddSurfaceToPage();
        });
    }


    // ============================================================
    // START RTSP
    // ============================================================

    private void StartRtsp()
    {
        try
        {
            StatusLabel.Text =
                "Starting RTSP player...";

            // ----------------------------------------------------
            // Make sure SurfaceView exists
            // ----------------------------------------------------

            if (_surfaceView == null)
            {
                CreateVideoSurface();
            }

            // ----------------------------------------------------
            // Stop previous player
            // ----------------------------------------------------

            StopPlayer();

            // ----------------------------------------------------
            // Android application context
            // ----------------------------------------------------

            var context =
                Android.App.Application.Context;

            // ----------------------------------------------------
            // Create Media3 ExoPlayer
            // ----------------------------------------------------

            _player =
                new ExoPlayerBuilder(context)
                    .Build();

            // ----------------------------------------------------
            // Connect player to SurfaceView
            // ----------------------------------------------------

            if (_surfaceView != null)
            {
                _player.SetVideoSurfaceView(
                    _surfaceView);
            }

            // ----------------------------------------------------
            // RTSP URL
            // ----------------------------------------------------

            StatusLabel.Text =
                "Connecting to camera...";

            var uri =
                Android.Net.Uri.Parse(
                    HikvisionRtsp);

            // ----------------------------------------------------
            // Create MediaItem
            // ----------------------------------------------------

            var mediaItem =
                MediaItem.FromUri(uri);

            // ----------------------------------------------------
            // Give MediaItem to ExoPlayer
            // ----------------------------------------------------

            _player.SetMediaItem(
                mediaItem);

            // ----------------------------------------------------
            // Prepare
            // ----------------------------------------------------

            StatusLabel.Text =
                "Preparing RTSP stream...";

            _player.Prepare();

            // ----------------------------------------------------
            // Start playback
            // ----------------------------------------------------

            _player.PlayWhenReady = true;

            StatusLabel.Text =
                "RTSP buffering...";
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                "RTSP Error";

            System.Diagnostics.Debug.WriteLine(
                "RTSP ERROR: " + ex);

            MainThread.BeginInvokeOnMainThread(
                async () =>
                {
                    await DisplayAlertAsync(
                        "RTSP Error",
                        ex.Message,
                        "OK");
                });
        }
    }


    // ============================================================
    // STOP EXOPLAYER
    // ============================================================

    private void StopPlayer()
    {
        try
        {
            if (_player != null)
            {
                _player.PlayWhenReady = false;

                _player.Stop();

                _player.Release();

                _player.Dispose();

                _player = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "STOP PLAYER ERROR: " + ex);

            _player = null;
        }
    }

#endif


    // ============================================================
    // CAMERA SELECTION
    // ============================================================

    private void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        try
        {
            if (CameraPicker.SelectedIndex < 0)
                return;

            string cameraName =
                CameraPicker.Items[
                    CameraPicker.SelectedIndex];

            CameraStatus.Text =
                cameraName;

#if ANDROID

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StartRtsp();
            });

#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "CAMERA SELECTION ERROR: " + ex);
        }
    }


    // ============================================================
    // DOOR 1
    // ============================================================

    private async void Door1_Clicked(
        object? sender,
        EventArgs e)
    {
        await OpenDoorAsync(1);
    }


    // ============================================================
    // DOOR 2
    // ============================================================

    private async void Door2_Clicked(
        object? sender,
        EventArgs e)
    {
        await OpenDoorAsync(2);
    }


    // ============================================================
    // DOOR 4
    // ============================================================

    private async void Door4_Clicked(
        object? sender,
        EventArgs e)
    {
        await OpenDoorAsync(4);
    }


    // ============================================================
    // DOOR 5
    // ============================================================

    private async void Door5_Clicked(
        object? sender,
        EventArgs e)
    {
        await OpenDoorAsync(5);
    }


    // ============================================================
    // HIKVISION DOOR OPEN
    // ============================================================

    private async Task OpenDoorAsync(
        int doorNumber)
    {
        try
        {
            StatusLabel.Text =
                $"Opening Door {doorNumber}...";

            // ----------------------------------------------------
            // Hikvision ISAPI URL
            // ----------------------------------------------------

            string url =
                $"http://{HikvisionIp}/ISAPI/AccessControl/RemoteControl/door/{doorNumber}";

            // ----------------------------------------------------
            // Hikvision XML
            // ----------------------------------------------------

            string xml =
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<RemoteControlDoor>
    <cmd>open</cmd>
</RemoteControlDoor>";

            // ----------------------------------------------------
            // Authentication
            // ----------------------------------------------------

            using var handler =
                new HttpClientHandler
                {
                    Credentials =
                        new NetworkCredential(
                            HikvisionUsername,
                            HikvisionPassword),

                    PreAuthenticate = false
                };

            using var client =
                new HttpClient(handler);

            client.Timeout =
                TimeSpan.FromSeconds(5);

            // ----------------------------------------------------
            // Content
            // ----------------------------------------------------

            using var content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");

            // ----------------------------------------------------
            // Send request
            // ----------------------------------------------------

            HttpResponseMessage response =
                await client.PutAsync(
                    url,
                    content);

            // ----------------------------------------------------
            // Success
            // ----------------------------------------------------

            if (response.IsSuccessStatusCode)
            {
                StatusLabel.Text =
                    $"Door {doorNumber} Opened";

                await DisplayAlertAsync(
                    "Door",
                    $"Door {doorNumber} opened successfully.",
                    "OK");
            }
            else
            {
                string responseText =
                    await response.Content
                        .ReadAsStringAsync();

                StatusLabel.Text =
                    $"Door {doorNumber} Failed";

                await DisplayAlertAsync(
                    "Door Error",
                    $"HTTP {(int)response.StatusCode}\n{responseText}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Door {doorNumber} Error";

            await DisplayAlertAsync(
                "Door Error",
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // PAGE DISAPPEARING
    // ============================================================

    protected override void OnDisappearing()
    {
#if ANDROID

        StopPlayer();

#endif

        base.OnDisappearing();
    }
}
