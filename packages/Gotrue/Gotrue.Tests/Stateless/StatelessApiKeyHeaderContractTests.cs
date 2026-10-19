#region

using System.Threading.Tasks;
using Gotrue.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StitchDB.Gotrue;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using static StitchDB.Gotrue.StatelessClient;

#endregion

namespace Gotrue.Tests.Stateless;

/// <summary>
///     Pins the apikey header contract for the stateless client: like the standalone admin client it is built
///     without the meta client's header wiring, so an authed request must still carry an <c>apikey</c> header
///     for new-format keys to be accepted.
/// </summary>
[TestClass]
[TestCategory("Contract")]
public class StatelessApiKeyHeaderContractTests
{
    private const string ServiceKey = "sdb_secret_service_role_key";
    private const string UserId = "user-123";

    private MockGotrueServer server = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        this.server = new MockGotrueServer();
        this.server.Given(Request.Create().WithPath($"/admin/users/{UserId}").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody("{}"));
    }

    [TestCleanup]
    public void TestCleanup() => this.server.Dispose();

    [TestMethod]
    public async Task DeleteUser_ShouldSendApiKeyHeader_GivenNoApiKeyConfigured()
    {
        var options = new StatelessClientOptions { Url = this.server.Url };
        await new StatelessClient().DeleteUser(UserId, ServiceKey, options);
        this.server.VerifySingleReceivedRequest()
            .WithHeader("Authorization", $"Bearer {ServiceKey}")
            .WithHeader("apikey", ServiceKey);
    }
}
