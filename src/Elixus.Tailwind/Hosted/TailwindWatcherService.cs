using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

using Elixus.Tailwind.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elixus.Tailwind.Hosted;

/// <summary>
/// A runtime service to run the Tailwind CLI watcher for each defined input file.
/// </summary>
public sealed class TailwindWatcherService(
    ILogger<TailwindWatcherService> logger,
    IOptions<TailwindWatchOptions> options
) : BackgroundService
{
    private readonly List<(Process, CancellationTokenRegistration)> _watchers = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var rootFolder = options.Value.RootDirectory ?? Directory.GetCurrentDirectory();

        if (options.Value.AutoDetect)
            await this.AutoDetectWatchers(rootFolder, cancellationToken);

        var binary = DetectTailwindBinary(rootFolder);

        foreach (var file in options.Value.Inputs)
        {
            var result = StartWatchProcess(rootFolder, binary, file, cancellationToken);

            if (result is not null)
                _watchers.Add(result.Value);
        }

        // Wait indefinitely to keep watchers running.
        await Task.Delay(-1, cancellationToken);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var (process, registration) in _watchers)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                process.Dispose();
                registration.Dispose();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Error while disposing watcher");
            }
        }

        _watchers.Clear();
        base.Dispose();
    }

    private async Task AutoDetectWatchers(string rootFolder, CancellationToken cancellationToken)
    {
        var projectFile = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile))
        {
            logger.LogWarning("No project file found in root folder {RootFolder}", rootFolder);

            return;
        }

        logger.LogDebug("Using {ProjectFile} to infer watch files", projectFile);
        await using var stream = File.OpenRead(projectFile);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        foreach (var element in document.Descendants(XName.Get("TailwindInput")))
        {
            options.Value.Inputs.Add(new TailwindInput
            {
                Input = element.Attribute("Include")?.Value ?? throw new InvalidOperationException("TailwindInput must have an Include attribute"),
                Output = element.Attribute("Output")?.Value
            });
        }
    }

    /// <summary>
    /// Attempt to automatically detect the binary of the TailwindCLI, downloaded by Elixus.Tailwind.MSBuild
    /// </summary>
    private string DetectTailwindBinary(string rootFolder)
    {
        var tailwindDir = Path.Combine(rootFolder, ".tailwind");
        if (!Directory.Exists(tailwindDir))
        {
            logger.LogCritical("Failed to detect tailwind directory {RootFolder}", rootFolder);

            throw new DirectoryNotFoundException($"Failed to detect tailwind directory {rootFolder}");
        }

        var pattern = "tailwindcss-windows-*";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            pattern = "tailwindcss-macos-*";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            pattern = "tailwindcss-linux-*";

        var binary = Directory.GetFiles(tailwindDir, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(binary))
            throw new FileNotFoundException($"Failed to detect Tailwind binary in {tailwindDir}");

        logger.LogDebug("Using {Binary} to infer watch files", binary);
        return binary;
    }

    private (Process, CancellationTokenRegistration)? StartWatchProcess(string rootFolder, string binary,
        TailwindInput input,
        CancellationToken cancellationToken)
    {
        var output = input.Output ?? Path.Combine(rootFolder, "wwwroot", Path.GetFileName(input.Input));
        var startInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = $"--input \"{input.Input}\" --output \"{output}\" --watch",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };

        // Log output from the Tailwind CLI
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.LogInformation("[Tailwind][{InputFile}] {Message}", input.Input, args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.LogError("[Tailwind][{InputFile}] {Message}", input.Input, args.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var registration = cancellationToken.Register(() => process.Kill(entireProcessTree: true));

            logger.LogInformation("Started watching: {InputFile} -> {OutputFile}", input.Input, output);

            return (process, registration);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to start Tailwind watcher for {InputFile}", input.Input);

            return null;
        }
    }
}