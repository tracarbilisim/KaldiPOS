using System.Net;
using System.Text;
using System.Text.Json;
using KaldiPOS.Data;

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
        else
        {
            context.Response.StatusCode = 404;
        }

        context.Response.Close();
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
}