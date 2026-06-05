using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TiniTask.Services;

public static class ScreenshotService
{
    public static async Task<string> CaptureAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tinitask_{Guid.NewGuid():N}.png");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await CaptureWindws(path);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            await RunProcess("screencapture", $"-x \"{path}\"");
        else
            await CaptureLinux(path);
        
        return path;
    }
    
    private static async Task CaptureLinux(string path)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            await RunProcess("grim", $"\"{path}\"");
        else
            await RunProcess("scrot", $"\"{path}\"");
    }
    
    private static async Task CaptureWindws(string path)
    {
        var ps1 = Path.Combine(Path.GetTempPath(), $"ttscr_{Guid.NewGuid():N}.ps1");
        var safePath = path.Replace("\\", "\\\\");

        await File.WriteAllTextAsync(ps1, $"""
           Add-Type -AssemblyName System.Windows.Forms,System.Drawing
           $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
           $bmp = New-Object System.Drawing.Bitmap($b.Width, $b.Height)
           $g = [System.Drawing.Graphics]::FromImage($bmp)
           $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
           $bmp.Save("{safePath}")
           $g.Dispose(); $bmp.Dispose()
        """);

        try   { await RunProcess("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1}\""); }
        finally { File.Delete(ps1); }
    }
    
    private static async Task RunProcess(string program, string args)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = program,
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        
        p.Start();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
        {
            var err = await p.StandardError.ReadToEndAsync();
            throw new Exception($"[{program}] fallo: {err}");
        }
    }
}