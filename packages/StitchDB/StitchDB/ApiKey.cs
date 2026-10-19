using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace StitchDB;

/// <summary>
/// Helpers for reasoning about the format of a StitchDB API key.
///
/// StitchDB projects use JWT API keys: <c>anon</c> (client-side) and <c>service_role</c> (server-side).
/// Opaque, non-JWT keys (<c>sdb_publishable_…</c> / <c>sdb_secret_…</c>) are recognized for forward
/// compatibility: they are sent on the <c>apikey</c> header only, never as <c>Authorization: Bearer</c>.
/// </summary>
internal static class ApiKey
{
    private static readonly Regex SubtypePattern = new(@"^sdb_[a-zA-Z0-9]+_", RegexOptions.Compiled);

    /// <summary>
    /// Subtypes we've already warned about, so <see cref="CheckFormat"/> stays quiet after the
    /// first occurrence (mirrors stitchdb-js <c>checkApiKeyFormat</c>).
    /// </summary>
    private static readonly HashSet<string> WarnedSubtypes = new();

    /// <summary>
    /// True when the key is a new-format opaque key (publishable or secret). These are not JWTs
    /// and must never be sent as an <c>Authorization: Bearer</c> token.
    /// </summary>
    public static bool IsNewApiKey(string? key) =>
        key != null && (key.StartsWith("sdb_publishable_") || key.StartsWith("sdb_secret_"));

    /// <summary>
    /// Warns (once per unrecognized subtype) when the key uses an <c>sdb_…_</c> prefix the SDK
    /// does not recognize. Legacy JWT keys (no <c>sdb_</c> prefix), the recognized new-format keys,
    /// and temporary <c>sdb_temp_</c> keys are all considered fine and produce no warning.
    /// </summary>
    public static void CheckFormat(string? key)
    {
        if (key == null || !key.StartsWith("sdb_") || IsNewApiKey(key) || key.StartsWith("sdb_temp_"))
            return;

        var subtype = SubtypePattern.Match(key) is { Success: true } match ? match.Value : "unknown";

        lock (WarnedSubtypes)
        {
            if (!WarnedSubtypes.Add(subtype))
                return;
        }

        Trace.TraceWarning(
            "StitchDB: Unrecognized API key format '{0}'. Use the project's anon key for " +
            "client-side and the service_role key for server-side contexts (Settings → API keys).", subtype);
    }
}
