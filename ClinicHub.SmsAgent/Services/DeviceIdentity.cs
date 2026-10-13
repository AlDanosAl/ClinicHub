namespace ClinicHub.SmsAgent.Services;

/// <summary>
/// Постоянная личность телефона. Ключ устройства не меняется при переустановке приложения и очистке данных
/// (на Android это ANDROID_ID), поэтому сервер всегда узнаёт тот же самый телефон и не создаёт дубль.
/// </summary>
public static class DeviceIdentity
{
    /// <summary>Производитель и модель, например «samsung-SM-A515F».</summary>
    public static string Name => $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}";

    public static string Id
    {
        get
        {
#if ANDROID
            try
            {
                var v = Android.Provider.Settings.Secure.GetString(
                    Android.App.Application.Context.ContentResolver,
                    Android.Provider.Settings.Secure.AndroidId);
                // 9774d56d682e549c — известное «пустое» значение на некоторых старых прошивках.
                if (!string.IsNullOrWhiteSpace(v) && v != "9774d56d682e549c")
                    return v;
            }
            catch
            {
            }
#endif
            var saved = Preferences.Default.Get("agent.device_guid", "");
            if (saved.Length == 0)
            {
                saved = Guid.NewGuid().ToString("N");
                Preferences.Default.Set("agent.device_guid", saved);
            }
            return saved;
        }
    }
}
