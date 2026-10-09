using Microsoft.Extensions.Logging;
using ShortP2P.Auth;
using ShortP2P.Client.ProfileBackup;
using ShortP2P.Crypto;

namespace ShortP2P.WinForms;

/// <summary>
/// TRL-10: export/import of the Logopass profile as a <c>.tlp</c> file (WinForms dialogs).
/// Export lives in app settings, import on the login form (auto-login after apply).
/// </summary>
internal static class ProfileFileShare
{
    public static async Task ExportProfileAsync(
        IWin32Window owner,
        AuthService auth,
        ProfileBackupService backup,
        ILogger logger)
    {
        var u = auth.CurrentUser;
        if (u == null)
            return;

        // Мастер-пароль — самостоятельный: задаётся сразу и ни с чем не сравнивается.
        var master = PasswordPromptForm.PromptNew(owner,
            "Экспорт профиля",
            "Придумайте мастер-пароль для файла. Он понадобится при импорте профиля на другом устройстве.",
            "Мастер-пароль",
            "Повторите мастер-пароль");
        if (master == null)
            return;

        byte[] bytes;
        try
        {
            var (ok, err, fileBytes) = await backup.ExportAsync(u.Id, master).ConfigureAwait(true);
            if (!ok || fileBytes == null)
            {
                logger.LogWarning("Profile export failed: {Error}", err);
                MessageBox.Show(owner, LocalizeError(err), "Экспорт профиля",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bytes = fileBytes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Profile export failed");
            MessageBox.Show(owner, ex.Message, "Экспорт профиля",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var save = new SaveFileDialog
        {
            Title = "Экспорт профиля",
            Filter = "Профиль ShortP2P (*.tlp)|*.tlp|Все файлы (*.*)|*.*",
            DefaultExt = ".tlp",
            AddExtension = true,
            FileName = $"shortp2p_profile_{DateTime.Now:dd.MM.yyyy_HH-mm-ss}.tlp"
        };
        if (save.ShowDialog(owner) != DialogResult.OK)
            return;

        try
        {
            await File.WriteAllBytesAsync(save.FileName, bytes).ConfigureAwait(true);
            logger.LogInformation("Profile exported to {FileName}", save.FileName);
            MessageBox.Show(owner, "Профиль экспортирован.", "Экспорт профиля",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Write .tlp profile file");
            MessageBox.Show(owner, ex.Message, "Экспорт профиля",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Returns true when the profile was applied and the user is signed in.</summary>
    public static async Task<bool> ImportProfileAsync(
        IWin32Window owner,
        AuthService auth,
        ProfileBackupService backup,
        ILogger logger)
    {
        using var open = new OpenFileDialog
        {
            Title = "Импорт профиля",
            Filter = "Профиль ShortP2P (*.tlp)|*.tlp|Все файлы (*.*)|*.*",
            CheckFileExists = true
        };
        if (open.ShowDialog(owner) != DialogResult.OK)
            return false;

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(open.FileName).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Read .tlp profile file");
            MessageBox.Show(owner, ex.Message, "Импорт профиля",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var master = PasswordPromptForm.Prompt(owner,
            "Импорт профиля",
            "Введите мастер-пароль, которым зашифрован файл.",
            "Мастер-пароль");
        if (master == null)
            return false;

        try
        {
            var (result, user) = await backup.ImportAsync(bytes, master).ConfigureAwait(true);
            if (!result.Ok || user == null)
            {
                logger.LogWarning("Profile import failed: {Error}", result.Error);
                MessageBox.Show(owner, LocalizeError(result.Error), "Импорт профиля",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Auto-login: persist the session exactly like a password sign-in. Server re-login
            // (AccountPassword) happens silently when MainChatsForm starts UserP2pRuntime.
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
            MessageBox.Show(owner, ex.Message, "Импорт профиля",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    internal static string LocalizeError(string? err) => err switch
    {
        ProfileBackupService.ErrorMasterPasswordRequired => "Введите мастер-пароль.",
        ProfileBackupService.ErrorUnsupportedSaltSize =>
            "Экспорт невозможен: аккаунт повреждён (нестандартная соль пароля).",
        ProfileBackupService.ErrorBadFileOrPassword => "Неверный мастер-пароль или повреждённый файл.",
        ProfileBackupService.ErrorUnsupportedPasswordParams => "Неподдерживаемые параметры пароля в файле.",
        ProfileBackupService.ErrorMissingKeys => "В файле отсутствуют ключи RSA.",
        ProfileBackupService.ErrorInvalidServerUrl => "Некорректный адрес сервера в файле.",
        ProfileBackupService.ErrorNicknameTaken => "Ник уже используется другим локальным аккаунтом.",
        null => "Неверный мастер-пароль или повреждённый файл.",
        _ => err
    };
}
