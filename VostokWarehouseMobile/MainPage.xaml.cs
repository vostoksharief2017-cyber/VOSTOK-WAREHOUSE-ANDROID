using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LibVLCSharp.Shared;

namespace VostokWarehouseMobile;

public partial class MainPage : ContentPage
{
    private readonly LibVLC _libVLC;
    private MediaPlayer? _mediaPlayer;

    // =========================================================
    // HIKVISION LOGIN
    // =========================================================
    private const string UserName = "admin";
    private const string HikPassword = "Vos@3558817";

    // =========================================================
    // CAMERAS
    // =========================================================
    private readonly Dictionary<string, string> cameras = new()
    {
        ["WareHouse-7"] =
            "rtsp://admin:Vos%403558817@192.168.5.131:554/Streaming/Channels/101",

        ["WareHouse-9"] =
            "rtsp://admin:Vos%403558817@192.168.5.133:554/Streaming/Channels/101",

        ["WareHouse-4"] =
            "rtsp://admin:Vos%403558817@192.168.5.134:554/Streaming/Channels/101",

        ["WareHouse-5"] =
            "rtsp://admin:Vos%403558817@192.168.5.132:554/Streaming/Channels/101"
    };

    // =========================================================
    // DOORS
    // Door 3 intentionally removed / disabled
    // =========================================================
    private readonly Dictionary<string, string> doors = new()
    {
        ["Door 1 - WH7"] = "192.168.5.131",
        ["Door 2 - WH9"] = "192.168.5.133",
        ["Door 4 - WH4"] = "192.168.5.134",
        ["Door 5 - WH5"] = "192.168.5.132"
    };

    public MainPage()
    {
        InitializeComponent();

        // VLC
        _libVLC = new LibVLC();

        foreach (var name in cameras.Keys)
        {
            CameraPicker.Items.Add(name);
        }

        // Password is fixed in code
        if (PasswordEntry != null)
        {
            PasswordEntry.Text = HikPassword;
        }
    }

    // =========================================================
    // CAMERA SELECTION
    // =========================================================
    private void CameraPicker_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (CameraPicker.SelectedItem is not string name)
            return;

        if (!cameras.TryGetValue(name, out var rtsp))
            return;

        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();

            _mediaPlayer = new MediaPlayer(_libVLC);

            // VideoView is intentionally not used here because
            // your current MAUI project does not expose VideoView.

            using var media = new Media(
                _libVLC,
                new Uri(rtsp));

            _mediaPlayer.Play(media);

