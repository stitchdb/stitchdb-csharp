# StitchDB.Core

[![Build and Test](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml/badge.svg)](https://github.com/stitchdb/stitchdb-csharp/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/StitchDB.Core)](https://www.nuget.org/packages/StitchDB.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Shared primitives for the [StitchDB C# SDK](https://github.com/stitchdb/stitchdb-csharp) —
common helpers, extensions, attributes, and diagnostics used across the `StitchDB.*` packages.

This package is an internal building block. You normally get it transitively by installing
[`StitchDB`](../StitchDB/README.md) or any individual service package (Auth, Postgrest, Storage,
Realtime, Functions), and rarely reference it directly.

## Installation

```sh
dotnet add package StitchDB.Core
```

Targets .NET Standard 2.0.

## Contributing

Contributions are welcome. See the [repository root](https://github.com/stitchdb/stitchdb-csharp)
for how to build and test the SDK.

## License

[MIT](../../LICENSE)
