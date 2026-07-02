using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using OrderProcessing.Contracts.Orders;
using OrderProcessing.Domain.Orders;
using OrderProcessing.Shared.Pagination;
using OrderProcessing.Shared.Responses;

namespace Integration.Tests;

public sealed class OrderApiIntegrationTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient client = CreateAuthenticatedClient(factory);

    [Fact]
    public async Task Health_endpoint_returns_success()
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_order_then_get_by_id_returns_order_details()
    {
        var createRequest = CreateOrderRequest();

        var createResponse = await client.PostAsJsonAsync("/orders", createRequest, JsonOptions);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        created.Should().NotBeNull();
        created!.Success.Should().BeTrue();
        created.Data.Should().NotBeNull();
        created.Data!.Status.Should().Be(OrderStatus.Pending);
        created.Data.Items.Should().HaveCount(2);
        created.Data.TotalAmount.Should().Be(249.97m);

        var getResponse = await client.GetAsync($"/orders/{created.Data.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        fetched.Should().NotBeNull();
        fetched!.Data.Should().NotBeNull();
        fetched.Data!.Id.Should().Be(created.Data.Id);
        fetched.Data.StatusHistory.Should().ContainSingle(x => x.ToStatus == OrderStatus.Pending);
    }

    [Fact]
    public async Task List_orders_can_filter_by_status()
    {
        var createResponse = await client.PostAsJsonAsync("/orders", CreateOrderRequest(), JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        created!.Data.Should().NotBeNull();

        var listResponse = await client.GetAsync("/orders?status=Pending&pageSize=100");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<OrderResponse>>>(JsonOptions);
        list.Should().NotBeNull();
        list!.Success.Should().BeTrue();
        list.Data.Should().NotBeNull();
        list.Data!.Items.Should().Contain(x => x.Id == created.Data!.Id && x.Status == OrderStatus.Pending);
    }

    [Fact]
    public async Task Update_status_moves_order_to_processing()
    {
        var createResponse = await client.PostAsJsonAsync("/orders", CreateOrderRequest(), JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);

        var updateResponse = await client.PatchAsJsonAsync(
            $"/orders/{created!.Data!.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Processing, "Integration test processing."),
            JsonOptions);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.Data.Should().NotBeNull();
        updated.Data!.Status.Should().Be(OrderStatus.Processing);
        updated.Data.StatusHistory.Should().Contain(x => x.FromStatus == OrderStatus.Pending && x.ToStatus == OrderStatus.Processing);
    }

    [Fact]
    public async Task Cancel_pending_order_returns_no_content_and_marks_order_cancelled()
    {
        var createResponse = await client.PostAsJsonAsync("/orders", CreateOrderRequest(), JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/orders/{created!.Data!.Id}")
        {
            Content = JsonContent.Create(new CancelOrderRequest("Customer requested cancellation."), options: JsonOptions)
        };

        var cancelResponse = await client.SendAsync(cancelRequest);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync($"/orders/{created.Data.Id}");
        var cancelled = await getResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        cancelled!.Data.Should().NotBeNull();
        cancelled.Data!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_processing_order_returns_bad_request()
    {
        var createResponse = await client.PostAsJsonAsync("/orders", CreateOrderRequest(), JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>(JsonOptions);
        await client.PatchAsJsonAsync(
            $"/orders/{created!.Data!.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Processing, "Integration test processing."),
            JsonOptions);

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/orders/{created.Data.Id}")
        {
            Content = JsonContent.Create(new CancelOrderRequest("Too late to cancel."), options: JsonOptions)
        };

        var cancelResponse = await client.SendAsync(cancelRequest);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var failure = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
        failure.Should().NotBeNull();
        failure!.Success.Should().BeFalse();
        failure.Errors.Should().Contain("Only pending orders can be cancelled.");
    }

    private static CreateOrderRequest CreateOrderRequest()
        => new(
            Guid.NewGuid(),
            new AddressDto("100 Market Street", null, "San Francisco", "CA", "94105", "US"),
            [
                new CreateOrderItemRequest(Guid.NewGuid(), $"SKU-LAPTOP-{Guid.NewGuid():N}"[..16], "Enterprise Laptop", 1, 199.99m, "USD"),
                new CreateOrderItemRequest(Guid.NewGuid(), $"SKU-DOCK-{Guid.NewGuid():N}"[..14], "USB-C Dock", 2, 24.99m, "USD")
            ]);

    private static HttpClient CreateAuthenticatedClient(OrderApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAdminToken(factory));
        return client;
    }

    private static string CreateAdminToken(OrderApiFactory factory)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(factory.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: factory.JwtIssuer,
            audience: factory.JwtAudience,
            claims:
            [
                new Claim(ClaimTypes.Name, "integration-test-admin"),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
