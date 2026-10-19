using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StitchDB.Core.Http;
using static StitchDB.Gotrue.Constants;
using static StitchDB.StatelessClient;

namespace StitchDB.Tests;

/// <summary>
/// The stateless facade owns the same composition concern as <see cref="StitchDB.Client"/> — building
/// child-client options (headers, schema, urls) from a url/key — but expressed as pure static helpers.
/// All hermetic; the live stateless round-trip lives in <see cref="StitchDBClientDatabaseTests"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class StatelessClientTests
{
    private const string StitchDBUrl = "http://localhost";

    private static StitchDB.StitchDBOptions FormatOptions() => new()
    {
        AuthUrlFormat = "{0}:54321/rest/v1",
        RealtimeUrlFormat = "ws://127.0.0.1:54321/realtime/v1",
        RestUrlFormat = "{0}:54321/rest/v1",
    };

    [TestMethod]
    public void GetAuthOptions_ShouldProduceUsableGotrueClient()
    {
        var gotrueOptions = GetAuthOptions(StitchDBUrl, null, FormatOptions());
        new StitchDB.Gotrue.Client(gotrueOptions).SignIn(Provider.Spotify).Should().NotBeNull();
    }

    [TestMethod]
    public void GetAuthOptions_ShouldPreferDeveloperAuthorizationHeader_GivenAuthorizationInOptions()
    {
        var options = new StitchDB.StitchDBOptions
        {
            AuthUrlFormat = "{0}:9999",
            RealtimeUrlFormat = "{0}:4000/socket",
            RestUrlFormat = "{0}:3000",
            Headers = new Dictionary<string, string> { { "Authorization", "Bearer 123" } }
        };
        GetAuthOptions(StitchDBUrl, "456", options).Headers.Should().ContainKey("Authorization")
            .WhoseValue.Should().Be("Bearer 123",
                "an explicit Authorization header must win over the key-derived bearer (issue #5)");
    }

    [TestMethod]
    public void GetRestOptions_ShouldComposeSchemaKeyAndAuthHeaders()
    {
        var restOptions = GetRestOptions("my-key", new StitchDB.StitchDBOptions { Schema = "custom" });
        using (new AssertionScope())
        {
            restOptions.Schema.Should().Be("custom");
            restOptions.Headers.Should().ContainKey("apiKey").WhoseValue.Should().Be("my-key");
            restOptions.Headers.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer my-key",
                "with no developer Authorization the bearer falls back to the stitchdb key");
            restOptions.Headers.Should().ContainKey("X-Client-Info");
        }
    }

    [TestMethod]
    public void GetRestOptions_ShouldMergeDeveloperSuppliedHeaders_GivenCustomHeader()
    {
        var options = new StitchDB.StitchDBOptions();
        options.Headers["X-Custom"] = "custom-value";
        GetRestOptions("my-key", options).Headers.Should().ContainKey("X-Custom")
            .WhoseValue.Should().Be("custom-value", "developer headers must flow through to the child clients");
    }

    [TestMethod]
    public void GetRestOptions_ShouldForwardPostgrestRetry_GivenCustomRetryOptions()
    {
        var retry = new RetryOptions { MaxRetries = 2, BaseDelay = TimeSpan.FromMilliseconds(5) };
        var restOptions = GetRestOptions("my-key", new StitchDB.StitchDBOptions { PostgrestRetry = retry });
        restOptions.Retry.Should().BeSameAs(retry, "the retry policy must reach the Postgrest client");
    }

    [TestMethod]
    public void GetRestOptions_ShouldForwardPostgrestTimeout_GivenATimeout() =>
        GetRestOptions("my-key", new StitchDB.StitchDBOptions { PostgrestTimeout = TimeSpan.FromSeconds(5) })
            .Timeout.Should().Be(TimeSpan.FromSeconds(5), "the timeout must reach the Postgrest client");

    [TestMethod]
    public void Functions_ShouldCarryDeveloperHeadersToClient_GivenCustomHeader()
    {
        var options = FormatOptions();
        options.Headers["X-Custom"] = "custom-value";
        StatelessClient.Functions(StitchDBUrl, "my-key", options).GetHeaders!().Should().ContainKey("X-Custom")
            .WhoseValue.Should().Be("custom-value", "developer headers must reach the composed functions client");
    }

    [TestMethod]
    public void Storage_ShouldCarryDeveloperHeadersToClient_GivenCustomHeader()
    {
        var options = FormatOptions();
        options.Headers["X-Custom"] = "custom-value";
        StatelessClient.Storage(StitchDBUrl, "my-key", options).Headers.Should().ContainKey("X-Custom")
            .WhoseValue.Should().Be("custom-value", "developer headers must reach the composed storage client");
    }

    [TestMethod]
    [DataRow("sdb_publishable_abc123")]
    [DataRow("sdb_secret_abc123")]
    public void Functions_ShouldOmitBearer_GivenNewFormatKey(string key)
    {
        var headers = StatelessClient.Functions(StitchDBUrl, key, FormatOptions()).GetHeaders!();
        using (new AssertionScope())
        {
            headers.Should().ContainKey("apiKey").WhoseValue.Should().Be(key);
            headers.Should().NotContainKey("Authorization",
                "an opaque (non-JWT) key must not be sent as a Bearer token to the Edge Functions gateway");
        }
    }

    [TestMethod]
    public void Functions_ShouldSendKeyAsBearer_GivenLegacyKey()
    {
        StatelessClient.Functions(StitchDBUrl, "legacy-jwt-key", FormatOptions()).GetHeaders!()
            .Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer legacy-jwt-key",
                "legacy JWT keys are unchanged on the functions path");
    }

    [TestMethod]
    public void GetRestOptions_ShouldStillSendKeyAsBearer_GivenNewFormatKey()
    {
        GetRestOptions("sdb_publishable_abc123", FormatOptions()).Headers.Should().ContainKey("Authorization")
            .WhoseValue.Should().Be("Bearer sdb_publishable_abc123",
                "only the functions path omits the key-as-bearer; database keeps it (exact-match exception)");
    }
}
