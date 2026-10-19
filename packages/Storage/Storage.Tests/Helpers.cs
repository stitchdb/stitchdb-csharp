using System;
using System.Collections.Generic;

namespace Storage.Tests;

public static class Helpers
{
    public static string StitchDBUrl => "http://127.0.0.1:54321/storage/v1";
    public static string PublicKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdGl0Y2hkYi1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.30k-T-w6MlWAwrHo1aRk8_7MaPM-m08Yj65j8D3qnmo";
    public static string ServiceKey => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";

    public static string StorageUrl => $"{StitchDBUrl}";

    public static StitchDB.Storage.Client GetServiceClient()
    {
        return new StitchDB.Storage.Client(StorageUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {ServiceKey}" },
        });
    }

    public static StitchDB.Storage.Client GetPublicClient()
    {
        return new StitchDB.Storage.Client(StorageUrl, new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {PublicKey}" },
        });
    }
}
