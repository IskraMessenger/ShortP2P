using Microsoft.Extensions.Logging;
using ShortP2P.Auth;
using ShortP2P.Client.ProfileBackup;

namespace ShortP2P.MauiApp;

/// <summary>
/// TRL-10: export/import of the Logopass profile as a <c>.tlp</c> file.
/// Export lives on the chats toolbar, import on the login page (auto-login after apply).
/// </summary>
internal static class ProfileFileShare
{
    public static async Task ExportProfileAsync(
        Page host,
        AuthService auth,
        ProfileBackupService backup,
        ILogger logger)
    {
        var u = auth.CurrentUser;
        if (u == null)
            return;

        // The master password is standalone: set right away, not checked against anything.
        var master = await PasswordPromptPage.ShowNewAsync(host,
            "Export profile",
            "Create a master password for the file. You will need it to import the profile on another device.",
            "Master password",
            "Confirm master password").ConfigureAwait(true);
        if (master == null)
            return;

        byte[] bytes;
        try
        {
            var (ok, err, fileBytes) = await backup.ExportAsync(u.Id, master).ConfigureAwait(true);
            if (!ok || fileBytes == null)
            {
                logger.LogWarning("Profile export failed: {Error}", err);
                await host.DisplayAlert("Export profile", err ?? "Export failed.", "OK").ConfigureAwait(true);
                return;
            }

            bytes = fileBytes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Profile export failed");
            await host.DisplayAlert("Export profile", ex.Message, "OK").ConfigureAwait(true);
            return;
        }

        try
        {
            var name = $"shortp2p_profile_{DateTime.Now:dd.MM.yyyy_HH-mm-ss}.tlp";
            var temp = Path.Combine(FileSystem.CacheDirectory, name);
            await File.WriteAllBytesAsync(temp, bytes).ConfigureAwait(true);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export profile",
                File = new ShareFile(temp)
            }).ConfigureAwait(true);
            logger.LogInformation("Profile exported to {FileName}", name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Share .tlp profile file");
            await host.DisplayAlert("Export profile", ex.Message, "OK").ConfigureAwait(true);
        }
    }

    /// <summary>Returns true when the profile was applied and the user is signed in.</summary>
    public static async Task<bool> ImportProfileAsync(
        Page host,
        AuthService auth,
        ProfileBackupService backup,
        ILogger logger)
    {
        FileResult? picked;
        try
        {
            picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Import profile",
                FileTypes = TlpFileTypes
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Pick .tlp profile file");
            await host.DisplayAlert("Import profile", ex.Message, "OK").ConfigureAwait(true);
            return false;
        }

        if (picked == null)
            return false;

        byte[] bytes;
        try
        {
            await using var stream = await picked.OpenReadAsync().ConfigureAwait(true);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(true);
            bytes = ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Read .tlp profile file");
            await host.DisplayAlert("Import profile", ex.Message, "OK").ConfigureAwait(true);
            return false;
        }

        var master = await PasswordPromptPage.ShowAsync(host,
            "Import profile",
            "Enter the master password the file is encrypted with.",
            "Master password").ConfigureAwait(true);
        if (master == null)
            return false;

        try
        {
            var (result, user) = await backup.ImportAsync(bytes, master).ConfigureAwait(true);
            if (!result.Ok || user == null)
            {
                logger.LogWarning("Profile import failed: {Error}", result.Error);
                await host.DisplayAlert("Import profile",
                    result.Error ?? ProfileBackupService.ErrorBadFileOrPassword, "OK").ConfigureAwait(true);
                return false;
            }

            // Auto-login: persist the session exactly like a password sign-in. Server re-login
            // (AccountPassword) happens silently when GoToChatsAsync starts UserP2pRuntime.
            await auth.AdoptRestoredUserAsync(user).ConfigureAwait(true);
            logger.LogInformation(
                "Profile imported: {Nickname} id={Id}, servers={Servers}",
                user.Nickname,
                user.NetworkIdShort,
                result.Preview?.ServerCount ?? 0);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Profile import failed");
            await host.DisplayAlert("Import profile", ex.Message, "OK").ConfigureAwait(true);
            return false;
        }
    }

    private static readonly FilePickerFileType TlpFileTypes = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.WinUI] = [".tlp"],
            [DevicePlatform.Android] = ["*/*"]
        });
}
