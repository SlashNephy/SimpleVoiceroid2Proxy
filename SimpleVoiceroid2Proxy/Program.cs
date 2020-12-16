using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SimpleVoiceroid2Proxy
{
    public static class Program
    {
        static Program()
        {
            KillDuplicatedProcesses();
        }

        public static readonly ILogger Logger = new LoggerImpl();
        private static readonly HttpServer Server = new();
        public static readonly VoiceroidEngine VoiceroidEngine = new();

        public static void Main()
        {
            Task.Run(async () =>
            {
                await VoiceroidEngine.TalkAsync("準備完了！");
                await Server.ConsumeAsync();
            }).Wait();
        }

        private static void KillDuplicatedProcesses()
        {
            var currentProcess = Process.GetCurrentProcess();
            var imageName = Assembly.GetExecutingAssembly()
                .Location
                .Split(Path.DirectorySeparatorChar)
                .Last()
                .Replace(".exe", "");

            foreach (var process in Process.GetProcessesByName(imageName).Where(x => x.Id != currentProcess.Id))
            {
                try
                {
                    process.Kill();
                    Logger.Info($"{imageName}.exe (PID: {process.Id}) has been killed.");
                }
                catch
                {
                    Logger.Warn($"Failed to kill {imageName}.exe (PID: {process.Id}).");
                }
            }
        }

        private class LoggerImpl : ILogger
        {
            public void Info(string message)
            {
                Write("Info", message);
            }

            public void Warn(string message)
            {
                Write("Warn", message);
            }

            public void Error(Exception exception)
            {
                Write("Error", exception.ToString());
            }

            private static void Write(string level, string message)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
            }
        }
    }

    public interface ILogger
    {
        public void Info(string message);
        public void Warn(string message);
        public void Error(Exception exception);
    }
}
