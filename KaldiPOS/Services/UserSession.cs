using KaldiPOS.Data;

namespace KaldiPOS.Services;

public static class UserSession
{
    public static UserRecord? CurrentUser { get; private set; }

    private static readonly HashSet<string> _permissions = new();

    public static bool IsLoggedIn =>
        CurrentUser is not null;

    public static IReadOnlyCollection<string> CurrentPermissions =>
        _permissions;

    public static async Task StartAsync(UserRecord user)
    {
        CurrentUser = user;

        _permissions.Clear();

        NetworkSettings settings =
            NetworkSettingsService.Load();

        List<PermissionRecord> permissions;

        if (string.Equals(
                settings.Mode,
                "Client",
                StringComparison.OrdinalIgnoreCase))
        {
            permissions =
                await LocalClientService.GetUserPermissionsAsync(
                    user.Id);
        }
        else
        {
            permissions =
                Database.GetUserPermissions(user.Id);
        }

        foreach (PermissionRecord permission in permissions)
        {
            if (permission.IsAllowed)
                _permissions.Add(permission.PermissionKey);
        }
    }

    public static bool HasPermission(string permissionKey)
    {
        if (CurrentUser is null)
            return false;

        // Yönetici her zaman tam yetkilidir.
        if (CurrentUser.Role.Equals("Yönetici",
                StringComparison.OrdinalIgnoreCase))
            return true;

        return _permissions.Contains(permissionKey);
    }

    public static void Clear()
    {
        CurrentUser = null;
        _permissions.Clear();
    }
}