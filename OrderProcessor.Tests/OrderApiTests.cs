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

    // --- Helpers ---

    private async Task<string> CreateOrderIdAsync()
    {
        var resp = await _client.PostAsync("/api/orders", null);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateConfirmedOrderIdAsync()
    {
        var id = await CreateOrderIdAsync();
        await _client.PostAsync($"/api/orders/{id}/confirm", null);
        return id;
    }

    private async Task<string> CreateShippedOrderIdAsync()
    {
        var id = await CreateConfirmedOrderIdAsync();
        await _client.PostAsJsonAsync($"/api/orders/{id}/ship", new { trackingNumber = "TRACK-001" });
        return id;
    }

    // --- Tests ---

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
        var orderId = await CreateOrderIdAsync();

        var product = new
        {
            name = "Glass Vase", color = "Red", size = "Medium",
            price = 25.00m, discount = 0.1m, material = "Glass",
            weightKg = 0.5, fragile = true, containsLiquids = false,
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
        var orderId = await CreateOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostConfirm_OnAlreadyConfirmedOrder_ReturnsConflict()
    {
        var orderId = await CreateConfirmedOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task PostShip_OnConfirmedOrder_ReturnsOkWithShippedStatus()
    {
        var orderId = await CreateConfirmedOrderIdAsync();

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/ship",
            new { trackingNumber = "TRACK-001" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Shipped", body.GetProperty("status").GetString());
        Assert.Equal("TRACK-001", body.GetProperty("trackingNumber").GetString());
    }

    [Fact]
    public async Task PostShip_OnPendingOrder_ReturnsConflict()
    {
        var orderId = await CreateOrderIdAsync();

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/ship",
            new { trackingNumber = "TRACK-001" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task PostDeliver_OnShippedOrder_ReturnsOkWithDeliveredStatus()
    {
        var orderId = await CreateShippedOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/deliver", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Delivered", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostDeliver_OnConfirmedOrder_ReturnsConflict()
    {
        var orderId = await CreateConfirmedOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/deliver", null);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // --- Cancel ---

    [Fact]
    public async Task PostCancel_OnPendingOrder_ReturnsOkWithCancelledStatus()
    {
        var orderId = await CreateOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostCancel_OnConfirmedOrder_ReturnsOkWithCancelledStatus()
    {
        var orderId = await CreateConfirmedOrderIdAsync();

        var resp = await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostCancel_OnDeliveredOrder_ReturnsConflict()
    {
        var orderId = await CreateShippedOrderIdAsync();
        await _client.PostAsync($"/api/orders/{orderId}/deliver", null);

        var resp = await _client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // --- Shipping ---

    [Fact]
    public async Task PostShipping_Ground_Domestic_ReturnsOrderWithShippingCost()
    {
        var orderId = await CreateOrderIdAsync();

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/shipping",
            new { method = "Ground", destination = "Domestic" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("shippingCost").GetDecimal() >= 0m);
        Assert.Equal("Ground", body.GetProperty("shippingMethod").GetString());
        Assert.Equal("Domestic", body.GetProperty("destination").GetString());
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
