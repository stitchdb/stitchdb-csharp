using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StitchDB.Core;
using StitchDB.Functions.Interfaces;
using StitchDB.Gotrue;
using StitchDB.Gotrue.Interfaces;
using StitchDB.Interfaces;
using StitchDB.Postgrest.Interfaces;
using StitchDB.Postgrest.Models;
using StitchDB.Postgrest.Responses;
using StitchDB.Realtime;
using StitchDB.Realtime.Interfaces;
using StitchDB.Storage;
using StitchDB.Storage.Interfaces;
using static StitchDB.Gotrue.Constants;

namespace StitchDB;

/// <summary>
/// A class representing the StitchDB Client, coordinating between all child APIs
/// </summary>
public class Client : IStitchDBClient<User, Session, RealtimeSocket, RealtimeChannel, Bucket, FileObject>
{
    /// <summary>
    /// StitchDB Auth allows you to create and manage user sessions for access to data that is secured by access policies.
    /// </summary>
    public IGotrueClient<User, Session> Auth
    {
        get => this.auth;
        set
        {
            // Remove existing internal state listener (if applicable)
            this.auth.RemoveStateChangedListener(this.Auth_StateChanged);
            this.auth = value;
            this.auth.AddStateChangedListener(this.Auth_StateChanged);
        }
    }

    private IGotrueClient<User, Session> auth;

    /// <summary>
    /// Returns a Stateless Gotrue Admin client given a service_key JWT. This should really only be accessed from a
    /// server environment where a private service_key would remain secure.
    /// </summary>
    /// <param name="serviceKey">
    /// The project's <c>service_role</c> key. It grants elevated,
    /// RLS-bypassing access, so only supply it from a secure server environment.
    /// </param>
    /// <returns>An admin client scoped to the supplied service key.</returns>
    public IGotrueAdminClient<User> AdminAuth(string serviceKey) =>
        new AdminClient(serviceKey, new Gotrue.ClientOptions
        {
            Url = string.Format(this.options.AuthUrlFormat, this.stitchdbUrl),
            AutoRefreshToken = this.options.AutoRefreshToken,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.GotrueRetry
        })
        {
            GetHeaders = () => this.GetAuthHeaders(),
        };

    /// <summary>
    /// StitchDB Realtime allows for realtime feedback on database changes.
    /// </summary>
    public IRealtimeClient<RealtimeSocket, RealtimeChannel> Realtime
    {
        get => this.realtime;
        set
        {
            // Disconnect from previous RealtimeSocket (if applicable)
            this.realtime.Disconnect();
            this.realtime = value;
        }
    }

    private IRealtimeClient<RealtimeSocket, RealtimeChannel> realtime;

    /// <summary>
    /// StitchDB Edge functions allow you to deploy and invoke edge functions.
    /// </summary>
    public IFunctionsClient Functions
    {
        get => this.functions;
        set => this.functions = value;
    }

    private IFunctionsClient functions;

    /// <summary>
    /// StitchDB Postgrest allows for strongly typed REST interactions with your database.
    /// </summary>
    public IPostgrestClient Postgrest
    {
        get => this.postgrest;
        set => this.postgrest = value;
    }

    private IPostgrestClient postgrest;

    /// <summary>
    /// StitchDB Storage allows you to manage user-generated content, such as photos or videos.
    /// </summary>
    public IStorageClient<Bucket, FileObject> Storage
    {
        get => this.storage;
        set => this.storage = value;
    }

    private IStorageClient<Bucket, FileObject> storage;

    private readonly string? stitchdbUrl;
    private readonly string? stitchdbKey;
    private readonly StitchDBOptions options;

