using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TiniTask.Services;

public static class ImageDetectionService
{
    public static async Task<(int cx, int cy)?> FindOnScreenAsync(IEnumerable<string> templatePaths, float threshold = 0.80f)
    {
        var screenPath = await ScreenshotService.CaptureAsync();
        try
        {
            return await Task.Run(() => FindBest(screenPath, templatePaths, threshold));
        }
        finally
        {
            if (File.Exists(screenPath))
                File.Delete(screenPath);
        }
    }
    
    private static (int cx, int cy)? FindBest(string screenPath, IEnumerable<string> templatePaths, float threshold)
    {
        const int scale = 1;

        // La pantalla se carga y escala una sola vez para todos los templates
        using var screen  = Image.Load<Rgb24>(screenPath);
        
        // using var screenS = screen.Clone(ctx => ctx.Resize(screen.Width / scale, screen.Height / scale));

        var sp  = GetPixels(screen);
        int ssw = screen.Width;
        int ssh = screen.Height;

        float bestScore = float.MinValue;
        int bestCx = -1;
        int bestCy = -1;

        foreach (var path in templatePaths)
        {
            if (!File.Exists(path)) 
                continue;

            using var tmpl  = Image.Load<Rgb24>(path);
            
            // int tsw = Math.Max(4, tmpl.Width  / scale);
            // int tsh = Math.Max(4, tmpl.Height / scale);
            // using var tmplS = tmpl.Clone(ctx => ctx.Resize(tsw, tsh));

            var tp = GetPixels(tmpl);
            var (bx, by, score) = BestMatch(sp, tp, ssw, ssh, tmpl.Width, tmpl.Height);

            if (score > bestScore)
            {
                bestScore = score;
                bestCx = bx + tmpl.Width / 2;
                bestCy = by * scale + tmpl.Height / 2;
            }
        }

        return bestScore >= threshold ? (bestCx, bestCy) : null;
    }
    
    private static (int x, int y, float score) BestMatch(Rgb24[] screen, Rgb24[] tmpl, int sw, int sh, int tw, int th)
    {
        long maxSad = (long)tw * th * 3 * 255;
        float best = float.MinValue;
        int bestX = 0, bestY = 0;

        for (int y = 0; y <= sh - th; y++)
        {
            for (int x = 0; x <= sw - tw; x++)
            {
                long sad = ComputeSAD(screen, tmpl, x, y, sw, tw, th);
                float score = 1f - (float)sad / maxSad;
                
                if (score > best)
                {
                    best = score;
                    bestX = x;
                    bestY = y;
                }
            }
        }
        
        return (bestX, bestY, best);
    }
    
    private static long ComputeSAD(Rgb24[] screen, Rgb24[] tmpl, int ox, int oy, int sw, int tw, int th)
    {
        long sad = 0;

        for (int y = 0; y < th; y++)
        {
            int sRow = (oy + y) * sw + ox;
            int tRow = y * tw;

            for (int x = 0; x < tw; x++)
            {
                var sc= screen[sRow + x];
                var tc = tmpl[tRow + x];
                sad += Math.Abs(sc.R - tc.R);
                sad += Math.Abs(sc.G - tc.G);
                sad += Math.Abs(sc.B - tc.B);
            }
        }
        
        return sad;
    }
    
    private static Rgb24[] GetPixels(Image<Rgb24> img)
    {
        var pixels = new Rgb24[img.Width * img.Height];
        img.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < img.Height; y++)
                acc.GetRowSpan(y).CopyTo(pixels.AsSpan(y * img.Width, img.Width));
        });
        return pixels;
    }
}