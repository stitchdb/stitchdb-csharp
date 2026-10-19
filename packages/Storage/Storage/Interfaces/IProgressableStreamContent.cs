using System;

namespace StitchDB.Storage.Interfaces
{
    internal interface IProgressableStreamContent
    {
        IProgress<float>? Progress { get; }
    }
}