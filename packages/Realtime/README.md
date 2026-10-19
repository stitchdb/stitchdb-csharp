# StitchDB.Realtime

[![Build and Test](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Realtime)](https://www.nuget.org/packages/StitchDB.Realtime/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

A C# client for [StitchDB Realtime](https://stitchdb.com/docs/realtime) — listen to Postgres
changes, and use Broadcast and Presence over a single websocket connection.

Part of the [StitchDB C# SDK](https://github.com/stitchdb/stitchdb-csharp). Most projects
use it through the [`StitchDB`](../StitchDB/README.md) meta-package (`stitchdb.Realtime`); reference
this package directly to use Realtime on its own.

## Installation

```sh
dotnet add package StitchDB.Realtime
```

Targets .NET Standard 2.1.

## Getting started

`ConnectAsync()` and `Subscribe()` are awaitable, so you can be sure a connection exists before
interacting with it. On the initial connection the client is **fail-fast** — `ConnectAsync()` throws a
`RealtimeException` if the socket server is unreachable. Once connected, it reconnects indefinitely
until disconnected.

```csharp
using StitchDB.Realtime;
using StitchDB.Realtime.PostgresChanges;
using static StitchDB.Realtime.PostgresChanges.PostgresChangesOptions;

var client = new Client("ws://realtime-dev.localhost:4000/socket");
await client.ConnectAsync();

var channel = client.Channel("todos");

channel.OnPostgresChange((_, change) =>
{
    var updated = change.Model<Todo>();
    var previous = change.OldModel<Todo>();
}, ListenType.Updates, new PostgresChangesFilter { Table = "todos" });

await channel.Subscribe();
```

`Todo` derives from `StitchDB.Postgrest.Models.BaseModel`, letting the client coerce change payloads
into your model via `change.Model<T>()`.

Full generated API reference:
[StitchDB.Realtime](https://stitchdb.com/docs/sdk/csharp).

## Postgres changes

Listen to inserts, updates, and deletes on a table, delivered to authorized clients according to your
[Row Level Security](https://stitchdb.com/docs/database/row-level-security) policies. The table
must belong to the `stitchdb_realtime` publication.
[More on Postgres changes](https://stitchdb.com/docs/realtime).

```csharp
var channel = client.Channel("public-users");

channel.OnPostgresChange((_, change) =>
{
    switch (change.Payload?.Data?.Type)
    {
        case EventType.Insert: /* row created */ break;
        case EventType.Update: /* row updated */ break;
        case EventType.Delete: /* row deleted */ break;
    }
}, ListenType.All, new PostgresChangesFilter { Table = "users" });

await channel.Subscribe();
```

## Broadcast

Publish-subscribe messaging: a client sends messages to a named channel, and any other client
subscribed to that channel receives them in real time — useful for ephemeral, high-frequency data
like cursor positions. [More on Broadcast](https://stitchdb.com/docs/realtime).

Given a typed broadcast model:

```csharp
class MouseBroadcast : BaseBroadcast<MouseStatus> { }

class MouseStatus
{
    [JsonPropertyName("mouseX")] public float MouseX { get; set; }
    [JsonPropertyName("mouseY")] public float MouseY { get; set; }
    [JsonPropertyName("userId")] public string UserId { get; set; }
}
```

**Receive** typed broadcast events:

```csharp
var channel = client.Channel("cursor");
var broadcast = channel.Register<MouseBroadcast>(broadcastSelf: true);

broadcast.AddBroadcastEventHandler((_, _) =>
{
    var state = broadcast.Current();
    Debug.WriteLine($"{state.Payload.MouseX}:{state.Payload.MouseY}");
});

await channel.Subscribe();
```

**Send** a broadcast event on the same `broadcast` handle:

```csharp
await broadcast.Send("cursor", new MouseBroadcast
{
    Payload = new MouseStatus { MouseX = 123, MouseY = 456 }
});
```

## Presence

Presence tracks shared state across clients using a CRDT: each client publishes its own state, and
all subscribers converge on the same view. When a client disconnects, its state is removed
automatically — which makes "who's online" features straightforward.
[More on Presence](https://stitchdb.com/docs/realtime).

Given a typed presence model:

```csharp
class UserPresence : BasePresence
{
    [JsonPropertyName("lastSeen")] public DateTime LastSeen { get; set; }
}
```

**Receive** presence sync events:

```csharp
var presenceId = Guid.NewGuid().ToString();
var channel = client.Channel("last-seen");
var presence = channel.Register<UserPresence>(presenceId);

presence.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (_, _) =>
{
    foreach (var state in presence.CurrentState)
    {
        var userId = state.Key;
        var lastSeen = state.Value.First().LastSeen;
        Debug.WriteLine($"{userId}: {lastSeen}");
    }
});

await channel.Subscribe();
```

**Track** this client's presence:

```csharp
presence.Track(new UserPresence { LastSeen = DateTime.Now });
```

## Events and logging

Event handlers are delegates, scoped to the object they concern — socket handlers receive
connectivity events, channel handlers receive join/leave events, and so on. Register and remove them
with the `Add`/`Remove`/`Clear` methods (e.g. `RealtimeSocket.AddStateChangedHandler`,
`RealtimeBroadcast.AddBroadcastEventHandler`).

For logging, attach a debug handler rather than relying on console output:

```csharp
client.AddDebugHandler((sender, message, exception) => Debug.WriteLine(message));
```

> **Observability:** unlike the HTTP-based StitchDB clients, Realtime is **not yet** instrumented for
> OpenTelemetry, so no websocket traces or metrics are emitted.

## Contributing

Contributions are welcome. See the [repository root](https://github.com/stitchdb/stitchdb-csharp)
for how to build and test the SDK.

## License

[MIT](../../LICENSE)
