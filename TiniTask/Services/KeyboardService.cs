using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using InputSimulatorStandard;

namespace TiniTask.Services;

public static class KeyboardService
{
    public static async Task TypeAsync(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await TypeWindows(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            await TypeMacOs(text);
        else
            await TypeLinux(text);
    }

    private static Task TypeWindows(string text)
    {
        var simulator = new InputSimulator();
        simulator.Keyboard.TextEntry(text);
        
        Task.Delay(10).Wait();
        simulator.Keyboard.KeyPress(InputSimulatorStandard.Native.VirtualKeyCode.RETURN);
        return Task.CompletedTask;
    }

    private static async Task TypeMacOs(string text)
    {
        var escaped = text.Replace("\"", "\\\"");
        await RunProcess("osascript", $"-e 'tell application \"System Events\" to keystroke \"{escaped}\"'" +
            $"-e 'tell application \"System Events\" to key code 36'"
        );
    }

    private static async Task TypeLinux(string text)
    {
        if (IsWayland())
            await RunProcess("ydotool", $"type \"{EscapeArg(text)}\" && ydotool key 28:1 28:0");
        else 
            await RunProcess("xdotool", $"type --delay 50 \"{EscapeArg(text)}\" key Return");
    }

    private static bool IsWayland()
    {
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        return !string.IsNullOrEmpty(waylandDisplay);
    }
    
    private static string EscapeArg(string text) =>
        text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static async Task RunProcess(string program, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = program,
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };
        
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"[{program}] fallo: {error}");
        }
    }
}