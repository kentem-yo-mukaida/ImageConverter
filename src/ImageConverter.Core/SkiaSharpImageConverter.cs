using ImageMagick;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageConverter.Core;

public class SkiaSharpImageConverter
{
    public async Task<bool> ConvertAsync(
        string inputFilePath,
        string outputFilePath,
        MagickFormat format,
        bool convertByMagicNet,
        int quality = 75,
        int? width = null,
        int? height = null)
    {
        try
        {
            if (convertByMagicNet)
            {
                return await ConvertByMagicNetAsync(
                    inputFilePath, outputFilePath, format, quality, width, height);
            }
            else
            {
                return await ConvertBySkiaSharpNetAsync(
                    inputFilePath, outputFilePath, ConvertSKEncodedImageFormat(format), quality, width, height);
            }

            throw new NotSupportedException(
                $"The specified image format {format} is not supported.");
        }
        catch (FileNotFoundException)
        {
        }
        catch (Exception)
        {
        }
        return false;
    }

    private async Task<bool> ConvertByMagicNetAsync(
        string inputFilePath,
        string outputFilePath,
        MagickFormat format,
        int quality,
        int? width,
        int? height)
    {
        var imageBytes = await File.ReadAllBytesAsync(inputFilePath);
        using var image = new MagickImage(imageBytes);

        (int newWidth, int newHeight) = GetNewSize(
            (int)image.Width, (int)image.Height, width, height);
        image.Resize((uint)newWidth, (uint)newHeight);

        // AVIF形式で保存する
        // 品質設定 (0-100、高いほど高画質)
        image.Quality = (uint)quality;

        // 256x256ピクセルを基準とするタイルサイズ
        // この設定はAVIFのエンコーディングオプションの一部で、
        // 省略してもデフォルトで動作することが多いですが、品質やエンコード速度に影響します。
        // 64の倍数である必要があります。
        // 例えば、256x256ピクセルのタイルサイズを指定する場合は以下のようにします。
        // ただし、Magick.NETではタイルサイズの指定はオプションであり、
        // 必ずしも必要ではありません。タイルサイズを指定しない場合は、
        // デフォルトのタイルサイズが使用されます。
        // ただし、タイルサイズを指定することで、特に大きな画像を扱う場合に
        // エンコードの効率が向上することがあります。

        await image.WriteAsync(outputFilePath, new WriteDefines(
            new MagickDefine(format, "tile-size", "256x256"),
            new MagickDefine(format, "lossless", "false"),
            new MagickDefine(format, "method", "6"),
            new MagickDefine(format, "auto-filter", "true"),
            new MagickDefine(format, "emulate-jpeg-size", "true")
            ));

        return true;
    }

    private async Task<bool> ConvertBySkiaSharpNetAsync(
        string inputFilePath,
        string outputFilePath,
        SKEncodedImageFormat format,
        int quality,
        int? width,
        int? height)
    {
        using var bitmap = await LoadBitmapAsync(inputFilePath, width, height);
        if (bitmap is null)
            return false;

        using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
        return bitmap.Encode(outputStream, format, quality);
    }

    private async Task<SKBitmap?> LoadBitmapAsync(string filePath, int? width, int? height)
    {
        var inputBuffer = await File.ReadAllBytesAsync(filePath);
        var originalBitmap = SKBitmap.Decode(inputBuffer);

        if (originalBitmap is null)
            return null;

        if (!width.HasValue || !height.HasValue)
            return originalBitmap;

        (int newWidth, int newHeight) = GetNewSize(
            originalBitmap.Width, originalBitmap.Height, width, height);

        // 指定された幅と高さにリサイズ
        var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(newWidth, newHeight),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (resizedBitmap is null)
        {
            return originalBitmap;
        }

        originalBitmap.Dispose();
        return resizedBitmap;
    }

    private (int Width, int Height) GetNewSize(
        int imageWidth, int imageHeight,
        int? width, int? height)
    {
        if (!width.HasValue || !height.HasValue)
            return (imageWidth, imageHeight);

        int newWidth, newHeight;
        double dX = (double)width / imageWidth;
        double dY = (double)height / imageHeight;

        if ((dX - dY) > 1.0e-6f)
        {
            newHeight = height.Value;
            newWidth = RoundOff(dY * imageWidth);
        }
        else
        {
            newWidth = width.Value;
            newHeight = RoundOff(dX * imageHeight);
        }

        return (newWidth, newHeight);
    }

    private int RoundOff(double dd)
    {
        if (dd >= 0)
            return Convert.ToInt32(Math.Truncate(dd + 0.5000004));
        else
            return Convert.ToInt32(Math.Truncate(dd - 0.5000004));
    }

    private SKEncodedImageFormat ConvertSKEncodedImageFormat(
        MagickFormat format)
    {
        return format switch
        {
            MagickFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            MagickFormat.WebP => SKEncodedImageFormat.Webp,
            MagickFormat.Avif => SKEncodedImageFormat.Avif,
            MagickFormat.Jxl => SKEncodedImageFormat.Jpegxl,
            _ => throw new NotSupportedException(
                $"The specified image format {format} is not supported."),
        };
    }

    private class WriteDefines(params IDefine[] defines) : IWriteDefines
    {
        public MagickFormat Format => defines[0].Format;

        public IEnumerable<IDefine> Defines => defines;
    }
}