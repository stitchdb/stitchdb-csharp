# StitchDB

[![Build and Test](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StitchDB)](https://www.nuget.org/packages/StitchDB/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

The unified C# client for [StitchDB](https://stitchdb.com). One client composes Auth, Database,
Storage, Realtime, and Edge Functions, so you configure your project once and reach every service
from a single object.

Part of the [StitchDB C# SDK](https://github.com/stitchdb/stitchdb-csharp). To use a single
service on its own, reference its package instead — see [Individual services](#individual-services).

## Installation

```sh
dotnet add package StitchDB
```

Targets .NET Standard 2.1, so it runs on .NET Core / .NET 5+, Xamarin, MAUI, and Unity. See the
[C# guide](https://stitchdb.com/docs/sdk/csharp) for platform notes.

## Getting started

Create a project in the [StitchDB dashboard](https://stitchdb.com/dashboard) and copy your project URL
and **anon key** from **Settings → API keys**. The anon key is safe in client apps; Row Level Security
decides what each user can read and write. Then initialize the client:

```csharp
using StitchDB;

var url = Environment.GetEnvironmentVariable("STITCHDB_URL");
var key = Environment.GetEnvironmentVariable("STITCHDB_KEY");

var options = new StitchDBOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
};

var stitchdb = new Client(url, key, options);
await stitchdb.InitializeAsync();
```

`InitializeAsync()` wires up the child clients and, if a session has been persisted, restores and
refreshes it. Once it returns, reach each service from the client:

```csharp
// Auth
var session = await stitchdb.Auth.SignIn("user@example.com", "password");

// Database (models derive from BaseModel — see the Postgrest package)
var response = await stitchdb.From<Movie>().Get();
var movies = response.Models;

// Call a Postgres function
await stitchdb.Rpc("some_rpc", new { some_arg = "value" });

// Storage
await stitchdb.Storage.From("avatars").Upload("./local.png", "me.png");

// Realtime
var channel = stitchdb.Realtime.Channel("movies");
await channel.Subscribe();

// Edge Functions
await stitchdb.Functions.Invoke("hello-world");
```

## Configuration

Pass a `StitchDBOptions` to the constructor:

| Option | Default | Purpose |
| --- | --- | --- |
| `AutoRefreshToken` | `true` | Refresh the user's access token in the background before it expires. |
| `AutoConnectRealtime` | `false` | Connect the Realtime socket during `InitializeAsync()`. |
| `SessionHandler` | no-op | Persist, restore, and destroy the auth session (see below). |
| `Schema` | `"public"` | Postgres schema used by Database and Realtime. |
| `Headers` | empty | Extra headers forwarded to every child client. |
| `StorageClientOptions` | defaults | Options passed through to the Storage client. |

### Session persistence

By default the client does not persist the auth session, so the user is signed out when the process
ends. Provide a `SessionHandler` to save, load, and destroy the session — for example to a file or to
the platform's secure storage.

> **A note on keys:** some APIs (user administration, bypassing RLS, etc.) require an elevated key
> (the `service_role` key) rather than the `anon` key. Never expose an elevated key in client-side code, and use a
> separate client instance for service and user contexts.

### API keys

| Key | Use in |
| --- | --- |
| `anon` | Client-side apps (browsers, mobile, desktop). Requests run as the signed-in user, or as `anon` without a session, and Row Level Security applies. |
| `service_role` | Server-side only. Bypasses Row Level Security — never ship it in client code. |

Both are in **Settings → API keys** in the dashboard. See [API keys](https://stitchdb.com/docs/platform/api-keys).

## Observability (OpenTelemetry)

The clients emit traces and metrics through `System.Diagnostics`, so you can wire them into
OpenTelemetry (or any `ActivityListener` / `MeterListener`) without taking a dependency on the
OpenTelemetry packages. Emission is zero-cost while nothing is listening, so it is always on and stays
silent until you subscribe.

Register every instrumented source at once with `StitchDBDiagnostics.SourceNames` — each client
shares one name between its `ActivitySource` and its `Meter`, so the same list works for tracing and
metrics:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StitchDB;

// Requires OpenTelemetry.Extensions.Hosting and an exporter package (e.g. OTLP) in your app.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(StitchDBDiagnostics.SourceNames.ToArray())
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(StitchDBDiagnostics.SourceNames.ToArray())
        .AddOtlpExporter());
```

This covers the Auth, Postgrest, Functions, and Storage clients. Realtime is **not** yet instrumented,
so no websocket telemetry is emitted. URLs are recorded without their query string, and no token,
credential, or payload is placed in a tag. Each service package's README documents the specific spans
and metrics it produces.

## Individual services

Each service is also published as a standalone package, useful when you only need one:

| Service | Package | Docs |
| --- | --- | --- |
| Auth | `StitchDB.Gotrue` | [README](../Gotrue/README.md) |
| Database | `StitchDB.Postgrest` | [README](../Postgrest/README.md) |
| Storage | `StitchDB.Storage` | [README](../Storage/README.md) |
| Realtime | `StitchDB.Realtime` | [README](../Realtime/README.md) |
| Functions | `StitchDB.Functions` | [README](../Functions/README.md) |

## Documentation

- [StitchDB C# SDK guide](https://stitchdb.com/docs/sdk/csharp)
- [StitchDB documentation](https://stitchdb.com/docs)

## Contributing

Contributions are welcome. See the [repository root](https://github.com/stitchdb/stitchdb-csharp)
for how to build and test the SDK.

## License

[MIT](../../LICENSE)
