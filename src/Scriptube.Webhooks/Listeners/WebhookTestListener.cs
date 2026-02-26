using System.Net;
using System.Text;
using System.Threading.Channels;
using Scriptube.Webhooks.Models;

namespace Scriptube.Webhooks.Listeners;

public sealed class WebhookTestListener : IAsyncDisposable
{
    private readonly HttpListener? _listener;
    private readonly CancellationTokenSource? _cts;
    private Task? _acceptLoopTask;
    private readonly Channel<WebhookDelivery> _deliveries;

    private WebhookTestListener(string callbackUrl, HttpListener? listener, CancellationTokenSource? cts, Task? acceptLoopTask)
    {
        CallbackUrl = callbackUrl;
        _listener = listener;
        _cts = cts;
        _acceptLoopTask = acceptLoopTask;
        _deliveries = Channel.CreateUnbounded<WebhookDelivery>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public string CallbackUrl { get; }

    public static WebhookTestListener CreateExternal(string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            throw new ArgumentException("Callback URL is required.", nameof(callbackUrl));
        }

        return new WebhookTestListener(callbackUrl, listener: null, cts: null, acceptLoopTask: null);
    }

    public static WebhookTestListener StartLocal(int port = 0, string path = "/webhook-test/")
    {
        if (port is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 0 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        if (!normalizedPath.EndsWith('/'))
        {
            normalizedPath += "/";
        }

        var selectedPort = port == 0 ? FindAvailablePort() : port;
        var prefix = $"http://localhost:{selectedPort}{normalizedPath}";

        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var cts = new CancellationTokenSource();
        var instance = new WebhookTestListener(prefix, listener, cts, acceptLoopTask: null);
        instance._acceptLoopTask = Task.Run(() => AcceptLoopAsync(instance, listener, cts.Token), cts.Token);
        return instance;
    }

    public async Task<WebhookDelivery?> WaitForDeliveryAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await _deliveries.Reader.ReadAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public ValueTask RecordDeliveryAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        return _deliveries.Writer.WriteAsync(delivery, cancellationToken);
    }

    public ValueTask<bool> HasPendingDeliveryAsync(CancellationToken cancellationToken = default)
    {
        return _deliveries.Reader.WaitToReadAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();

        if (_listener is not null)
        {
            try
            {
                _listener.Stop();
            }
            catch
            {
                // Ignore shutdown race when listener already stopped.
            }
            _listener.Close();
        }

        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on normal disposal.
            }
            catch (HttpListenerException)
            {
                // Expected while stopping listener.
            }
        }

        _cts?.Dispose();
    }

    private static async Task AcceptLoopAsync(WebhookTestListener instance, HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            var delivery = await ReadDeliveryAsync(context.Request, cancellationToken);
            await instance._deliveries.Writer.WriteAsync(delivery, cancellationToken);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            var responseBuffer = Encoding.UTF8.GetBytes("ok");
            await context.Response.OutputStream.WriteAsync(responseBuffer, cancellationToken);
            context.Response.Close();
        }
    }

    private static async Task<WebhookDelivery> ReadDeliveryAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var headers = request.Headers.AllKeys
            .Where(static key => key is not null)
            .ToDictionary(
                static key => key!,
                key => request.Headers.GetValues(key!) ?? []);

        return new WebhookDelivery
        {
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            Method = request.HttpMethod,
            Path = request.Url?.AbsolutePath ?? "/",
            Body = body,
            Headers = headers
        };
    }

    private static int FindAvailablePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
