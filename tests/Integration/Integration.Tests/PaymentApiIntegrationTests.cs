using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration.Tests;

public class PaymentApiIntegrationTests : IClassFixture<WebApplicationFactory<YourCompanyBNPL.Payment.API.Program>>
{
    private readonly WebApplicationFactory<YourCompanyBNPL.Payment.API.Program> _factory;

    public PaymentApiIntegrationTests(WebApplicationFactory<YourCompanyBNPL.Payment.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    // TODO: Add comprehensive integration tests
    // - End-to-end payment flow
    // - BNPL application and approval
    // - Database integration
    // - External service integration
    // - Error handling
}