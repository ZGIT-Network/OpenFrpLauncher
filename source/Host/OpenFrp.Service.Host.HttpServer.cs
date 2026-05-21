// OpenFrp.Service, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// OpenFrp.Service.Host.HttpServerRequest
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace OpenFrp.Launcher.HostLite;

public class HttpServerRequest
{
    private readonly HttpListenerContext context;

    public Uri? Url => context.Request.Url;

    public bool HasPostBody => context.Request.HasEntityBody;

    public string HttpMethod => context.Request.HttpMethod;

    public int StatusCode
    {
        get
        {
            return context.Response.StatusCode;
        }
        set
        {
            context.Response.StatusCode = value;
        }
    }

    public bool IsWebSocketRequest => context.Request.IsWebSocketRequest;

    public HttpServerRequest(HttpListenerContext context)
    {
        this.context = context;
    }

    public HttpListenerContext GetContext()
    {
        return context;
    }

    public void Abort()
    {
        context.Response.StatusCode = 503;
        context.Response.Abort();
    }

    public NameValueCollection GetQuerys()
    {
        if (context.Request.Url == null)
        {
            return new NameValueCollection();
        }
        return System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
    }

    public async Task RespondWithStreamAsync(Stream originalSource, string contentType = "application/json", CancellationToken cancellationToken = default(CancellationToken))
    {
        context.Response.ContentType = contentType;
        originalSource.Seek(0L, SeekOrigin.Begin);
        using Stream output = context.Response.OutputStream;
        await originalSource.CopyToAsync(output, 1024, cancellationToken);
        await output.FlushAsync(cancellationToken);
        context.Response.Close();
    }

    public async Task RespondWithJsonBodyAsync<T>(T jsonBody, CancellationToken cancellationToken = default(CancellationToken))
    {
        context.Response.ContentType = "application/json";
        using MemoryStream input = new MemoryStream();
        using Stream output = context.Response.OutputStream;
        await JsonSerializer.SerializeAsync((Stream)input, jsonBody, (JsonSerializerOptions?)null, cancellationToken);
        context.Response.ContentLength64 = input.Length;
        if (input.Length == 0)
        {
            context.Response.StatusCode = 504;
            output.Close();
            return;
        }
        input.Seek(0L, SeekOrigin.Begin);
        await input.CopyToAsync(output, 1024, cancellationToken);
        await output.FlushAsync(cancellationToken);
        context.Response.Close();
    }

    public async Task RespondWithStringAsync(string text, string contentType = "text/plain; charset=utf-8")
    {
        using Stream output = context.Response.OutputStream;
        context.Response.ContentType = contentType;
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        await output.WriteAsync(buffer, 0, buffer.Length);
        await output.FlushAsync();
        context.Response.Close();
    }

    public void RedirectTo(string url)
    {
        context.Response.Redirect(url);
        context.Response.Close();
    }
}


public abstract class HttpServer : IDisposable
{
    private ushort _port;

    private TaskCompletionSource<bool> _taskAwaiter;

    public HttpListener? _listener;

    public ushort Port => _port;

    public HttpServer()
    {
        _taskAwaiter = new TaskCompletionSource<bool>();
    }

    public HttpServer(ushort tryUsePort)
        : this()
    {
        _port = tryUsePort;
    }

    public void StopListen()
    {
        Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!_taskAwaiter.Task.IsCompleted)
        {
            var listener = _listener;
            if (listener == null || !listener.IsListening)
            {
                StartListen(cancellationToken);
                return _taskAwaiter.Task;
            }
        }
        return Task.CompletedTask;
    }

    public void StartListen(CancellationToken token = default(CancellationToken))
    {
        ushort port;
        var listener = CreateHttpListener(_port, out port);
        if (listener == null || port == 0)
        {
            Exception ex = new UnauthorizedAccessException("Failed to create http listener");
            _taskAwaiter.TrySetException(ex);
            HandleException(ex);
        }
        else
        {
            _port = port;
            _taskAwaiter.SetResult(result: true);
            token.Register(StopListen);
            _ = HostThreadCreate(_listener = listener, token);
        }
    }

    private async Task HostThreadCreate(HttpListener listener, CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            while (listener.IsListening && !cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                try
                {
                    _ = AcceptContextAsync(new HttpServerRequest(context));
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }
        }
        finally
        {

        }
    }

    public static string GetContentType(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        string contentType = "application/" + extension;
        switch (extension)
        {
            case ".html":
            case ".htm":
                contentType = "text/html";
                break;
            case ".js":
                contentType = "application/javascript";
                break;
            case ".css":
                contentType = "text/css";
                break;
            case ".png":
                contentType = "image/png";
                break;
            case ".svg":
                contentType = "image/svg+xml";
                break;
        }
        return contentType;
    }

    private static HttpListener? CreateHttpListener(ushort tryUse, out ushort port)
    {
        HashSet<ushort> usedPort = new HashSet<ushort>();
        Random rand = new Random();
        for (int i = 0; i < 5; i++)
        {
            if (tryUse > 0 && !usedPort.Contains(tryUse))
            {
                port = tryUse;
            }
            else
            {
                port = (ushort)rand.Next(49152, 65535);
            }
            if (usedPort.Contains(port))
            {
                i--;
                continue;
            }
            HttpListener m_Listener = new HttpListener();
            m_Listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                m_Listener.Start();
                return m_Listener;
            }
            catch
            {
                if (i == 5)
                {
                    throw;
                }
            }
        }
        port = 0;
        return null;
    }

    public void Dispose()
    {
        _listener?.Abort();
        _listener?.Close();
        _listener = null;
    }

    public abstract void HandleException(Exception ex);

    public abstract Task AcceptContextAsync(HttpServerRequest request);
}
