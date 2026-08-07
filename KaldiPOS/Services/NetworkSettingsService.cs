using System.IO;
using System.Text.Json;

namespace KaldiPOS.Services;

public sealed class NetworkSettings
{
    public string Mode { get; set; } = "Server";
    public string ServerAddress { get; set; } = "localhost";
    public int Port { get; set; } = 5050;
}

public static class NetworkSettingsService
{
    private static readonly string SettingsDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "KaldiPOS");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDirectory, "network.json");

    public static string SettingsFilePath =>
    SettingsPath;

    public static NetworkSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var defaultSettings = new NetworkSettings();

                Save(defaultSettings);

                return defaultSettings;
            }

            string json = File.ReadAllText(SettingsPath);

            return JsonSerializer.Deserialize<NetworkSettings>(json)
                   ?? new NetworkSettings();
        }
        catch
        {
            return new NetworkSettings();
        }
    }

    public static void ConfigureServer(
    string serverAddress,
    int port = 5050)
    {
        Save(new NetworkSettings
        {
            Mode = "Server",
            ServerAddress = serverAddress.Trim(),
            Port = port
        });
    }

    public static void ConfigureClient(
        string serverAddress,
        int port = 5050)
    {
        Save(new NetworkSettings
        {
            Mode = "Client",
            ServerAddress = serverAddress.Trim(),
            Port = port
        });
    }

    public static void Save(NetworkSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);

        string json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(SettingsPath, json);
    }
}