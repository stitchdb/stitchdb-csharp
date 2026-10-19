# StitchDB C# SDK

[![Build and Test](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StitchDB?label=StitchDB)](https://www.nuget.org/packages/StitchDB/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

The official C# client for [StitchDB](https://stitchdb.com). This is a monorepo: the top-level
`StitchDB` client and every service package it builds on live here, under [`packages/`](./packages).

Use the `StitchDB` meta-package for the full client, or reference an individual service package
(Auth, Postgrest, Storage, Realtime, Functions) on its own.

## Packages

| Package | NuGet | Source | Purpose |
| --- | --- | --- | --- |
| **StitchDB** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB)](https://www.nuget.org/packages/StitchDB/) | [`packages/StitchDB`](./packages/StitchDB) | The unified client that wires the services below together. Start here. |
| **StitchDB.Gotrue** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Gotrue)](https://www.nuget.org/packages/StitchDB.Gotrue/) | [`packages/Gotrue`](./packages/Gotrue) | Authentication — email/password, OAuth, SSO, magic links. |
| **StitchDB.Postgrest** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Postgrest)](https://www.nuget.org/packages/StitchDB.Postgrest/) | [`packages/Postgrest`](./packages/Postgrest) | Query your database through the auto-generated REST API, with LINQ. |
| **StitchDB.Storage** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Storage)](https://www.nuget.org/packages/StitchDB.Storage/) | [`packages/Storage`](./packages/Storage) | File storage — upload, download, signed URLs, buckets. |
| **StitchDB.Realtime** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Realtime)](https://www.nuget.org/packages/StitchDB.Realtime/) | [`packages/Realtime`](./packages/Realtime) | Realtime — Postgres changes, Broadcast, and Presence over websockets. |
| **StitchDB.Functions** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Functions)](https://www.nuget.org/packages/StitchDB.Functions/) | [`packages/Functions`](./packages/Functions) | Invoke Edge Functions. |
| **StitchDB.Core** | [![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Core)](https://www.nuget.org/packages/StitchDB.Core/) | [`packages/Core`](./packages/Core) | Shared primitives used by the packages above. Rarely referenced directly. |

Every package targets .NET Standard 2.1, so it runs on .NET Core 3.0+ / .NET 5+, recent MAUI, Xamarin, and Unity.
Guides and API reference: https://stitchdb.com/docs/sdk/csharp

## Installation

```sh
dotnet add package StitchDB
```

## Quickstart

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

// Auth
await stitchdb.Auth.SignIn("user@example.com", "password");

// Database
var movies = await stitchdb.From<Movie>().Get();

// Storage
await stitchdb.Storage.From("avatars").Download("me.png", null);
```

Each service is reached from the initialized client: `stitchdb.Auth`, `stitchdb.From<T>()` /
`stitchdb.Rpc(...)`, `stitchdb.Storage`, `stitchdb.Realtime`, and `stitchdb.Functions`. For using a
single service on its own, see that package's README.

> **A note on keys:** some APIs (user administration, bypassing RLS, etc.) require the `service_key`
> rather than the public/anon key. Never expose a `service_key` in client-side code, and use a
> separate client instance for service and user contexts.

## Observability (OpenTelemetry)

The clients emit traces and metrics through `System.Diagnostics`, so you can wire them into
OpenTelemetry (or any `ActivityListener` / `MeterListener`) without taking a dependency on the
OpenTelemetry packages. Emission is zero-cost while nothing is listening, so it is always on and
stays silent until you subscribe.

Register every instrumented source at once with `StitchDBDiagnostics.SourceNames` — each client
shares one name between its `ActivitySource` and its `Meter`, so the same list works for tracing and
metrics:

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StitchDB;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(StitchDBDiagnostics.SourceNames.ToArray())
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(StitchDBDiagnostics.SourceNames.ToArray())
        .AddOtlpExporter());
```

This covers the Auth, Postgrest, Functions, and Storage clients (Realtime is not yet instrumented).
URLs are recorded without their query string, and no token, credential, or payload is placed in a
tag. Each package's README documents the specific spans and metrics it produces.

## Documentation

- [StitchDB C# SDK guide](https://stitchdb.com/docs/sdk/csharp)
- [StitchDB documentation](https://stitchdb.com/docs)
- Support: [support@stitchdb.com](mailto:support@stitchdb.com) or **Support** in the StitchDB dashboard

## Repository layout

```
packages/
  StitchDB/    The unified client (NuGet: StitchDB)
  Gotrue/      Auth        (NuGet: StitchDB.Gotrue)
  Postgrest/   Database    (NuGet: StitchDB.Postgrest)
  Storage/     Storage     (NuGet: StitchDB.Storage)
  Realtime/    Realtime    (NuGet: StitchDB.Realtime)
  Functions/   Functions   (NuGet: StitchDB.Functions)
  Core/        Shared      (NuGet: StitchDB.Core)
```

Each `packages/<Name>/` holds the library project and its test project. Package metadata common to
every project (license, authors, repository) lives in [`Directory.Build.props`](./Directory.Build.props);
dependency versions are managed centrally in [`Directory.Packages.props`](./Directory.Packages.props).

## Building & testing

The SDK builds with the .NET SDK version pinned in [`global.json`](./global.json).

```sh
dotnet restore
dotnet build StitchDB.sln --configuration Release
dotnet test  StitchDB.sln --configuration Release
```

Some test suites are end-to-end and need a local StitchDB stack via the
[StitchDB CLI](https://stitchdb.com/docs/cli/local-development) (`stitchdb start`, which requires Docker).

## Versioning & releases

Releases follow semantic versioning and are published to NuGet from GitHub releases. Commits follow
[Conventional Commits](https://www.conventionalcommits.org/).

## Contributing

Contributions are welcome — please open an issue to discuss substantial changes, then submit a PR.


## License

[MIT](./LICENSE)
