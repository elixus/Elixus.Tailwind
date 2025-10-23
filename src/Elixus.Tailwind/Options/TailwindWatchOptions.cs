namespace Elixus.Tailwind.Options;

/// <summary>
/// Configuration options for the watcher.
/// </summary>
public sealed class TailwindWatchOptions
{
    /// <summary>
    /// Whether to automatically detect tailwind files from the csproj file.
    /// </summary>
    public bool AutoDetect { get; set; } = false;

    /// <summary>
    /// The root path containing the project file.
    /// If left to <c>null</c>, the path is automatically detected.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>
    /// A list of all tailwind files to be processed.
    /// </summary>
    public List<TailwindInput> Inputs { get; set; } = [];
}

/// <summary>
/// Represents an input/output configuration of a single file.
/// </summary>
public sealed class TailwindInput
{
    /// <summary>
    /// The source stylesheet.
    /// </summary>
    public required string Input { get; set; }

    /// <summary>
    /// The file to output to.
    /// If left to <c>null</c>, automatically detects the output.
    /// </summary>
    public string? Output { get; set; }
}