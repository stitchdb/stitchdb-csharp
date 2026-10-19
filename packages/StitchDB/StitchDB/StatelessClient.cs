using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StitchDB.Core;
using StitchDB.Extensions;
using StitchDB.Functions.Interfaces;
using StitchDB.Postgrest.Models;
using StitchDB.Postgrest.Responses;
using StitchDB.Storage;
using StitchDB.Storage.Interfaces;

namespace StitchDB;

/// <summary>
/// A Static class representing a StitchDB Client.
/// </summary>
public static class StatelessClient
{
    /// <summary>
    /// Returns an instance of <see cref="Gotrue.ClientOptions"/> given a provided url and key.
    /// </summary>
    /// <param name="stitchdbUrl"></param>
    /// <param name="stitchdbKey"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static Gotrue.ClientOptions GetAuthOptions(string stitchdbUrl, string? stitchdbKey = null, StitchDBOptions? options = null)
    {
        options ??= new StitchDBOptions();

        var headers = GetAuthHeaders(stitchdbKey, options).MergeLeft(options.Headers);

        return new Gotrue.ClientOptions
        {
            Url = string.Format(options.AuthUrlFormat, stitchdbUrl),
            Headers = headers,
            HttpClient = options.HttpClient,
            Proxy = options.Proxy,
            Retry = options.GotrueRetry
        };
    }

    /// <summary>
    /// Returns an instance of <see cref="Postgrest.ClientOptions"/> for a given stitchdb key.
    /// </summary>
    /// <param name="stitchdbKey"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static Postgrest.ClientOptions GetRestOptions(string? stitchdbKey = null, StitchDBOptions? options = null)
    {
        options ??= new StitchDBOptions();

        var headers = GetAuthHeaders(stitchdbKey, options).MergeLeft(options.Headers);

        return new Postgrest.ClientOptions
        {
            Schema = options.Schema,
            Headers = headers,
            Retry = options.PostgrestRetry,
            Timeout = options.PostgrestTimeout,
            HttpClient = options.HttpClient,
            Proxy = options.Proxy
        };
    }

    /// <summary>
    /// StitchDB Storage allows you to manage user-generated content, such as photos or videos.
    /// </summary>
    /// <param name="stitchdbUrl"></param>
    /// <param name="stitchdbKey"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IStorageClient<Bucket, FileObject> Storage(string stitchdbUrl, string? stitchdbKey = null, StitchDBOptions? options = null)
    {
        options ??= new StitchDBOptions();

        var headers = GetAuthHeaders(stitchdbKey, options).MergeLeft(options.Headers);

        return new Storage.Client(string.Format(options.StorageUrlFormat, stitchdbUrl), options.StorageClientOptions, headers);
    }

    /// <summary>
    /// StitchDB Edge functions allow you to deploy and invoke edge functions.
    /// </summary>
    /// <param name="stitchdbUrl"></param>
    /// <param name="stitchdbKey"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IFunctionsClient Functions(string stitchdbUrl, string stitchdbKey, StitchDBOptions? options = null)
    {
        options ??= new StitchDBOptions();

        // StitchDB serves Edge Functions under the project URL: <project>/functions/v1.
        var functionsUrl = string.Format(options.FunctionsUrlFormat, stitchdbUrl);

        var headers = GetAuthHeaders(stitchdbKey, options, omitApiKeyAsBearer: true).MergeLeft(options.Headers);
        var client = new Functions.Client(functionsUrl, options: new Functions.ClientOptions
        {
            HttpClient = options.HttpClient,
            Proxy = options.Proxy,
            Retry = options.FunctionsRetry
        });
        client.GetHeaders = () => headers;

        return client;
    }

    /// <summary>
    /// Gets the Postgrest client to prepare for a query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static StitchDBTable<T> From<T>(string stitchdbUrl, string stitchdbKey, StitchDBOptions? options = null) where T : BaseModel, new()
    {
        options ??= new StitchDBOptions();

        var restUrl = string.Format(options.RestUrlFormat, stitchdbUrl);
        var realtimeUrl = string.Format(options.RealtimeUrlFormat, stitchdbUrl).Replace("http", "ws");

        var restOptions = GetRestOptions(stitchdbKey, options);
        restOptions.Headers.MergeLeft(options.Headers);

        var realtimeOptions = new Realtime.ClientOptions
        {
            Parameters = { ApiKey = stitchdbKey },
            WebSocketFactory = options.WebSocketFactory
        };

        var postgrestClient = new Postgrest.Client(restUrl, restOptions);
        var realtimeClient = new Realtime.Client(realtimeUrl, realtimeOptions);

        return new StitchDBTable<T>(postgrestClient, realtimeClient, options.Schema);
    }

    /// <summary>
    /// Runs a remote procedure.
    /// </summary>
    /// <param name="stitchdbUrl"></param>
    /// <param name="stitchdbKey"></param>
    /// <param name="procedureName"></param>
    /// <param name="parameters"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static Task<BaseResponse> Rpc(string stitchdbUrl, string stitchdbKey, string procedureName, Dictionary<string, object> parameters, StitchDBOptions? options = null)
    {
        options ??= new StitchDBOptions();

        return new Postgrest.Client(string.Format(options.RestUrlFormat, stitchdbUrl), GetRestOptions(stitchdbKey, options)).Rpc(procedureName, parameters);
    }


    internal static Dictionary<string, string> GetAuthHeaders(string? stitchdbKey, StitchDBOptions options, bool omitApiKeyAsBearer = false)
    {
        ApiKey.CheckFormat(stitchdbKey);

        var headers = new Dictionary<string, string>();

        headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

        if (stitchdbKey != null)
        {
            headers["apiKey"] = stitchdbKey;
        }

        // Matches the upstream client behaviour.
        if (options.Headers.TryGetValue("Authorization", out var header))
        {
            headers["Authorization"] = header;
        }
        // The stateless client has no session, so the key is the only Bearer candidate. A
        // new-format (opaque) key must not be sent as a Bearer token on the Edge Functions path.
        else if (!(omitApiKeyAsBearer && ApiKey.IsNewApiKey(stitchdbKey)))
        {
            headers["Authorization"] = $"Bearer {stitchdbKey}";
        }

        return headers;
    }
}
