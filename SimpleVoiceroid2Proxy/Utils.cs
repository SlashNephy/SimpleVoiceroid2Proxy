using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleVoiceroid2Proxy;

public static class Utils
{
    public static void KillDuplicateProcesses()
    {
        var currentProcess = Process.GetCurrentProcess();
        var imageName = Assembly.GetExecutingAssembly()
            .Location
            .Split(Path.DirectorySeparatorChar)
            .Last()
            .Replace(".exe", "");

        foreach (var process in Process.GetProcessesByName(imageName))
        {
            if (process.Id != currentProcess.Id)
            {
                process.Kill();
                process.WaitForExit();
            }
        }
    }
}
