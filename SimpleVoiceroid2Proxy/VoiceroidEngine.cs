using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Controls;
using Codeer.Friendly.Windows;
using Codeer.Friendly.Windows.Grasp;
using Microsoft.Win32;
using RM.Friendly.WPFStandardControls;

namespace SimpleVoiceroid2Proxy
{
    public class VoiceroidEngine : IDisposable
    {
        private readonly Process process;
        private readonly WindowsAppFriend app;

        private readonly WPFTextBox talkTextBox;
        private readonly WPFButtonBase playButton;
        private readonly WPFButtonBase stopButton;
        private readonly WPFButtonBase moveButton;

        private readonly Channel<string> queue = Channel.CreateUnbounded<string>();
        private static readonly TimeSpan TalkCooldown = TimeSpan.FromMilliseconds(200);
        private DateTime lastPlay;
        private volatile bool interrupt = true;
        private volatile bool paused;

        public VoiceroidEngine()
        {
            Program.Logger.Info("VOICEROID init started...");

            process = GetOrCreateVoiceroidProcess();
            app = new WindowsAppFriend(process);

            try
            {
                // Timeout = 60 sec
                for (var i = 0; i < 1200; i++)
                {
                    var window = app.FromZTop();
                    WinApi.ShowWindow(window.Handle, WinApi.SwMinimize);

                    var tree = window.GetFromTypeFullName("AI.Talk.Editor.TextEditView")
                        .FirstOrDefault()
                        ?.LogicalTree();
                    if (tree == null)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    var text = tree.ByType<TextBox>().Single();
                    var play = tree.ByBinding("PlayCommand").Single();
                    var stop = tree.ByBinding("StopCommand").Single();
                    // var move = tree.ByBinding("MoveToBeginningCommand").Single();

                    talkTextBox = new WPFTextBox(text);
                    playButton = new WPFButtonBase(play);
                    stopButton = new WPFButtonBase(stop);
                    moveButton = new WPFButtonBase(tree[15]);
                    // moveButton = new WPFButtonBase(move);

                    Program.Logger.Info("VOICEROID ready!");
                    Task.Run(ConsumeAsync);

                    return;
                }
            }
            catch (Exception exception)
            {
                Program.Logger.Error(exception);
                throw new ApplicationException("VOICEROID init failed.");
            }

            throw new TimeoutException("VOICEROID init timed out.");
        }

        public void Dispose()
        {
            queue.Writer.TryComplete();
            app.Dispose();
            process.Kill();
            process.Dispose();
        }

        private static readonly Regex BracketRegex = new Regex(@"<(?<command>.+?)>", RegexOptions.Compiled);

        public async Task TalkAsync(string text)
        {
            text = BracketRegex.Replace(text, match =>
            {
                switch (match.Groups["command"].Value)
                {
                    case "clear":
                        while (queue.Reader.TryRead(out _))
                        {
                        }

                        Program.Logger.Info("**********");
                        break;
                    case "pause":
                        paused = true;

                        Program.Logger.Info("********** => Paused.");
                        break;
                    case "resume":
                        paused = false;

                        Program.Logger.Info("********** => Resumed.");
                        break;
                    case "interrupt_enable":
                        interrupt = true;

                        Program.Logger.Info("********** => Interrupt enabled.");
                        break;
                    case "interrupt_disable":
                        interrupt = false;

                        Program.Logger.Info("********** => Interrupt disabled.");
                        break;
                    default:
                        return match.Value;
                }

                return string.Empty;
            });

            await queue.Writer.WriteAsync(text);
        }

        private async Task ConsumeAsync()
        {
            while (await queue.Reader.WaitToReadAsync())
            {
                while (queue.Reader.TryRead(out var text))
                {
                    await SpeakAsync(text!);
                }
            }
        }

        private async Task SpeakAsync(string text)
        {
            // VOICEROID2 が発話中の時は「先頭」ボタンが無効になるので、それを利用して発話中かどうかを判定します
            while (!interrupt && !moveButton.IsEnabled)
            {
                await Task.Delay(50); // spin wait
            }

            while (paused)
            {
                await Task.Delay(500);
            }

            var cooldown = TalkCooldown - (DateTime.Now - lastPlay);
            if (cooldown.TotalMilliseconds > 0)
            {
                await Task.Delay(cooldown);
            }

            stopButton.EmulateClick();
            talkTextBox.EmulateChangeText(text);
            moveButton.EmulateClick();
            playButton.EmulateClick();

            lastPlay = DateTime.Now;
            Program.Logger.Info($"=> {text}");
        }

        private static Process GetOrCreateVoiceroidProcess()
        {
            return (Process.GetProcessesByName("VoiceroidEditor").FirstOrDefault() ?? Process.Start(new ProcessStartInfo
            {
                FileName = FindVoiceroidPath(),
                WindowStyle = ProcessWindowStyle.Minimized
            }))!;
        }

        private static string FindVoiceroidPath()
        {
            return Registry.ClassesRoot
                .OpenSubKey(@"Installer\Assemblies")
                ?.GetSubKeyNames()
                .Where(x => x.EndsWith("VoiceroidEditor.exe"))
                .Select(x => x.Replace('|', '\\'))
                .FirstOrDefault() ?? throw new ApplicationException("VOICEROID not found.");
        }

        private static class WinApi
        {
            public const int SwMinimize = 6;

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
    }
}
