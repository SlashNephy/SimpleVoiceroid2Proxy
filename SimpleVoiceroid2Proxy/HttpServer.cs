using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace SimpleVoiceroid2Proxy
{
    internal class HttpServer : IDisposable
    {
        private const int Port = 4532;

        private readonly HttpListener listener = new HttpListener();

        public HttpServer()
        {
            listener.Prefixes.Add($"http://+:{Port}/");
            Program.Logger.Info($"http://localhost:{Port} でアクセスできます。GET /talk?text=... または POST /talk `{{\"text\": \"...\"}}` で発話させることができます。");

            OpenPort();
        }

        public void Dispose()
        {
            listener.Close();
        }

        private static void OpenPort()
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"http add urlacl url=http://+:{Port}/ user=Everyone",
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Minimized,
            });
            process?.WaitForExit();

            Program.Logger.Info($"ポート: {Port}/tcp を開放しました。ローカルネットワークから Voiceroid2Proxy にアクセスできます。");
        }

        public async Task ConsumeAsync()
        {
            listener.Start();

            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();

                    var request = new HttpRequest(context);
                    await request.HandleAsync();
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
