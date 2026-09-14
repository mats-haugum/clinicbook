using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Health;

// Deliberately failing test to verify a red CI run does not deploy.
// Reverted immediately after the check.
public class DeployGateCanaryTests
{
    [Fact]
    public void DeployGate_FailsOnPurpose()
    {
        true.Should().BeFalse("this commit must not be deployed");
    }
}
