# Elixus.Tailwind

Automatically install and invoke TailwindCSS builds in your .NET projects with seamless Blazor integration.

## Features

- **Build-time Processing** - Automatically process TailwindCSS files during project build with minification
- **Runtime Watching** - Hot-reload TailwindCSS changes during development without restarting your application
- **Auto-detection** - Automatically discover Tailwind input files from your project configuration
- **Multi-platform** - Cross-platform support for Windows, macOS, and Linux
- **Scoped Styles** - Support for Blazor scoped styles integration
- **Zero Config** - Works out of the box with sensible defaults

## Installation

Install the package via NuGet:

```bash
dotnet add package Elixus.Tailwind
```

## Quick Start

### 1. Configure Your Project

Add TailwindCSS input files to your `.csproj`:

```xml
<ItemGroup>
  <TailwindFile Include="Styles/app.css" />
</ItemGroup>
```

### 2. Enable Runtime Watching (Development)

In your `Program.cs`, add the Tailwind watcher service:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Tailwind watcher for development
builder.Services.AddTailwindWatcher(autoDetect: true);

// ... rest of your configuration
```

### 3. Build Your Project

That's it! The TailwindCSS CLI will be automatically downloaded and invoked during build, and your styles will be watched for changes during development.

## Configuration

### Basic Configuration

The simplest configuration uses auto-detection:

```csharp
builder.Services.AddTailwindWatcher(autoDetect: true);
```

### Advanced Configuration

For more control, configure the watcher options:

```csharp
builder.Services.AddTailwindWatcher(options =>
{
    options.AutoDetect = true;
    options.RootDirectory = "/path/to/project";
    options.Files.Add(new TailwindFile
    {
        Input = "Styles/app.css",
        Output = "wwwroot/app.css"
    });
}, configuration: null);
```

### Project File Configuration

Define TailwindCSS files in your `.csproj`:

```xml
<ItemGroup>
  <TailwindFile Include="Styles/app.css" />
  <TailwindFile Include="Styles/admin.css" Output="wwwroot/css/admin.css" />
</ItemGroup>
```

### MSBuild Properties

You can customize the TailwindCSS CLI download and configuration using MSBuild properties:

```xml
<PropertyGroup>
  <!-- Specify the TailwindCSS version to download (default: v4.1.14) -->
  <TailwindVersion>v4.1.14</TailwindVersion>

  <!-- Customize the directory where the CLI binary is stored (default: $(MSBuildProjectDirectory)\.tailwind) -->
  <TailwindCliDirectory>$(MSBuildProjectDirectory)\.tailwind</TailwindCliDirectory>
</PropertyGroup>
```

**Available Properties:**

- **`TailwindVersion`** - The version of TailwindCSS CLI to download from GitHub releases (default: `v4.1.14`)
- **`TailwindCliDirectory`** - The directory where the TailwindCSS CLI binary will be stored (default: `.tailwind` in your project directory)

The CLI binary is automatically downloaded for your platform (Windows x64/ARM64, macOS x64/ARM64, Linux x64/ARM64/ARMv7) during the first build.

## How It Works

### Build-Time Processing

The `Elixus.Tailwind.MSBuild` package provides an MSBuild task that:
- Downloads the appropriate TailwindCSS CLI binary for your platform
- Processes your TailwindCSS files during build
- Minifies output for production builds
- Outputs processed files to your `wwwroot` directory

### Runtime Watching

The `TailwindWatcherService` is a hosted service that:
- Monitors your TailwindCSS input files for changes
- Automatically rebuilds styles when changes are detected
- Logs output from the TailwindCSS CLI
- Supports multiple input files simultaneously

## Architecture

This package consists of two components:

- **Elixus.Tailwind** - Runtime watcher service and configuration
- **Elixus.Tailwind.MSBuild** - MSBuild tasks for build-time processing

## Requirements

- .NET 9.0 or higher
- TailwindCSS (automatically downloaded)

## Example Project

See the [BlazorExample](samples/BlazorExample) project for a complete working example.

## Development

### Building from Source

```bash
git clone https://github.com/elixus/Elixus.Tailwind.git
cd Elixus.Tailwind
dotnet build
```

### Running the Example

```bash
cd samples/BlazorExample
dotnet run
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.

## Author

Wannes Gennar - [Elixus](https://github.com/elixus)

## Links

- [Repository](https://github.com/elixus/Elixus.Tailwind)
- [NuGet Package](https://www.nuget.org/packages/Elixus.Tailwind)
- [TailwindCSS Documentation](https://tailwindcss.com/docs)
