using KaldiPOS.Data;

namespace KaldiPOS.Services
{
    public static class UserSession
    {
        public static UserRecord? CurrentUser { get; private set; }

        public static bool IsLoggedIn =>
            CurrentUser is not null;

        public static void Start(UserRecord user)
        {
            CurrentUser = user;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}