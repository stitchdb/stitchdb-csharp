using System.Collections.Generic;
using System.Diagnostics;
using StitchDB.Realtime;
using StitchDB.Realtime.Socket;
using Client = StitchDB.Realtime.Client;

namespace Realtime.Tests;

internal static class Helpers
{
    public const string ApiKeyAnon = "local-test-anon-key";
    public const string ApiKey = "local-test-service-key";

    private const string SocketEndpoint = "ws://127.0.0.1:54321/realtime/v1";
    private const string RestEndpoint = "http://localhost:54321/rest/v1";

    public static StitchDB.Postgrest.Client RestClient() => new(RestEndpoint, new StitchDB.Postgrest.ClientOptions());

    public static Client SocketClient()
    {
        var client = new Client(SocketEndpoint, new ClientOptions
        {
            Parameters = new SocketOptionsParameters
            {
                ApiKey = ApiKeyAnon
            }
        });

        client.AddDebugHandler((_, message, _) => Debug.WriteLine(message));

        return client;
    }

    public static Client PrivateSocketClient()
    {
        var client = new Client(SocketEndpoint, new ClientOptions
        {
            Parameters = new SocketOptionsParameters
            {
                ApiKey = ApiKey
            }
        });

        client.AddDebugHandler((_, message, _) => Debug.WriteLine(message));

        return client;
    }
}
