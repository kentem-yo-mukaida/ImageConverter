// See https://aka.ms/new-console-template for more information
using ImageConverter;
using ImageConverter.Core;
using ImageMagick;
using SkiaSharp;
using System.Threading.Tasks;

await RunAsync();

async Task RunAsync()
{
    if (args.Length > 0)
    {
        if (!File.Exists(args[0]))
        {
            Console.WriteLine($"指定されたファイル {args[0]} が存在しません。");
            return;
        }
        var file = args[0];

        string? outputFilePath = null;
        if (args.Length > 1)
        {
            try
            {
                if (!string.IsNullOrEmpty(args[1]))
                {
                    var dirName = Path.GetDirectoryName(args[1]);
                    Directory.CreateDirectory(dirName!);
                    outputFilePath = args[1];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ディレクトリの作成に失敗しました。{ex.Message}");
                return;
            }
        }

        var format = MagickFormat.Bmp;
        if (args.Length > 2 && !Enum.TryParse<MagickFormat>(args[2], true, out format))
        {
            Console.WriteLine($"無効な形式 {args[2]} です。");
            return;
        }

        var quality = 75;
        if (args.Length > 3 && !int.TryParse(args[3], out quality))
        {
            Console.WriteLine($"無効な品質 {args[3]} です。");
            return;
        }

        int? width = null;
        if (args.Length > 4 && int.TryParse(args[4], out var widthValue))
            width = widthValue;

        int? height = null;
        if (args.Length > 5 && int.TryParse(args[5], out var heightValue))
            height = heightValue;

        if (string.IsNullOrEmpty(outputFilePath))
        {
            var dirName = Path.Combine(Path.GetDirectoryName(file)!, "_output");
            Directory.CreateDirectory(dirName);

            var sizeText = "";
            if (width.HasValue && height.HasValue)
                sizeText = $"_{width}x{height}";

            outputFilePath = Path.Combine(
                dirName,
                Path.GetFileNameWithoutExtension(file) + $"{sizeText}.{format.ToString().ToLower()}");
        }

        if (File.Exists(outputFilePath))
        {
            Console.WriteLine($"ファイル {outputFilePath} は既に存在します。スキップします。");
            return;
        }

        SkiaSharpImageConverter imageConverter = new();
        await imageConverter.ConvertAsync(
            file,
            outputFilePath,
            format,
            false,
            quality,
            width,
            height);
    }
    else
    {
        Console.WriteLine("変換したいファイルが入ったフォルダーパスを入力してください。");
        string inputDirectoryPath;
        while (true)
        {
            inputDirectoryPath = Console.ReadLine() ?? string.Empty;
            if (Directory.Exists(inputDirectoryPath))
                break;

            Console.WriteLine("指定されたフォルダーパスが存在しません。もう一度入力してください。");
        }

        Console.WriteLine("変換するファイルの形式を選んでください。");
        MagickFormat format;
        while (true)
        {
            foreach (var value in Enum.GetValues<SKEncodedImageFormat>())
            {
                Console.WriteLine($"{(int)value}: {value}");
            }

            string input = Console.ReadLine() ?? string.Empty;
            if (Enum.TryParse<MagickFormat>(input, out var inputFormat))
            {
                format = inputFormat;
                break;
            }

            Console.WriteLine("無効な形式です。もう一度入力してください。");
        }

        var outputDirectoryPath = Path.Combine(inputDirectoryPath, $"ConvertedImages_{format}");
        Directory.CreateDirectory(outputDirectoryPath);

        var start = DateTime.Now;

        SkiaSharpImageConverter imageConverter = new();
        List<Task> tasks = [];
        foreach (var file in Directory.GetFileSystemEntries(inputDirectoryPath, "*.jpg"))
        {
            var outputFilePath = Path.Combine(
                outputDirectoryPath,
                Path.GetFileNameWithoutExtension(file) + $".{format.ToString().ToLower()}");

            if (File.Exists(outputFilePath))
            {
                Console.WriteLine($"ファイル {outputFilePath} は既に存在します。スキップします。");
                continue;
            }

            tasks.Add(imageConverter.ConvertAsync(
                file,
                outputFilePath,
                format,
                false,
                75));
        }

        while (tasks.Count > 0)
        {
            Task finishedTask = await Task.WhenAny(tasks);

            if (finishedTask.Exception is not null)
                throw finishedTask.Exception;

            tasks.Remove(finishedTask);
            if (finishedTask.IsFaulted)
            {
                Console.WriteLine($"変換中にエラーが発生しました: {finishedTask.Exception?.Message}");
            }
        }

        var end = DateTime.Now;
        Console.WriteLine($"変換が完了しました。{(end - start).TotalSeconds}秒");
    }
}