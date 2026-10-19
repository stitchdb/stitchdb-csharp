using System;
using System.Net.Http;

namespace StitchDB.Storage.Exceptions;

public class StitchDBStorageException : Exception
{
    public StitchDBStorageException(string? message) : base(message) { }
    public StitchDBStorageException(string? message, Exception? innerException) : base(message, innerException) { }

    public HttpResponseMessage? Response { get; internal set; }

    public string? Content { get; internal set; }

    public int StatusCode { get; internal set; }

    /// <summary>
    /// Gets the error code returned by the Storage service, if available.
    /// </summary>
    public string? Code { get; internal set; }

    public FailureHint.Reason Reason { get; private set; }

    public void AddReason() => this.Reason = FailureHint.DetectReason(this);
}
