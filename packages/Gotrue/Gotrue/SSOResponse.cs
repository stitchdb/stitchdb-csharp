using System;

namespace StitchDB.Gotrue
{
	/// <summary>
	/// Single sign on (SSO) response data deserialized from the API {stitchdbAuthUrl}/sso
	/// </summary>
	public class SSOResponse : ProviderAuthState
	{
		/// <summary>
		/// Deserialized response from {stitchdbAuthUrl}/sso
		/// </summary>
		/// <param name="uri">Uri from the response, this will open the SSO providers login page and allow a user to login to their provider</param>
		public SSOResponse(Uri uri) : base(uri) { }
	}
}
