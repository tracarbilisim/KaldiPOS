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

    public static async Task<UserRecord?> VerifyUserPinAsync(
    string pin)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/login?pin={Uri.EscapeDataString(pin)}";

        try
        {
            using HttpResponseMessage response =
                await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            string json =
                await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<UserRecord>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return null;
        }
    }

    public static async Task<List<ProductRecord>> GetProductsAsync()
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/products";

        string json = await HttpClient.GetStringAsync(url);

        return JsonSerializer.Deserialize<List<ProductRecord>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ProductRecord>();
    }

    public static async Task<List<string>> GetCategoriesAsync()
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/categories";

        string json = await HttpClient.GetStringAsync(url);

        return JsonSerializer.Deserialize<List<string>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<string>();
    }

    public static async Task<List<SavedOrderItem>> LoadOpenOrderAsync(
        string tableName)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/open-order?table={Uri.EscapeDataString(tableName)}";

        string json = await HttpClient.GetStringAsync(url);

        return JsonSerializer.Deserialize<List<SavedOrderItem>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SavedOrderItem>();
    }

    public static async Task SaveOpenOrderAsync(
    string tableName,
    IEnumerable<SavedOrderItem> items)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/save-open-order?table={Uri.EscapeDataString(tableName)}";

        string json =
            JsonSerializer.Serialize(items);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<bool> PrintPreparationTicketsAsync(
    string tableName,
    IEnumerable<PreparationTicketItem> items)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/print-preparation";

        var request = new
        {
            TableName = tableName,
            Items = items.ToList()
        };

        string json =
            JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();

        string result =
            await response.Content.ReadAsStringAsync();

        return bool.Parse(result);
    }

    public static async Task MarkOpenOrderSentAsync(
        string tableName)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/mark-order-sent?table={Uri.EscapeDataString(tableName)}";

        using HttpResponseMessage response =
            await HttpClient.PostAsync(
                url,
                new StringContent(string.Empty));

        response.EnsureSuccessStatusCode();
    }

    public static async Task<List<PermissionRecord>>
        GetUserPermissionsAsync(int userId)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/user-permissions?userId={userId}";

        string json =
            await HttpClient.GetStringAsync(url);

        return JsonSerializer.Deserialize<List<PermissionRecord>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<PermissionRecord>();
    }

    public static async Task CancelOpenOrderAsync(
    string tableName,
    string reason,
    string userName)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/cancel-order";

        var request = new
        {
            TableName = tableName,
            Reason = reason,
            UserName = userName
        };

        string json =
            JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task TransferOpenOrderAsync(
    string sourceTable,
    string targetTable)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/transfer-order";

        var request = new
        {
            SourceTable = sourceTable,
            TargetTable = targetTable
        };

        string json =
            JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task DeleteOpenOrderAsync(
    string tableName)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/delete-open-order";

        var request = new
        {
            TableName = tableName
        };

        string json =
            JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task TransferProductsAsync(
    string sourceTable,
    string targetTable,
    IEnumerable<SavedOrderItem> items)
    {
        NetworkSettings settings =
            NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/transfer-products";

        var request = new
        {
            SourceTable = sourceTable,
            TargetTable = targetTable,
            Items = items.ToList()
        };

        string json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task RecordProductCancellationAsync(
    string tableName,
    int productId,
    string productName,
    int quantity,
    decimal unitPrice,
    string reason,
    string userName)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/cancel-product";

        var request = new
        {
            TableName = tableName,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Reason = reason,
            UserName = userName
        };

        string json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<decimal> GetOpenOrderPaidTotalAsync(
    string tableName)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}" +
            $"/api/order-paid-total?table={Uri.EscapeDataString(tableName)}";

        string text = await HttpClient.GetStringAsync(url);

        return decimal.Parse(
            text,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task AddOpenOrderPaymentAsync(
        string tableName,
        string paymentType,
        decimal amount,
        string description)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/add-payment";

        var request = new
        {
            TableName = tableName,
            PaymentType = paymentType,
            Amount = amount,
            Description = description
        };

        string json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task CloseOpenOrderAsync(
        string tableName,
        string paymentType,
        decimal totalAmount)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/close-order";

        var request = new
        {
            TableName = tableName,
            PaymentType = paymentType,
            TotalAmount = totalAmount
        };

        string json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<bool> ProcessProductPaymentAsync(
        string tableName,
        IEnumerable<SavedOrderItem> items,
        string paymentType,
        decimal amount,
        string description)
    {
        NetworkSettings settings = NetworkSettingsService.Load();

        string url =
            $"http://{settings.ServerAddress}:{settings.Port}/api/product-payment";

        var request = new
        {
            TableName = tableName,
            Items = items.ToList(),
            PaymentType = paymentType,
            Amount = amount,
            Description = description
        };

        string json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.PostAsync(url, content);

        response.EnsureSuccessStatusCode();

        string result =
            await response.Content.ReadAsStringAsync();

        return bool.Parse(result);
    }
}