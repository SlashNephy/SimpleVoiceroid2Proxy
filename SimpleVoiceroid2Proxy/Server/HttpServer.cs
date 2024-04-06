using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleVoiceroid2Proxy.Server;

public sealed class HttpServer : IDisposable
{
    private const int Port = 4532;

    private readonly HttpListener listener = new();
    private readonly Controller controller = new();

    public HttpServer()
    {
        listener.Prefixes.Add($"http://+:{Port}/");
    }

    public async Task ListenAsync()
    {
        listener.Start();

        while (listener.IsListening)
        {
            try
            {
                var context = await listener.GetContextAsync();

                var request = new HttpContext(context);
                await controller.HandleAsync(request);
            }
            catch
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        listener.Close();
    }
}
