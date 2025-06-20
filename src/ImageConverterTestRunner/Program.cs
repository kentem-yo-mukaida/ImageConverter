// See https://aka.ms/new-console-template for more information
using ImageConverter.Core;
using ImageMagick;
using SkiaSharp;

var convertByMagicNet = true;

var sourceFilePath = @"";
Console.WriteLine($"Convert From {Path.GetFileName(sourceFilePath)}" +
    $"Size: {new FileInfo(sourceFilePath).Length / 1024.0:F2} KB");

var quality = 75;

Console.WriteLine();

var inputFilePath = sourceFilePath;

MagickFormat[] formats = [
    MagickFormat.Jpeg,
    MagickFormat.WebP,
    MagickFormat.Avif,
    MagickFormat.Jxl,
    MagickFormat.Heic
    ];

(int Width, int Height)[] sizes = [
    (1280, 720),
    (640, 480),
    (320, 240),
    (160, 120),
];

var outputDirectory = Path.Combine(
    Path.GetDirectoryName(inputFilePath)!,
    $"_output_{(convertByMagicNet ? "magic" : "skia")}_{DateTime.Now:yyyyMMdd_HHmmss}");
Directory.CreateDirectory(outputDirectory);

foreach (var size in sizes)
{
    Console.WriteLine($"Converting to {size.Width}x{size.Height}...");
    foreach (var format in formats)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{size.Width}x{size.Height}.{format.ToString().ToLower()}";
        var outputFilePath = Path.Combine(
            outputDirectory,
            fileName);
        var converter = new SkiaSharpImageConverter();

        var startTime = DateTime.Now;
        var result = await converter.ConvertAsync(
            inputFilePath, outputFilePath, format, convertByMagicNet, quality, size.Width, size.Height);
        var endTime = DateTime.Now;

        var elapsed = endTime - startTime;
        if (result)
        {
            Console.WriteLine(
                $"{format.ToString()}\t" +
                $"{elapsed.TotalSeconds:F2} seconds\t" +
                $"Size: {new FileInfo(outputFilePath).Length / 1024.0:F2} KB");
        }
        else
        {
            Console.WriteLine(
                $"{format.ToString()}\t" +
                $"{elapsed.TotalSeconds:F2} seconds\t" +
                $"Failed!");
        }
    }

    Console.WriteLine();
}

Console.WriteLine("完了！");
Console.ReadKey();

