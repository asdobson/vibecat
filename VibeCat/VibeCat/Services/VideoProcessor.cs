using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FFMpegCore;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace VibeCat.Services;

public class VideoProcessor
{
    
    public async Task<List<BitmapSource>> ExtractFramesAsync(string videoPath, int maxFrames = 300)
    {
        var frames = new List<BitmapSource>();
        
        var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
        var duration = mediaInfo.Duration;
        var frameRate = mediaInfo.VideoStreams.First().FrameRate;
        
        // Calculate frame extraction interval
        var totalFrames = (int)(duration.TotalSeconds * frameRate);
        var frameInterval = Math.Max(1, totalFrames / maxFrames);
        
        // Use FFMpeg to extract frames
        var tempDir = Path.Combine(Path.GetTempPath(), $"VibeCat_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Extract frames as images
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(Path.Combine(tempDir, "frame_%04d.png"), false, options => options
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(400, 400))
                    .WithFramerate(frameRate / frameInterval))
                .ProcessAsynchronously();
            
            // Load and process frames
            var frameFiles = Directory.GetFiles(tempDir, "*.png").OrderBy(f => f);
            foreach (var frameFile in frameFiles)
            {
                using var bitmap = new Bitmap(frameFile);
                var processedBitmap = ApplyChromaKey(bitmap);
                frames.Add(ConvertToBitmapSource(processedBitmap));
                processedBitmap.Dispose();
            }
        }
        finally
        {
            // Cleanup temp files
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        
        return frames;
    }
    
    private Bitmap ApplyChromaKey(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        
        using (var g = Graphics.FromImage(result))
        {
            g.Clear(System.Drawing.Color.Transparent);
        }
        
        var sourceData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
            
        var resultData = result.LockBits(
            new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        
        unsafe
        {
            byte* sourcePtr = (byte*)sourceData.Scan0;
            byte* resultPtr = (byte*)resultData.Scan0;
            
            int bytes = Math.Abs(sourceData.Stride) * source.Height;
            
            for (int i = 0; i < bytes; i += 4)
            {
                byte b = sourcePtr[i];
                byte g = sourcePtr[i + 1];
                byte r = sourcePtr[i + 2];
                byte a = sourcePtr[i + 3];
                
                // Convert to HSL for better green detection
                RgbToHsl(r, g, b, out float h, out float s, out float l);
                
                // Check if pixel is in green screen range
                bool isGreen = Math.Abs(h - 120) < 40 && s > 0.3f && l > 0.2f && l < 0.8f;
                
                if (isGreen)
                {
                    // Make transparent
                    resultPtr[i] = 0;
                    resultPtr[i + 1] = 0;
                    resultPtr[i + 2] = 0;
                    resultPtr[i + 3] = 0;
                }
                else
                {
                    // Copy pixel
                    resultPtr[i] = b;
                    resultPtr[i + 1] = g;
                    resultPtr[i + 2] = r;
                    resultPtr[i + 3] = a;
                }
            }
        }
        
        source.UnlockBits(sourceData);
        result.UnlockBits(resultData);
        
        return result;
    }
    
    private void RgbToHsl(byte r, byte g, byte b, out float h, out float s, out float l)
    {
        float rf = r / 255f;
        float gf = g / 255f;
        float bf = b / 255f;
        
        float max = Math.Max(rf, Math.Max(gf, bf));
        float min = Math.Min(rf, Math.Min(gf, bf));
        
        l = (max + min) / 2f;
        
        if (max == min)
        {
            h = s = 0;
        }
        else
        {
            float diff = max - min;
            s = l > 0.5f ? diff / (2f - max - min) : diff / (max + min);
            
            if (max == rf)
                h = ((gf - bf) / diff + (gf < bf ? 6f : 0f)) * 60f;
            else if (max == gf)
                h = ((bf - rf) / diff + 2f) * 60f;
            else
                h = ((rf - gf) / diff + 4f) * 60f;
        }
    }
    
    private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            bitmap.PixelFormat);
        
        var bitmapSource = BitmapSource.Create(
            bitmapData.Width,
            bitmapData.Height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            bitmapData.Scan0,
            bitmapData.Stride * bitmapData.Height,
            bitmapData.Stride);
        
        bitmap.UnlockBits(bitmapData);
        
        // Freeze for performance
        bitmapSource.Freeze();
        
        return bitmapSource;
    }
}