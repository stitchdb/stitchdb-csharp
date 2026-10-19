#region

using System.Threading.Tasks;
using FluentAssertions;
using Gotrue.Tests.Support;
using StitchDB.Gotrue;
using StitchDB.Gotrue.Interfaces;
using StitchDB.Gotrue.Mfa;

#endregion

namespace Gotrue.Tests.Mfa;

/// <summary>
///     Base fixture for the MFA E2E tests: reuses the stateful client from <see cref="AuthClientFixture" /> and
///     adds a service-role admin client plus TOTP enrollment / code helpers so each scenario reads as intent.
/// </summary>
public abstract class MfaFixture : AuthClientFixture
{
    private IGotrueAdminClient<User>? admin;
    protected IGotrueAdminClient<User> Admin => admin ??= TestClients.AdminAgainstCliStack();

    protected async Task<MfaEnrollResponse> EnrollTotp()
    {
        var enrollment = await this.Client.Enroll(new MfaEnrollParams { Issuer = "StitchDB", FactorType = "totp", FriendlyName = "Enroll test" });
        enrollment.Should().NotBeNull();
        return enrollment!;
    }

    protected static string TotpCode(MfaEnrollResponse enrollment) =>
        TotpGenerator.GeneratePin(enrollment.Totp!.Secret, 30, 6);
}
