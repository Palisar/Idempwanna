using System;
using Idempwanna.Core.Implementations;
using Idempwanna.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Idempwanna.Core.Configuration;

/// <summary>
/// Extension methods for setting up idempotency services in an <see cref="IServiceCollection" />.
/// </summary>
public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Adds idempotency services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register the default implementations
        services.TryAddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
        services.TryAddSingleton<IIdempotencyCache, InMemoryIdempotencyCache>();
        services.TryAddScoped<IIdempotencyService, DefaultIdempotencyService>();

        return services;
    }

    /// <summary>
    /// Adds idempotency services with in-memory caching to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="IdempotencyOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddIdempotencyWithInMemoryCache(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configureOptions = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Configure options if provided
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register the default implementations
        services.TryAddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
        services.TryAddSingleton<IIdempotencyCache, InMemoryIdempotencyCache>();
        services.TryAddScoped<IIdempotencyService, DefaultIdempotencyService>();

        return services;
    }

    /// <summary>
    /// Adds idempotency services with a custom cache implementation to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="IdempotencyOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddIdempotencyWithCustomCache<TCache>(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configureOptions = null)
        where TCache : class, IIdempotencyCache
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Configure options if provided
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register with custom cache implementation
        services.TryAddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
        services.TryAddSingleton<IIdempotencyCache, TCache>();
        services.TryAddScoped<IIdempotencyService, DefaultIdempotencyService>();

        return services;
    }
}
