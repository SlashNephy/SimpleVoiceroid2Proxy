using System.Threading.Tasks;
using SimpleVoiceroid2Proxy.Server;

namespace SimpleVoiceroid2Proxy;

public static class Program
{
    private static readonly HttpServer server = new();
    public static readonly VoiceroidEngine VoiceroidEngine = new();

    static Program()
    {
        Utils.KillDuplicateProcesses();
    }

    public static void Main()
    {
        Task.WaitAll(
            server.ListenAsync(),
            VoiceroidEngine.TalkAsync("準備完了！")
        );
    }
}