    /// <summary>
    /// Constructor supplied for dependency injection support. The children are used as provided; the
    /// caller is responsible for wiring their auth headers (unlike the url/key constructor, which
    /// builds and wires them itself).
    /// </summary>
    /// <param name="auth">The Gotrue (auth) client.</param>
    /// <param name="realtime">The Realtime client; its <c>Options.PostgrestClient</c> is set to <paramref name="postgrest"/>.</param>
    /// <param name="functions">The Edge Functions client.</param>
    /// <param name="postgrest">The Postgrest (database) client.</param>
    /// <param name="storage">The Storage client.</param>
    /// <param name="options">Client configuration.</param>
    public Client(IGotrueClient<User, Session> auth, IRealtimeClient<RealtimeSocket, RealtimeChannel> realtime,
        IFunctionsClient functions, IPostgrestClient postgrest, IStorageClient<Bucket, FileObject> storage,
        StitchDBOptions options)
    {
        this.auth = auth;
        this.realtime = realtime;
        this.functions = functions;
        this.postgrest = postgrest;
        this.storage = storage;
        this.options = options;
        this.realtime.Options.PostgrestClient = this.postgrest;
    }

    /// <summary>
    /// Creates a new StitchDB Client.
    /// </summary>
    /// <param name="stitchdbUrl">
    /// The project URL from <b>Settings → API keys</b>, e.g. <c>https://xyzcompany.stitchdb.com</c>. The
    /// service URLs (auth, rest, realtime, storage, functions) are derived from it.
    /// </param>
    /// <param name="stitchdbKey">
    /// The project API key from <b>Settings → API keys</b>: the <c>anon</c> key for client-side apps, or the
    /// <c>service_role</c> key only in trusted server-side contexts, as it bypasses Row Level Security.
    /// </param>
    /// <param name="options">Client configuration; a default <see cref="StitchDBOptions"/> is used when null.</param>
    public Client(string stitchdbUrl, string? stitchdbKey, StitchDBOptions? options = null)
    {
        this.stitchdbUrl = stitchdbUrl;
        this.stitchdbKey = stitchdbKey;
        this.options = options ?? new StitchDBOptions();

        ApiKey.CheckFormat(stitchdbKey);

        var authUrl = string.Format(this.options.AuthUrlFormat, stitchdbUrl);
        var restUrl = string.Format(this.options.RestUrlFormat, stitchdbUrl);
        var realtimeUrl = string.Format(this.options.RealtimeUrlFormat, stitchdbUrl).Replace("http", "ws");
        var storageUrl = string.Format(this.options.StorageUrlFormat, stitchdbUrl);
        var schema = this.options.Schema;

        // StitchDB serves Edge Functions under the project URL: <project>/functions/v1.
        var functionsUrl = string.Format(this.options.FunctionsUrlFormat, stitchdbUrl);

        // Init Auth
        var gotrueOptions = new Gotrue.ClientOptions
        {
            Url = authUrl,
            AutoRefreshToken = this.options.AutoRefreshToken,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.GotrueRetry
        };
        this.auth = new Gotrue.Client(gotrueOptions);
        this.auth.SetPersistence(this.options.SessionHandler);
        this.auth.AddStateChangedListener(this.Auth_StateChanged);
        this.auth.GetHeaders = () => this.GetAuthHeaders();
        this.postgrest = new Postgrest.Client(restUrl, new Postgrest.ClientOptions
        {
            Schema = schema,
            Retry = this.options.PostgrestRetry,
            Timeout = this.options.PostgrestTimeout,
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy
        });
        this.postgrest.GetHeaders = () => this.GetAuthHeaders();

        // Init Realtime

        var realtimeOptions = new Realtime.ClientOptions
        {
            Parameters = { ApiKey = this.stitchdbKey },
            PostgrestClient = this.postgrest,
            WebSocketFactory = this.options.WebSocketFactory
        };
        this.realtime = new Realtime.Client(realtimeUrl, realtimeOptions);
        this.realtime.GetHeaders = () => this.GetAuthHeaders();
        this.functions = new Functions.Client(functionsUrl, options: new Functions.ClientOptions
        {
            HttpClient = this.options.HttpClient,
            Proxy = this.options.Proxy,
            Retry = this.options.FunctionsRetry
        });
        // Edge Functions must not receive a new-format (opaque) API key on the Authorization
        // header — the gateway would try to parse it as a JWT and reject the request.
        this.functions.GetHeaders = () => this.GetAuthHeaders(omitApiKeyAsBearer: true);
        this.storage = new Storage.Client(storageUrl, this.options.StorageClientOptions);
        this.storage.GetHeaders = () => this.GetAuthHeaders();
    }


