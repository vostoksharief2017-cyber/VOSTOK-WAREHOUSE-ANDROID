using System.Net;
using System.Text;

#if ANDROID
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
    // HIKVISION
    // ============================================================

    private const string HikvisionIp = "192.168.5.131";

    private const string HikvisionUsername = "admin";

    private const string HikvisionPassword = "Vos@3558817";

    private const string HikvisionRtsp =
        "rtsp://admin:Vos@3558817@192.168.5.131:554/Streaming/Channels/102";


    private readonly Dictionary<string, string> Cameras =
        new()
        {
            {
                "Hikvision Camera",
                HikvisionRtsp
            }
        };


#if ANDROID

    // IMPORTANT:
    // Use the .NET binding interface instead of the Java
    // ExoPlayer class name.

    private IExoPlayer? _player;

    private SurfaceView? _surfaceView;

#endif


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainPage()
    {
        InitializeComponent();

        foreach (string camera in Cameras.Keys)
        {
            CameraPicker.Items.Add(camera);
        }

        if (CameraPicker.Items.Count > 0)
        {
            CameraPicker.SelectedIndex = 0;
        }

        StatusLabel.Text = "Ready";
    }


    // ============================================================
    // APPEARING
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID

        try
        {
            StatusLabel.Text = "Starting RTSP...";

            await Task.Delay(500);

            CreateVideoSurface();

            await Task.Delay(500);

            StartRtsp();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "RTSP Error";

            System.Diagnostics.Debug.WriteLine(
                "RTSP START ERROR: " + ex);
        }

#else

        StatusLabel.Text = "Android required";

#endif
    }


#if ANDROID

    // ============================================================
    // CREATE SURFACE
    // ============================================================

    private void CreateVideoSurface()
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


    // ============================================================
    // ADD SURFACE TO MAUI
    // ============================================================

    private void AddSurfaceToPage()
    {
        if (_surfaceView == null)
            return;

        if (VideoContainer.Handler?.PlatformView
            is Android.Views.ViewGroup parent)
        {
            try
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "SURFACE ERROR: " + ex);
            }

            return;
        }

        VideoContainer.HandlerChanged +=
            VideoContainer_HandlerChanged;
    }


    private void VideoContainer_HandlerChanged(
        object? sender,
        EventArgs e)
    {
        VideoContainer.HandlerChanged -=
            VideoContainer_HandlerChanged;

        MainThread.BeginInvokeOnMainThread(
            AddSurfaceToPage);
    }


    // ============================================================
    // START RTSP
    // ============================================================

    private void StartRtsp()
    {
        try
        {
            if (_surfaceView == null)
            {
                CreateVideoSurface();
            }

            StopPlayer();

            StatusLabel.Text =
                "Creating Media3 player...";


            // ====================================================
            // MEDIA3 PLAYER
            // ====================================================

            var context =
                Android.App.Application.Context;

            _player =
                new ExoPlayerBuilder(context)
                    .Build();


            // ====================================================
            // CONNECT SURFACE
            // ====================================================

            if (_surfaceView != null)
            {
                _player.SetVideoSurfaceView(
                    _surfaceView);
            }


            // ====================================================
            // RTSP URI
            // ====================================================

            var uri =
                Android.Net.Uri.Parse(
                    HikvisionRtsp);


            // ====================================================
            // MEDIA ITEM
            // ====================================================

            var mediaItem =
                MediaItem.FromUri(uri);


            // ====================================================
            // RTSP MEDIA SOURCE
            // ====================================================
            //
            // IMPORTANT:
            // The Microsoft binding does not expose the Java
            // RtspMediaSource.Factory exactly as shown in the
            // Android Java documentation.
            //
            // Therefore we configure RTSP through the
            // ExoPlayerBuilder media-source factory.
            // ====================================================

            


            // ====================================================
            // SET SOURCE
            // ====================================================

            _player.SetMediaSource(
                mediaSource);


            // ====================================================
            // PREPARE
            // ====================================================

            StatusLabel.Text =
                "Connecting to Hikvision...";

            _player.Prepare();


            // ====================================================
            // PLAY
            // ====================================================

            _player.PlayWhenReady =
                true;


            StatusLabel.Text =
                "RTSP connected - waiting for video...";
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
                    await DisplayAlert(
                        "RTSP Error",
                        ex.Message,
                        "OK");
                });
        }
    }


    // ============================================================
    // STOP PLAYER
    // ============================================================

    private void StopPlayer()
    {
        try
        {
            if (_player != null)
            {
                _player.Stop();

                _player.Release();

                _player.Dispose();

                _player = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "PLAYER STOP ERROR: " + ex);
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
        if (CameraPicker.SelectedIndex < 0)
            return;

        string cameraName =
            CameraPicker.Items[
                CameraPicker.SelectedIndex];

        CameraStatus.Text =
            cameraName;

#if ANDROID

        MainThread.BeginInvokeOnMainThread(
            StartRtsp);

#endif
    }


    // ============================================================
    // DISAPPEARING
    // ============================================================

    protected override void OnDisappearing()
    {
#if ANDROID

        StopPlayer();

#endif

        base.OnDisappearing();
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


            string url =
                $"http://{HikvisionIp}/ISAPI/AccessControl/RemoteControl/door/{doorNumber}";


            string xml =
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<RemoteControlDoor>
    <cmd>open</cmd>
</RemoteControlDoor>";


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


            using var content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");


            HttpResponseMessage response =
                await client.PutAsync(
                    url,
                    content);


            if (response.IsSuccessStatusCode)
            {
                StatusLabel.Text =
                    $"Door {doorNumber} Opened";

                await DisplayAlert(
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
                    $"Door {doorNumber} failed";

                await DisplayAlert(
                    "Door Error",
                    $"HTTP {(int)response.StatusCode}\n{responseText}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Door {doorNumber} error";

            await DisplayAlert(
                "Door Error",
                ex.Message,
                "OK");
        }
    }
}
