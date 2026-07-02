using API.Common.Cli;
using API.Common.Storage;

namespace API.Cli.Commands;

[CommandGroup("test")]
[Command("upload", "Test file upload to the configured storage provider. Usage: dotnet run -- cli test:upload <file-path>")]
public class TestUploadCommand(IStorageService storage) : CliCommand
{
    public override async Task ExecuteAsync(string[] args, CancellationToken ct)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Error: Please specify the file path to upload.");
            Console.Error.WriteLine("Usage: dotnet run -- cli test:upload <file-path>");
            return;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Error: File '{filePath}' does not exist.");
            return;
        }

        Console.WriteLine($"Starting upload of '{filePath}'...");
        try
        {
            using var stream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            var url = await storage.UploadAsync(stream, fileName, contentType, ct);
            Console.WriteLine("Upload successful!");
            Console.WriteLine($"URL: {url}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Upload failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
