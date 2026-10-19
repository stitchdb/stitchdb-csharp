using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using StitchDB.Core.Http;

namespace StitchDB.Extensions.DependencyInjection;

/// <summary>
/// Registers a <see cref="StitchDB.Client"/> — and each of its sub-clients individually — against
/// <see cref="IHttpClientFactory"/>-managed, pooled <see cref="HttpClient"/>s. This is the fix for the
/// socket/connection-pool exhaustion that an SDK building its own <see cref="HttpClient"/> per instance
/// causes under sustained ASP.NET Core load.
///
/// This package adds no new injection seams of its own: <see cref="StitchDBOptions.HttpClient"/>,
/// <see cref="StitchDBOptions.Proxy"/>, the per-package retry options, and Storage's three named
/// clients on <see cref="Storage.ClientOptions"/> already exist. <see cref="AddStitchDB"/> only wires
/// <see cref="IHttpClientFactory"/>-created clients into those existing seams and calls the
/// already-public <see cref="Client(string, string?, StitchDBOptions?)"/> constructor.
/// </summary>
public static class StitchDBServiceCollectionExtensions
{
    private const string HttpClientName = "StitchDB";
    private const string StorageRequestHttpClientName = "StitchDB.Storage.Request";
    private const string StorageUploadHttpClientName = "StitchDB.Storage.Upload";
    private const string StorageDownloadHttpClientName = "StitchDB.Storage.Download";

    /// <summary>
    /// Registers a scoped <see cref="StitchDB.Client"/>, plus each of its sub-clients individually
    /// (<c>IGotrueClient&lt;User, Session&gt;</c>, <c>IPostgrestClient</c>, <c>IStorageClient&lt;Bucket, FileObject&gt;</c>,
    /// <c>IFunctionsClient</c>, <c>IRealtimeClient&lt;RealtimeSocket, RealtimeChannel&gt;</c>) so a handler can inject
    /// just the one it needs instead of the whole umbrella client.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="stitchdbUrl">The project's StitchDB URL, e.g. "https://xyz.stitchdb.com".</param>
    /// <param name="stitchdbKey">The project's anon or service-role API key.</param>
    /// <param name="configureOptions">
    /// Configures the <see cref="StitchDBOptions"/> passed to every sub-client. Any
    /// <see cref="StitchDBOptions.HttpClient"/> or <see cref="Storage.ClientOptions"/> HttpXClient
    /// properties set here are overwritten — <see cref="AddStitchDB"/> supplies those from
    /// <see cref="IHttpClientFactory"/> so they come from the pooled, DI-managed handlers instead.
    /// </param>
    public static IServiceCollection AddStitchDB(
        this IServiceCollection services,
        string stitchdbUrl,
        string stitchdbKey,
        Action<StitchDBOptions>? configureOptions = null)
    {
        // Read once, purely for the static (non-per-request) proxy/timeout config below — never handed
        // to a Client instance. The Client actually constructed per scope gets its own fresh
        // StitchDBOptions further down, so concurrent scopes never race over shared mutable options.
        var seed = new StitchDBOptions();
        configureOptions?.Invoke(seed);

        services.AddHttpClient(HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => DefaultHttpClientFactory.CreateHandler(seed.Proxy));

        services.AddHttpClient(StorageRequestHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => DefaultHttpClientFactory.CreateHandler(seed.StorageClientOptions.Proxy))
            .ConfigureHttpClient(c => c.Timeout = seed.StorageClientOptions.HttpRequestTimeout);

        services.AddHttpClient(StorageUploadHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => DefaultHttpClientFactory.CreateHandler(seed.StorageClientOptions.Proxy))
            .ConfigureHttpClient(c => c.Timeout = seed.StorageClientOptions.HttpUploadTimeout);

        services.AddHttpClient(StorageDownloadHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => DefaultHttpClientFactory.CreateHandler(seed.StorageClientOptions.Proxy))
            .ConfigureHttpClient(c => c.Timeout = seed.StorageClientOptions.HttpDownloadTimeout);

        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var options = new StitchDBOptions();
            configureOptions?.Invoke(options);

            options.HttpClient = factory.CreateClient(HttpClientName);
            options.StorageClientOptions.HttpRequestClient = factory.CreateClient(StorageRequestHttpClientName);
            options.StorageClientOptions.HttpUploadClient = factory.CreateClient(StorageUploadHttpClientName);
            options.StorageClientOptions.HttpDownloadClient = factory.CreateClient(StorageDownloadHttpClientName);

            return new Client(stitchdbUrl, stitchdbKey, options);
        });

        services.AddScoped(sp => sp.GetRequiredService<Client>().Auth);
        services.AddScoped(sp => sp.GetRequiredService<Client>().Postgrest);
        services.AddScoped(sp => sp.GetRequiredService<Client>().Storage);
        services.AddScoped(sp => sp.GetRequiredService<Client>().Functions);
        services.AddScoped(sp => sp.GetRequiredService<Client>().Realtime);

        return services;
    }
}
