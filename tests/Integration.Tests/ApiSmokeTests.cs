using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration.Tests;

public sealed class ApiSmokeTests
{
    [Fact(Skip = "Requires dockerized PostgreSQL/Redis and a valid JWT fixture.")]
    public async Task Health_endpoint_returns_success()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
