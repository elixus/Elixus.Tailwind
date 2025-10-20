using System.Diagnostics;

using Microsoft.Build.Framework;

namespace Elixus.Tailwind.MSBuild;

/// <summary>
/// MSBuild task that processes a specified file.
/// </summary>
public class ProcessTailwindFileTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The file to process.
    /// </summary>
    [Required]
    public string? InputFile { get; set; }

    /// <summary>
    /// The output directory where processed files will be written.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// The project directory (used for relative path logging).
    /// </summary>
    public string? ProjectDirectory { get; set; }

    /// <summary>
    /// The path to the Tailwind CLI executable.
    /// </summary>
    [Required]
    public string? TailwindCliPath { get; set; }

    /// <summary>
    /// If true, processes the file in-place (overwrites the input file).
    /// </summary>
    public bool InPlace { get; set; }

    /// <summary>
    /// Executes the task to process the input file.
    /// </summary>
    public override bool Execute()
    {
        if (string.IsNullOrEmpty(InputFile))
        {
            Log.LogError("InputFile parameter is required");
            return false;
        }

        if (!File.Exists(InputFile))
        {
            Log.LogError($"Input file does not exist: {InputFile}");
            return false;
        }

        if (string.IsNullOrEmpty(TailwindCliPath) || !File.Exists(TailwindCliPath))
        {
            Log.LogError($"Tailwind CLI not found at: {TailwindCliPath}");
            return false;
        }

        Log.LogMessage(MessageImportance.Normal, $"Processing file: {InputFile}");
        Log.LogMessage(MessageImportance.Normal, $"Output directory: {OutputDirectory}");
        Log.LogMessage(MessageImportance.Normal, $"Tailwind CLI: {TailwindCliPath}");

        try
        {
            // Ensure output directory exists
            if (!string.IsNullOrEmpty(OutputDirectory) && !Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
                Log.LogMessage(MessageImportance.Normal, $"Created output directory: {OutputDirectory}");
            }

            // Generate output filename: keep the same name but in output directory
            var fileName = Path.GetFileName(InputFile);

            // Determine output path
            string outputPath;
            string? tempOutputPath = null;

            if (InPlace)
            {
                // For in-place processing, use a temporary file first
                tempOutputPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}.{Guid.NewGuid()}{Path.GetExtension(fileName)}");
                outputPath = tempOutputPath;
            }
            else
            {
                // Use OutputDirectory if specified, otherwise use the same directory as input file
                var outputDir = string.IsNullOrEmpty(OutputDirectory)
                    ? (Path.GetDirectoryName(InputFile) ?? string.Empty)
                    : OutputDirectory;

                outputPath = Path.Combine(outputDir, fileName);
            }

            // Get absolute path to input file
            var absoluteInputPath = Path.IsPathRooted(InputFile)
                ? InputFile
                : Path.Combine(ProjectDirectory ?? string.Empty, InputFile);

            // Invoke Tailwind CLI to process the file
            var startInfo = new ProcessStartInfo
            {
                FileName = TailwindCliPath,
                Arguments = $"--input \"{absoluteInputPath}\" --output \"{outputPath}\" --minify",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory ?? Directory.GetCurrentDirectory()
            };

            Log.LogMessage(MessageImportance.Normal,
                $"Running: {TailwindCliPath} --input \"{absoluteInputPath}\" --output \"{outputPath}\" --minify");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Log.LogError("Failed to start Tailwind CLI process");
                return false;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Log.LogMessage(MessageImportance.Normal, stdout);
            }

            if (process.ExitCode != 0)
            {
                Log.LogError($"Tailwind CLI exited with code {process.ExitCode}");
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Log.LogError(stderr);
                }

                return false;
            }

            if (!File.Exists(outputPath))
            {
                Log.LogError($"Expected output file was not created: {outputPath}");
                return false;
            }

            // If processing in-place, copy temp file back to original location
            if (InPlace && tempOutputPath != null)
            {
                try
                {
                    File.Copy(tempOutputPath, absoluteInputPath, overwrite: true);
                    File.Delete(tempOutputPath);
                    Log.LogMessage(MessageImportance.High, $"Updated file in-place: {absoluteInputPath}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to copy processed file back to original location: {ex.Message}");
                    // Clean up temp file
                    if (File.Exists(tempOutputPath))
                    {
                        try { File.Delete(tempOutputPath); } catch { }
                    }
                    return false;
                }
            }
            else
            {
                Log.LogMessage(MessageImportance.High, $"Generated output file: {outputPath}");
            }

            Log.LogMessage(MessageImportance.High, "File processing completed successfully");

            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Error processing file: {ex.Message}");
            return false;
        }
    }
}