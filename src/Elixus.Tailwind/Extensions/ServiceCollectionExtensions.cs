using Elixus.Tailwind.Hosted;
using Elixus.Tailwind.Options;

using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a background watcher for all Tailwind input files.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the watcher to.</param>
    /// <param name="autoDetect">Configures the watcher to automatically detect input and output files.</param>
    public static IServiceCollection AddTailwindWatcher(this IServiceCollection services, bool autoDetect = true)
    {
        return AddTailwindWatcher(services, options =>
        {
            options.AutoDetect = autoDetect;
        }, null);
    }

    /// <summary>
    /// Adds a background watcher for all Tailwind input files.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the watcher to.</param>
    /// <param name="setupAction">Configures the <see cref="TailwindWatchOptions" />.</param>
    /// <param name="configuration">An optional <see cref="IConfiguration" /> to configure the <see cref="TailwindWatchOptions" />.</param>
    public static IServiceCollection AddTailwindWatcher(this IServiceCollection services,
        Action<TailwindWatchOptions>? setupAction, IConfiguration? configuration)
    {
        services.AddOptions<TailwindWatchOptions>();
        services.AddHostedService<TailwindWatcherService>();

        if (configuration is not null)
            services.Configure<TailwindWatchOptions>(configuration);

        if (setupAction is not null)
            services.Configure(setupAction);

        return services;
    }
}