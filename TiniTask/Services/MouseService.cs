using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TiniTask.Services;

public static class MouseService
{
    public static async Task ClickAtAsync(int x, int y)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ClickWindows(x, y);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            await ClickMacOs(x, y);
        else
            await ClickLinux(x, y);
    }
    
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, int dx, int dy, uint data, IntPtr extra);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP   = 0x0004;

    private static void ClickWindows(int x, int y)
    {
        SetCursorPos(x, y);
        Task.Delay(30).Wait();
        mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero);
        Task.Delay(30).Wait();
        mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero);
    }
    
    private static async Task ClickMacOs(int x, int y) =>
        await RunProcess("cliclick", $"c:{x},{y}");
    
    private static async Task ClickLinux(int x, int y)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            // Usamos la sintaxis limpia de 'ydotool mousemove -a'
            await RunProcess("ydotool", $"mousemove -a {x} {y}");
            // Para el click izquierdo normal se usa 'click 1' (0x40000001 a veces se rompe según el layout de uinput)
            await RunProcess("ydotool", "click 1");
        }
        else
        {
            await RunProcess("xdotool", $"mousemove {x} {y} click 1");
        }
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
                UseShellExecute = false,
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