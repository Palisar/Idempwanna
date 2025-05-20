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
    /// <returns>An <see cref="IdempotencyBuilder"/> that can be used to configure idempotency services.</returns>
    public static IdempotencyBuilder AddIdempotency(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register the default implementations
        services.TryAddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
        services.TryAddSingleton<IIdempotencyCache, InMemoryIdempotencyCache>();
        services.TryAddScoped<IIdempotencyService, DefaultIdempotencyService>();

        return new IdempotencyBuilder(services);
    }
}

/// <summary>
/// A builder for configuring idempotency services.
/// </summary>
public class IdempotencyBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyBuilder"/> class.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    public IdempotencyBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Configures the idempotency services to use in-memory caching.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="IdempotencyOptions"/>.</param>
    /// <returns>The <see cref="IdempotencyBuilder"/> so that additional calls can be chained.</returns>
    public IdempotencyBuilder WithInMemoryCache(Action<IdempotencyOptions>? configureOptions = null)
    {
        // Configure options if provided
        if (configureOptions != null)
        {
            _services.Configure(configureOptions);
        }

        // Register the in-memory cache implementation
        _services.RemoveAll<IIdempotencyCache>();
        _services.TryAddSingleton<IIdempotencyCache, InMemoryIdempotencyCache>();

        return this;
    }

    /// <summary>
    /// Configures the idempotency services to use a custom cache implementation.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache implementation.</typeparam>
    /// <param name="configureOptions">A delegate to configure the <see cref="IdempotencyOptions"/>.</param>
    /// <returns>The <see cref="IdempotencyBuilder"/> so that additional calls can be chained.</returns>
    public IdempotencyBuilder WithCustomCache<TCache>(Action<IdempotencyOptions>? configureOptions = null)
        where TCache : class, IIdempotencyCache
    {
        // Configure options if provided
        if (configureOptions != null)
        {
            _services.Configure(configureOptions);
        }

        // Register with custom cache implementation
        _services.RemoveAll<IIdempotencyCache>();
        _services.TryAddSingleton<IIdempotencyCache, TCache>();

        return this;
    }

    /// <summary>
    /// Configures the idempotency key generator.
    /// </summary>
    /// <typeparam name="TGenerator">The type of the key generator implementation.</typeparam>
    /// <returns>The <see cref="IdempotencyBuilder"/> so that additional calls can be chained.</returns>
    public IdempotencyBuilder WithCustomKeyGenerator<TGenerator>()
        where TGenerator : class, IIdempotencyKeyGenerator
    {
        _services.RemoveAll<IIdempotencyKeyGenerator>();
        _services.TryAddSingleton<IIdempotencyKeyGenerator, TGenerator>();

        return this;
    }

    /// <summary>
    /// Configures the idempotency service implementation.
    /// </summary>
    /// <typeparam name="TService">The type of the service implementation.</typeparam>
    /// <returns>The <see cref="IdempotencyBuilder"/> so that additional calls can be chained.</returns>
    public IdempotencyBuilder WithCustomService<TService>()
        where TService : class, IIdempotencyService
    {
        _services.RemoveAll<IIdempotencyService>();
        _services.TryAddScoped<IIdempotencyService, TService>();

        return this;
    }

    /// <summary>
    /// Configures the idempotency options.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="IdempotencyOptions"/>.</param>
    /// <returns>The <see cref="IdempotencyBuilder"/> so that additional calls can be chained.</returns>
    public IdempotencyBuilder Configure(Action<IdempotencyOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        _services.Configure(configureOptions);

        return this;
    }
}
