using System;
namespace StitchDB.Storage
{
    /// <summary>
    /// Represents a Generated Upload Signed Url - can be used to upload a file without needing a logged in token or user.
    /// </summary>
    public class UploadSignedUrl
    {
        /// <summary>
        /// The Full Signed Url
        /// </summary>
        public Uri SignedUrl { get; }

        /// <summary>
        /// The generated token
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// The Key that can be uploaded to (the stitchdb filename)
        /// </summary>
        public string Key { get; }

        public UploadSignedUrl(Uri signedUrl, string token, string key)
        {
            SignedUrl = signedUrl;
            Token = token;
            Key = key;
        }
    }
}

