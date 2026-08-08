using KaldiPOS.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KaldiPOS.Services;

public sealed class LocalServerService
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _listener?.IsListening == true;

    public void Start(
        string address,
        int port = 5050)
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();

        _listener.Prefixes.Add(
            $"http://{address}:{port}/");

        _listener.Start();

        _ = ListenAsync(_cts.Token);
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsRunning)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = HandleAsync(context);
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static async Task HandleAsync(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath;

        if (path == "/api/ping")
        {
            var bytes = Encoding.UTF8.GetBytes("KALDIPOS_OK");

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
        else if (path == "/api/tables")
        {
            string hall =
                context.Request.QueryString["hall"] ?? "Salon";

            var tables = Database.GetTables(hall);

            string json = JsonSerializer.Serialize(tables);
            var bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }

        else if (path == "/api/login")
        {
            string pin =
                context.Request.QueryString["pin"] ?? string.Empty;

            UserRecord? user =
                Database.VerifyUserPin(pin);

            if (user is null)
            {
                context.Response.StatusCode = 401;
            }
            else
            {
                string json =
                    JsonSerializer.Serialize(user);

                var bytes =
                    Encoding.UTF8.GetBytes(json);

                context.Response.StatusCode = 200;
                context.Response.ContentType =
                    "application/json; charset=utf-8";
                context.Response.ContentLength64 =
                    bytes.Length;

                await context.Response.OutputStream.WriteAsync(bytes);
            }
        }

        else if (path == "/api/products")
        {
            var products = Database.GetProducts();

            string json = JsonSerializer.Serialize(products);
            var bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
        else if (path == "/api/categories")
        {
            var categories = Database.GetCategories();

            string json = JsonSerializer.Serialize(categories);
            var bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
        else if (path == "/api/open-order")
        {
            string table =
                context.Request.QueryString["table"] ?? string.Empty;

            var order = Database.LoadOpenOrder(table);

            string json = JsonSerializer.Serialize(order);
            var bytes = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }

        else if (path == "/api/save-open-order" &&
         context.Request.HttpMethod == "POST")
        {
            string table =
                context.Request.QueryString["table"] ?? string.Empty;

            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            List<SavedOrderItem> items =
                JsonSerializer.Deserialize<List<SavedOrderItem>>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SavedOrderItem>();

            Database.SaveOpenOrder(table, items);

            context.Response.StatusCode = 200;
        }
        else if (path == "/api/print-preparation" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body =
                await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<PreparationPrintRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                bool printSucceeded =
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                        () => PreparationTicketService.PrintPreparationTickets(
                            System.Windows.Application.Current.MainWindow,
                            request.TableName,
                            request.Items));

                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        printSucceeded.ToString());

                context.Response.StatusCode = 200;
                context.Response.ContentType =
                    "text/plain; charset=utf-8";
                context.Response.ContentLength64 =
                    bytes.Length;

                await context.Response.OutputStream.WriteAsync(bytes);
            }
        }
        else if (path == "/api/mark-order-sent" &&
                 context.Request.HttpMethod == "POST")
        {
            string table =
                context.Request.QueryString["table"] ?? string.Empty;

            Database.MarkOpenOrderSent(table);

            context.Response.StatusCode = 200;
        }

        else if (path == "/api/user-permissions")
        {
            string userIdText =
                context.Request.QueryString["userId"] ?? "0";

            int.TryParse(userIdText, out int userId);

            var permissions =
                Database.GetUserPermissions(userId);

            string json =
                JsonSerializer.Serialize(permissions);

            var bytes =
                Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "application/json; charset=utf-8";
            context.Response.ContentLength64 =
                bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }

        else if (path == "/api/cancel-order" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<OrderCancellationRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.CancelOpenOrder(
                    request.TableName,
                    request.Reason,
                    request.UserName);

                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/transfer-order" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<OrderTransferRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.TransferOpenOrder(
                    request.SourceTable,
                    request.TargetTable);

                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/transfer-products" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<ProductTransferRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.TransferProducts(
                    request.SourceTable,
                    request.TargetTable,
                    request.Items);

                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/delete-open-order" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<DeleteOpenOrderRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.DeleteOpenOrder(request.TableName);
                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/order-paid-total")
        {
            string table =
                context.Request.QueryString["table"] ?? string.Empty;

            decimal total =
                Database.GetOpenOrderPaidTotal(table);

            byte[] bytes = Encoding.UTF8.GetBytes(
                total.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));

            context.Response.StatusCode = 200;
            context.Response.ContentType =
                "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
        else if (path == "/api/add-payment" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<PaymentRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.AddOpenOrderPayment(
                    request.TableName,
                    request.PaymentType,
                    request.Amount,
                    request.Description);

                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/close-order" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<CloseOrderRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                Database.CloseOpenOrder(
                    request.TableName,
                    request.PaymentType,
                    request.TotalAmount);

                context.Response.StatusCode = 200;
            }
        }
        else if (path == "/api/product-payment" &&
                 context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding);

            string body = await reader.ReadToEndAsync();

            var request =
                JsonSerializer.Deserialize<ProductPaymentRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (request is null)
            {
                context.Response.StatusCode = 400;
            }
            else
            {
                bool orderClosed =
                    Database.ProcessProductPayment(
                        request.TableName,
                        request.Items,
                        request.PaymentType,
                        request.Amount,
                        request.Description);

                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        orderClosed.ToString());

                context.Response.StatusCode = 200;
                context.Response.ContentType =
                    "text/plain; charset=utf-8";
                context.Response.ContentLength64 =
                    bytes.Length;

                await context.Response.OutputStream.WriteAsync(bytes);
            }
        }
    }

    private sealed class ProductCancellationRequest
    {
        public string TableName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
    public void Stop()
    {
        _cts?.Cancel();

        if (_listener?.IsListening == true)
            _listener.Stop();

        _listener?.Close();
        _listener = null;

        _cts?.Dispose();
        _cts = null;
    }

    private sealed class OrderCancellationRequest
    {
        public string TableName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

    }

    private sealed class OrderTransferRequest
    {
        public string SourceTable { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
    }

    private sealed class ProductTransferRequest
    {
        public string SourceTable { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public List<SavedOrderItem> Items { get; set; } = new();
    }

    private sealed class DeleteOpenOrderRequest
    {
        public string TableName { get; set; } = string.Empty;
    }

    private sealed class PreparationPrintRequest
    {
        public string TableName { get; set; } = string.Empty;

        public List<PreparationTicketItem> Items { get; set; } = new();
    }

    private sealed class PaymentRequest
    {
        public string TableName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class CloseOrderRequest
    {
        public string TableName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    private sealed class ProductPaymentRequest
    {
        public string TableName { get; set; } = string.Empty;
        public List<SavedOrderItem> Items { get; set; } = new();
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
