using System.Net;
using System.Text;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Net;
using Android.Views;
using Android.Widget;

using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Rtsp;
using AndroidX.Media3.Ui;
#endif

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION SETTINGS
    // ============================================================

    private const string HikvisionIp = "192.168.5.131";

    private const string HikvisionUsername = "admin";

    private const string HikvisionPassword = "Vos@3558817";

    // Hikvision sub-stream
    private const string RtspUrl =
        "rtsp://admin:Vos@3558817@192.168.5.131:554/Streaming/Channels/102";


    // ============================================================
    // MEDIA3 PLAYER
    // ============================================================

#if ANDROID

    private ExoPlayer? _player;

    private PlayerView? _playerView;

#endif


    // ============================================================
    // CAMERA LIST
    // ============================================================

    private readonly Dictionary<string, string> Cameras =
        new()
        {
            { "Hikvision Camera", RtspUrl }
        };


    // ============================================================
    // PAGE
    // ============================================================

    public MainPage()
    {
        InitializeComponent();

        foreach (var camera in Cameras.Keys)
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
    // PAGE APPEARING
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(500);

#if ANDROID

        try
        {
            StatusLabel.Text = "Starting RTSP player...";

            CreatePlayerView();

            await Task.Delay(300);

            StartRtsp();

        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Video error";

            System.Diagnostics.Debug.WriteLine(
                "RTSP START ERROR: " + ex);
        }

#endif
    }


    // ============================================================
    // CREATE MEDIA3 PLAYER VIEW
    // ============================================================

#if ANDROID

    private void CreatePlayerView()
    {
        if (_playerView != null)
            return;

        var context =
            Android.App.Application.Context;

        _playerView = new PlayerView(context);

        _playerView.LayoutParameters =
            new Android.Widget.FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);

        _playerView.UseController = false;

        _playerView.KeepScreenOn = true;

        VideoContainer.Content = null;

        var nativeView =
            new Android.Views.ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);

        _playerView.LayoutParameters = nativeView;

        AddNativePlayerView();

        StatusLabel.Text = "Player ready";
    }


    private void AddNativePlayerView()
    {
        if (_playerView == null)
            return;

        if (VideoContainer.Handler?.PlatformView is Android.Views.ViewGroup parent)
        {
            try
            {
                if (_playerView.Parent is Android.Views.ViewGroup oldParent)
                {
                    oldParent.RemoveView(_playerView);
                }

                parent.AddView(_playerView);

                _playerView.LayoutParameters =
                    new Android.Views.ViewGroup.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.MatchParent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "PLAYER VIEW ADD ERROR: " + ex);
            }
        }
        else
        {
            VideoContainer.HandlerChanged +=
                VideoContainer_HandlerChanged;
        }
    }


    private void VideoContainer_HandlerChanged(
        object? sender,
        EventArgs e)
    {
        VideoContainer.HandlerChanged -=
            VideoContainer_HandlerChanged;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_playerView == null)
                    return;

                if (VideoContainer.Handler?.PlatformView
                    is Android.Views.ViewGroup parent)
                {
                    if (_playerView.Parent
                        is Android.Views.ViewGroup oldParent)
                    {
                        oldParent.RemoveView(_playerView);
                    }

                    parent.AddView(_playerView);

                    _playerView.LayoutParameters =
                        new Android.Views.ViewGroup.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "HANDLER PLAYER ERROR: " + ex);
            }
        });
    }

#endif


    // ============================================================
    // START RTSP
    // ============================================================

#if ANDROID

    private void StartRtsp()
    {
        try
        {
            if (_playerView == null)
            {
                CreatePlayerView();
            }

            StopPlayer();

            var context =
                Android.App.Application.Context;


            // ----------------------------------------------------
            // CREATE EXOPLAYER
            // ----------------------------------------------------

            _player =
                new ExoPlayer.Builder(context)
                    .Build();


            // ----------------------------------------------------
            // CONNECT PLAYER TO PLAYER VIEW
            // ----------------------------------------------------

            _playerView!.Player = _player;


            // ----------------------------------------------------
            // RTSP MEDIA ITEM
            // ----------------------------------------------------

            var uri =
                Android.Net.Uri.Parse(RtspUrl);

            var mediaItem =
                MediaItem.FromUri(uri);


            // ----------------------------------------------------
            // RTSP MEDIA SOURCE
            // ----------------------------------------------------

            var rtspFactory =
                new RtspMediaSource.Factory();

            // Force RTP over TCP.
            // This is normally more reliable for CCTV networks.

            rtspFactory.SetForceUseRtpTcp(true);


            var mediaSource =
                rtspFactory.CreateMediaSource(mediaItem);


            // ----------------------------------------------------
            // SET MEDIA SOURCE
            // ----------------------------------------------------

            _player.SetMediaSource(mediaSource);


            // ----------------------------------------------------
            // PREPARE
            // ----------------------------------------------------

            _player.Prepare();


            // ----------------------------------------------------
            // PLAY
            // ----------------------------------------------------

            _player.PlayWhenReady = true;


            StatusLabel.Text =
                "Connecting to Hikvision RTSP...";

        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                "RTSP error: " + ex.Message;

            System.Diagnostics.Debug.WriteLine(
                "RTSP ERROR: " + ex);
        }
    }

#endif


    // ============================================================
    // CAMERA CHANGED
    // ============================================================

    private void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedIndex < 0)
            return;

        var name =
            CameraPicker.Items[
                CameraPicker.SelectedIndex];

        CameraStatus.Text =
            name;

#if ANDROID

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StartRtsp();
        });

#endif
    }


    // ============================================================
    // STOP PLAYER
    // ============================================================

#if ANDROID

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

            if (_playerView != null)
            {
                _playerView.Player = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "STOP PLAYER ERROR: " + ex);
        }
    }

#endif


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

    private async Task OpenDoorAsync(int doorNumber)
    {
        try
        {
            StatusLabel.Text =
                $"Opening Door {doorNumber}...";


            // Hikvision Access Control API
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


            var response =
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
                    await response.Content.ReadAsStringAsync();

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
