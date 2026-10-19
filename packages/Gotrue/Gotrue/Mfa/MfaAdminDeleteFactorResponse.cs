using System.Text.Json.Serialization;

namespace StitchDB.Gotrue.Mfa
{
	public class MfaAdminDeleteFactorResponse
	{
		// Id of the factor that was successfully deleted
		[JsonPropertyName("id")]
		public string Id { get; set; }
	}
}