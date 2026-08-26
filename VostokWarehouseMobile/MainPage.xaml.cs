using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LibVLCSharp.Shared;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    // ============================================================
    // HIKVISION LOGIN
    // ============================================================

    private const string UserName = "admin";

    // Hikvision password
    private const string HikvisionPassword = "Vos@3558817";


    // ============================================================
    // LIBVLC
    // ============================================================

    private readonly LibVLC _libVLC;

    private MediaPlayer? _mediaPlayer;


    // ============================================================
    // CAMERA LIST
    // ============================================================

    private readonly Dictionary<string, string> cameras = new()
    {
        {
            "WareHouse-7",
            "rtsp://admin:PASSWORD@192.168.5.131:554/Streaming/Channels/101"
        },

        {
            "WareHouse-9",
            "rtsp://admin:PASSWORD@192.168.5.133:554/Streaming/Channels/101"
        },

        {
            "WareHouse-4",
            "rtsp://admin:PASSWORD@192.168.5.134:554/Streaming/Channels/101"
        },

        {
            "WareHouse-5",
            "rtsp://admin:PASSWORD@192.168.5.132:554/Streaming/Channels/101"
        }
    };


    // ============================================================
    // DOOR LIST
    // ============================================================

    private readonly Dictionary<string, string> doors = new()
    {
        {
            "Door 1 - WH7",
            "192.168.5.131"
        },

        {
            "Door 2 - WH9",
            "192.168.5.133"
        },

        {
            "Door 4 - WH4",
            "192.168.5.134"
        },

        {
            "Door 5 - WH5",
            "192.168.5.132"
        }
    };


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainPage()
    {
        InitializeComponent();

        // Initialize VLC
        _libVLC = new LibVLC();

        // Add cameras to Picker
        foreach (var cameraName in cameras.Keys)
        {
            CameraPicker.Items.Add(cameraName);
        }

        StatusLabel.Text =
            "Ready - Hikvision system configured.";
    }


    // ============================================================
    // CAMERA SELECTION
    // ============================================================

    private void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem is not string cameraName)
        {
            return;
        }

        if (!cameras.TryGetValue(
                cameraName,
                out string? rtspTemplate))
        {
            return;
        }

        try
        {
            // Stop previous camera
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }


            // ----------------------------------------------------
            // Put Hikvision password into RTSP URL
            // ----------------------------------------------------

            string encodedPassword =
                Uri.EscapeDataString(
                    HikvisionPassword);

            string rtsp =
                rtspTemplate.Replace(
                    "PASSWORD",
                    encodedPassword);


            // ----------------------------------------------------
            // Create VLC MediaPlayer
            // ----------------------------------------------------

            _mediaPlayer =
                new MediaPlayer(_libVLC);


            // ----------------------------------------------------
            // IMPORTANT
            //
            // Your current project does not have VideoView
            // available from LibVLCSharp.MAUI.
            //
            // Therefore this code starts the RTSP stream,
            // but the actual Android video rendering requires
            // the correct LibVLCSharp Android VideoView setup.
            // ----------------------------------------------------

            using var media =
                new Media(
                    _libVLC,
                    new Uri(rtsp));


            _mediaPlayer.Play(media);


            CameraStatus.Text =
                $"Live view started: {cameraName}";

            StatusLabel.Text =
                $"Connected to {cameraName}";
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                "Camera connection failed.";

            StatusLabel.Text =
                ex.Message;
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
    // OPEN DOOR
    //
    // Uses Hikvision Digest Authentication
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
                "Door configuration not found.",
                "OK");

            return;
        }


        string url =
            $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1";


        string requestUri =
            "/ISAPI/AccessControl/RemoteControl/door/1";


        // --------------------------------------------------------
        // Hikvision ISAPI XML
        // --------------------------------------------------------

        string xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<RemoteControlDoor>" +
            "<cmd>open</cmd>" +
            "</RemoteControlDoor>";


        try
        {
            using var client =
                new HttpClient();

            client.Timeout =
                TimeSpan.FromSeconds(10);


            // ====================================================
            // STEP 1
            // Initial request
            // ====================================================

            using var firstRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);


            firstRequest.Content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");


            using var firstResponse =
                await client.SendAsync(
                    firstRequest);


            // ====================================================
            // Some devices may accept Basic authentication
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
            // We expect 401 for Digest Authentication
            // ====================================================

            if (firstResponse.StatusCode !=
                HttpStatusCode.Unauthorized)
            {
                string error =
                    await firstResponse.Content
                        .ReadAsStringAsync();


                await DisplayAlertAsync(
                    "Door Control",
                    $"Device returned HTTP " +
                    $"{(int)firstResponse.StatusCode}" +
                    $"\n\n{error}",
                    "OK");

                return;
            }


            // ====================================================
            // Get WWW-Authenticate
            // ====================================================

            if (!firstResponse.Headers.TryGetValues(
                    "WWW-Authenticate",
                    out IEnumerable<string>? values))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Hikvision returned HTTP 401 " +
                    "but did not provide a " +
                    "WWW-Authenticate header.",
                    "OK");

                return;
            }


            string authHeader =
                values.FirstOrDefault() ?? "";


            if (!authHeader.StartsWith(
                    "Digest",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Device did not return Digest authentication." +
                    "\n\n" +
                    authHeader,
                    "OK");

                return;
            }


            // ====================================================
            // Parse Digest Challenge
            // ====================================================

            Dictionary<string, string> challenge =
                ParseDigestChallenge(
                    authHeader);


            if (!challenge.TryGetValue(
                    "realm",
                    out string? realm))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Digest realm was not provided.",
                    "OK");

                return;
            }


            if (!challenge.TryGetValue(
                    "nonce",
                    out string? nonce))
            {
                await DisplayAlertAsync(
                    "Authentication Error",
                    "Digest nonce was not provided.",
                    "OK");

                return;
            }


            // ====================================================
            // QOP
            // ====================================================

            string qop = "auth";


            if (challenge.TryGetValue(
                    "qop",
                    out string? qopValue))
            {
                string[] qops =
                    qopValue.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries);


                if (!qops.Any(
                        x => x.Trim().Equals(
                            "auth",
                            StringComparison.OrdinalIgnoreCase)))
                {
                    qop = qops.FirstOrDefault()?.Trim()
                          ?? "auth";
                }
            }


            // ====================================================
            // Digest values
            // ====================================================

            string nonceCount =
                "00000001";


            string clientNonce =
                CreateCnonce();


            string ha1 =
                Md5Hash(
                    $"{UserName}:{realm}:{HikvisionPassword}");


            string ha2 =
                Md5Hash(
                    $"PUT:{requestUri}");


            string responseDigest =
                Md5Hash(
                    $"{ha1}:{nonce}:{nonceCount}:" +
                    $"{clientNonce}:{qop}:{ha2}");


            // ====================================================
            // Authorization Header
            // ====================================================

            var authorization =
                new StringBuilder();


            authorization.Append(
                "Digest ");


            authorization.Append(
                $"username=\"{EscapeDigest(UserName)}\", ");


            authorization.Append(
                $"realm=\"{EscapeDigest(realm)}\", ");


            authorization.Append(
                $"nonce=\"{EscapeDigest(nonce)}\", ");


            authorization.Append(
                $"uri=\"{EscapeDigest(requestUri)}\", ");


            authorization.Append(
                "algorithm=MD5, ");


            authorization.Append(
                $"qop={qop}, ");


            authorization.Append(
                $"nc={nonceCount}, ");


            authorization.Append(
                $"cnonce=\"{clientNonce}\", ");


            authorization.Append(
                $"response=\"{responseDigest}\"");


            if (challenge.TryGetValue(
                    "opaque",
                    out string? opaque) &&
                !string.IsNullOrWhiteSpace(
                    opaque))
            {
                authorization.Append(
                    $", opaque=\"{EscapeDigest(opaque)}\"");
            }


            // ====================================================
            // STEP 2
            // Authenticated PUT
            // ====================================================

            using var authenticatedRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);


            authenticatedRequest.Headers
                .TryAddWithoutValidation(
                    "Authorization",
                    authorization.ToString());


            authenticatedRequest.Content =
                new StringContent(
                    xml,
                    Encoding.UTF8,
                    "application/xml");


            using var response =
                await client.SendAsync(
                    authenticatedRequest);


            string responseText =
                await response.Content
                    .ReadAsStringAsync();


            // ====================================================
            // RESULT
            // ====================================================

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"{doorName} opened successfully.",
                    "OK");


                StatusLabel.Text =
                    $"{doorName} - OPEN COMMAND SUCCESS";
            }
            else
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"Door command failed." +
                    $"\n\nDoor: {doorName}" +
                    $"\nIP: {ip}" +
                    $"\nHTTP: {(int)response.StatusCode}" +
                    $"\n\n{responseText}",
                    "OK");


                StatusLabel.Text =
                    $"{doorName} - Command Failed";
            }
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync(
                "Connection Error",
                $"Cannot connect to Hikvision device." +
                $"\n\nIP: {ip}" +
                $"\n\n{ex.Message}",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync(
                "Timeout",
                $"Hikvision device did not respond." +
                $"\n\nIP: {ip}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Door Control Error",
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // PARSE DIGEST CHALLENGE
    // ============================================================

    private static Dictionary<string, string>
        ParseDigestChallenge(
            string header)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);


        if (header.StartsWith(
                "Digest ",
                StringComparison.OrdinalIgnoreCase))
        {
            header =
                header.Substring(7);
        }


        MatchCollection matches =
            Regex.Matches(
                header,
                @"(\w+)\s*=\s*(?:""([^""]*)""|([^,\s]+))");


        foreach (Match match in matches)
        {
            string key =
                match.Groups[1].Value;


            string value =
                match.Groups[2].Success
                    ? match.Groups[2].Value
                    : match.Groups[3].Value;


            result[key] = value;
        }


        return result;
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


        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }


    // ============================================================
    // CLIENT NONCE
    // ============================================================

    private static string CreateCnonce()
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(16);


        return Convert.ToHexString(bytes)
            .ToLowerInvariant();
    }


    // ============================================================
    // ESCAPE DIGEST VALUE
    // ============================================================

    private static string EscapeDigest(
        string value)
    {
        return value
            .Replace(
                "\\",
                "\\\\")
            .Replace(
                "\"",
                "\\\"");
    }


    // ============================================================
    // CLEANUP
    // ============================================================

    protected override void OnDisappearing()
    {
        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;

            _libVLC.Dispose();
        }
        catch
        {
            // Ignore cleanup errors
        }

        base.OnDisappearing();
    }
}