            CameraStatus.Text = $"Playing: {name}";
        }
        catch (Exception ex)
        {
            CameraStatus.Text =
                $"Camera error: {ex.Message}";
        }
    }

    // =========================================================
    // DOOR BUTTONS
    // =========================================================
    private async void Door1_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 1 - WH7");
    }

    private async void Door2_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 2 - WH9");
    }

    private async void Door4_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 4 - WH4");
    }

    private async void Door5_Clicked(
        object sender,
        EventArgs e)
    {
        await OpenDoorAsync("Door 5 - WH5");
    }

    // =========================================================
    // HIKVISION DIGEST AUTHENTICATION
    // =========================================================
    private async Task OpenDoorAsync(string doorName)
    {
        if (!doors.TryGetValue(doorName, out var ip))
            return;

        const string username = UserName;
        const string password = HikPassword;

        string url =
            $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1";

        string requestUri =
            "/ISAPI/AccessControl/RemoteControl/door/1";

        string xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<RemoteControlDoor version=\"2.0\" " +
            "xmlns=\"http://www.isapi.org/ver20/XMLSchema\">" +
            "<cmd>open</cmd>" +
            "</RemoteControlDoor>";

        try
        {
            using var client = new HttpClient();

            client.Timeout =
                TimeSpan.FromSeconds(10);

            // =================================================
            // STEP 1
            // Send initial request WITHOUT authentication.
            // Hikvision should return HTTP 401 with:
            //
            // WWW-Authenticate: Digest realm="...",
            // nonce="...", qop="auth"
            // =================================================

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
                await client.SendAsync(firstRequest);

            // If device accepts without authentication
            if (firstResponse.IsSuccessStatusCode)
            {
                await ShowDoorSuccess(doorName);
                return;
            }

            // We specifically expect 401
            if (firstResponse.StatusCode !=
                HttpStatusCode.Unauthorized)
            {
                string firstError =
                    await firstResponse.Content.ReadAsStringAsync();

                await DisplayAlertAsync(
                    "Door Control",
                    $"Device returned HTTP " +
                    $"{(int)firstResponse.StatusCode}\n\n" +
                    firstError,
                    "OK");

                return;
            }

            // =================================================
            // STEP 2
            // Read WWW-Authenticate header
            // =================================================

            if (!firstResponse.Headers.TryGetValues(
                    "WWW-Authenticate",
                    out var authValues))
            {
                await DisplayAlertAsync(
                    "Door Control",
                    "Hikvision returned HTTP 401, " +
                    "but no WWW-Authenticate header was found.",
                    "OK");

                return;
            }

            string authHeader =
                authValues.FirstOrDefault() ?? "";

            if (!authHeader.StartsWith(
                    "Digest",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlertAsync(
                    "Door Control",
                    "The Hikvision device did not return " +
                    "Digest authentication.\n\n" +
                    authHeader,
                    "OK");

                return;
            }

            // =================================================
            // STEP 3
            // Parse Digest challenge
            // =================================================

            var challenge =
                ParseDigestChallenge(authHeader);

            if (!challenge.TryGetValue(
                    "realm",
                    out var realm) ||
                !challenge.TryGetValue(
                    "nonce",
                    out var nonce))
            {
                await DisplayAlertAsync(
                    "Door Control",
                    "Invalid Hikvision Digest challenge.",
                    "OK");

                return;
            }

            challenge.TryGetValue(
                "qop",
                out var qopValue);

            challenge.TryGetValue(
                "opaque",
                out var opaque);

            // =================================================
            // STEP 4
            // Generate Digest values
            // =================================================

            string qop = "auth";

            if (!string.IsNullOrWhiteSpace(qopValue))
            {
                var qops =
                    qopValue.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries);

                if (qops.Any(x =>
                    x.Trim().Equals(
                        "auth",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    qop = "auth";
                }
            }

            string nc = "00000001";

            string cnonce =
                CreateCnonce();

            string ha1 =
                Md5Hash(
                    $"{username}:{realm}:{password}");

            string ha2 =
                Md5Hash(
                    $"PUT:{requestUri}");

            string responseDigest =
                Md5Hash(
                    $"{ha1}:{nonce}:{nc}:{cnonce}:{qop}:{ha2}");

            // =================================================
            // STEP 5
            // Build Authorization header
            // =================================================

            var authorization =
                new StringBuilder();

            authorization.Append("Digest ");

            authorization.Append(
                $"username=\"{EscapeDigest(username)}\", ");

            authorization.Append(
                $"realm=\"{EscapeDigest(realm)}\", ");

            authorization.Append(
                $"nonce=\"{EscapeDigest(nonce)}\", ");

            authorization.Append(
                $"uri=\"{EscapeDigest(requestUri)}\", ");

            authorization.Append(
                $"algorithm=MD5, ");

            authorization.Append(
                $"qop={qop}, ");

            authorization.Append(
                $"nc={nc}, ");

            authorization.Append(
                $"cnonce=\"{cnonce}\", ");

            authorization.Append(
                $"response=\"{responseDigest}\"");

            if (!string.IsNullOrWhiteSpace(opaque))
            {
                authorization.Append(
                    $", opaque=\"{EscapeDigest(opaque)}\"");
            }

            // =================================================
            // STEP 6
            // Send authenticated PUT request
            // =================================================

            using var authenticatedRequest =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url);

            authenticatedRequest.Headers.TryAddWithoutValidation(
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
                await response.Content.ReadAsStringAsync();

            // =================================================
            // STEP 7
            // Result
            // =================================================

            if (response.IsSuccessStatusCode)
            {
                await ShowDoorSuccess(doorName);
            }
            else
            {
                await DisplayAlertAsync(
                    "Door Control",
                    $"Door command failed.\n\n" +
                    $"Door: {doorName}\n" +
                    $"IP: {ip}\n" +
                    $"HTTP: {(int)response.StatusCode}\n\n" +
                    responseText,
                    "OK");
            }
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Connection failed.\n\n" +
                $"Device: {ip}\n\n" +
                ex.Message,
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Connection timeout.\n\n" +
                $"Device: {ip}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Door Control",
                $"Door command error.\n\n" +
                ex.Message,
                "OK");
        }
    }

    // =========================================================
    // SUCCESS MESSAGE
    // =========================================================
    private async Task ShowDoorSuccess(
        string doorName)
    {
        await DisplayAlertAsync(
            "Door Control",
            $"{doorName} opened successfully.",
            "OK");
    }

    // =========================================================
    // PARSE WWW-AUTHENTICATE
    // =========================================================
    private static Dictionary<string, string>
        ParseDigestChallenge(string header)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        if (header.StartsWith(
                "Digest ",
                StringComparison.OrdinalIgnoreCase))
        {
            header = header.Substring(7);
        }

        var matches =
            Regex.Matches(
                header,
                @"(\w+)\s*=\s*(?:""([^""]*)""|([^,\s]+))");

        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;

            string value =
                match.Groups[2].Success
                    ? match.Groups[2].Value
                    : match.Groups[3].Value;

            result[key] = value;
        }

        return result;
    }

    // =========================================================
    // MD5
    // =========================================================
    private static string Md5Hash(string input)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(input);

        byte[] hash =
            MD5.HashData(bytes);

        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }

    // =========================================================
    // CNONCE
    // =========================================================
    private static string CreateCnonce()
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(16);

        return Convert.ToHexString(bytes)
            .ToLowerInvariant();
    }

    // =========================================================
    // DIGEST ESCAPE
    // =========================================================
    private static string EscapeDigest(
        string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    // =========================================================
    // SAVE PASSWORD BUTTON
    // =========================================================
    private void SavePassword_Clicked(
        object sender,
        EventArgs e)
    {
        if (PasswordEntry != null)
        {
            PasswordEntry.Text =
                HikPassword;
        }

        StatusLabel.Text =
            "Hikvision password configured.";
    }

    // =========================================================
    // CLEANUP
    // =========================================================
    protected override void OnDisappearing()
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;

        _libVLC.Dispose();

        base.OnDisappearing();
    }
}
