using System.Net.Http;
using System.Text.Json;
using KaldiPOS.Data;

namespace KaldiPOS.Services;

public static class LocalClientService
{
    private static readonly HttpClient HttpClient = new();

    public static async Task<bool> PingAsync()
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/ping";

        try
        {
            string response =
                await HttpClient.GetStringAsync(url);

            return string.Equals(
                response,
                "KALDIPOS_OK",
                StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<TableRecord>> GetTablesAsync(
        string hall)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/tables?hall={Uri.EscapeDataString(hall)}";

        string json =
            await HttpClient.GetStringAsync(url);

        return JsonSerializer.Deserialize<List<TableRecord>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TableRecord>();
    }
}