    /// <summary>
    /// Restores the session persisted by <see cref="StitchDBOptions.SessionHandler"/>, refreshes it, and connects to realtime (if `options.AutoConnectRealtime` is set)
    /// </summary>
    public async Task<IStitchDBClient<User, Session, RealtimeSocket, RealtimeChannel, Bucket, FileObject>>
        InitializeAsync()
    {
        // Don't clobber a session already set by the caller with a stale one from disk.
        if (this.Auth.CurrentSession == null)
            await this.Auth.LoadSessionAsync().ConfigureAwait(false);
        await this.Auth.RetrieveSessionAsync();

        if (this.options.AutoConnectRealtime)
            await this.Realtime.ConnectAsync();

        return this;
    }

    private void Auth_StateChanged(object sender, AuthState e)
    {
        switch (e)
        {
            // Pass new Auth down to Realtime
            // Matches the upstream client behaviour.
            case AuthState.SignedIn:
            case AuthState.TokenRefreshed:
            case AuthState.UserUpdated:
                if (this.Auth.CurrentSession?.AccessToken != null)
                    this.Realtime.SetAuth(this.Auth.CurrentSession.AccessToken);
                break;
            // Remove Realtime Subscriptions on Auth Sign-out.
            case AuthState.SignedOut:
                this.Realtime.Subscriptions.Values?.ToList().ForEach(subscription => subscription.Unsubscribe());
                break;
            case AuthState.PasswordRecovery:
            case AuthState.Shutdown: break;
            case AuthState.MfaChallengeVerified:
            default: throw new ArgumentOutOfRangeException(nameof(e), e, null);
        }
    }

    /// <summary>
    /// Gets the Postgrest client to prepare for a query.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public IStitchDBTable<TModel, RealtimeChannel> From<TModel>() where TModel : BaseModel, new() =>
        new StitchDBTable<TModel>(this.Postgrest, this.Realtime);

    /// <inheritdoc />
    public Task<BaseResponse> Rpc(string procedureName, object? parameters) => this.postgrest.Rpc(procedureName, parameters);

    /// <inheritdoc />
    public Task<TModeledResponse?> Rpc<TModeledResponse>(string procedureName, object? parameters) => this.postgrest.Rpc<TModeledResponse>(procedureName, parameters);

    /// <summary>
    /// Produces a dictionary of Headers that will be supplied to child clients.
    /// </summary>
    /// <param name="omitApiKeyAsBearer">
    /// When true, a new-format (opaque) API key is not used as the <c>Authorization: Bearer</c>
    /// stand-in for an absent session — only the <c>apikey</c> header carries it. Used by the Edge
    /// Functions path, whose gateway rejects opaque keys parsed as a JWT.
    /// </param>
    internal Dictionary<string, string> GetAuthHeaders(bool omitApiKeyAsBearer = false)
    {
        var headers = CaseInsensitiveHeaders();
        headers["X-Client-Info"] = Util.GetAssemblyVersion(typeof(Client));

        if (this.stitchdbKey != null)
            headers["apiKey"] = this.stitchdbKey;

        // Matches the upstream client behaviour.
        if (this.options.Headers.TryGetValue("Authorization", out var header))
        {
            headers["Authorization"] = header;
        }
        else
        {
            var accessToken = this.Auth.CurrentSession?.AccessToken;
            if (accessToken != null)
                headers["Authorization"] = $"Bearer {accessToken}";
            else if (!(omitApiKeyAsBearer && ApiKey.IsNewApiKey(this.stitchdbKey)))
                headers["Authorization"] = $"Bearer {this.stitchdbKey}";
        }

        // Add supplied headers from `ClientOptions` by developer
        foreach (var kvp in this.options.Headers)
            headers[kvp.Key] = kvp.Value;

        return headers;
    }

    private static Dictionary<string, string> CaseInsensitiveHeaders() => new(StringComparer.OrdinalIgnoreCase);
}
