using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessor.Persistence;

namespace OrderProcessor.Tests;

public class OrderApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _tempFile;

    public OrderApiTests()
    {
        _tempFile = Path.GetTempFileName();
        File.WriteAllText(_tempFile, "[]");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IOrderRepository));
                if (existing != null) services.Remove(existing);
                services.AddSingleton<IOrderRepository>(new JsonOrderRepository(_tempFile));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostOrders_ReturnsCreatedWithId()
    {
        var response = await _client.PostAsync("/api/orders", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _));
        Assert.Equal(0m, body.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task GetOrders_ReturnsOrdersArray()
    {
        await _client.PostAsync("/api/orders", null);

        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(1, body.GetArrayLength());
    }

    [Fact]
    public async Task PostProductToOrder_ReturnsOrderWithUpdatedTotal()
    {
        var createResp = await _client.PostAsync("/api/orders", null);
        var order = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetString();

        var product = new
        {
            name = "Glass Vase",
            color = "Red",
            size = "Medium",
            price = 25.00m,
            discount = 0.1m,
            material = "Glass",
            weightKg = 0.5,
            fragile = true,
            containsLiquids = false,
            packaging = "Boxed",
            dimensions = new { lengthCm = 10.0, widthCm = 10.0, heightCm = 20.0 }
        };
        var addResp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/products", product);

        Assert.Equal(HttpStatusCode.Created, addResp.StatusCode);
        var updated = await addResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(22.50m, updated.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task PostConfirm_OnPendingOrder_ReturnsOkWithConfirmedStatus()
    {
        var createResp = await _client.PostAsync("/api/orders", null);
        var order = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetString();

        var confirmResp = await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
        var confirmed = await confirmResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", confirmed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostConfirm_OnAlreadyConfirmedOrder_ReturnsConflict()
    {
        var createResp = await _client.PostAsync("/api/orders", null);
        var order = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = order.GetProperty("id").GetString();
        await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        var secondConfirm = await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, secondConfirm.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
