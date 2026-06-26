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
        var id = body.GetProperty("id").GetString()!;
        Assert.Contains($"/api/orders/{id}", response.Headers.Location!.ToString());
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

    // --- Clear ---

    [Fact]
    public async Task DeleteOrders_ClearsAllOrders()
    {
        await _client.PostAsync("/api/orders", null);

        var deleteResp = await _client.DeleteAsync("/api/orders");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
        var getResp = await _client.GetAsync("/api/orders");
        var body = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task PostDiscount_WithShippingSet_PreservesShippingCostWhenTotalUnder200()
    {
        var orderId = await CreateOrderIdAsync();
        var product = new
        {
            name = "Widget", color = "Red", size = "Small", price = 50.0, discount = 0.0,
            material = "Glass", weightKg = 0.5, fragile = false, containsLiquids = false,
            packaging = "Boxed", dimensions = new { lengthCm = 5.0, widthCm = 5.0, heightCm = 5.0 }
        };
        await _client.PostAsJsonAsync($"/api/orders/{orderId}/products", product);
        await _client.PostAsJsonAsync($"/api/orders/{orderId}/shipping",
            new { method = "Ground", destination = "Domestic" });

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/discount",
            new { isHolidayPeriod = false, isLoyaltyCustomer = false });

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        // total is 50, not > 200, so effectiveShippingCost should equal actual shipping cost (non-zero)
        Assert.True(body.GetProperty("effectiveShippingCost").GetDecimal() > 0m);
    }

    // --- Discounts ---

    [Fact]
    public async Task PostConfirm_NonExistentOrder_Returns404()
    {
        var resp = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task PostDiscount_WithNoShippingSet_DefaultsShippingCostToZero()
    {
        var orderId = await CreateOrderIdAsync();
        // No shipping set — ShippingCost is null, should default to 0

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/discount",
            new { isHolidayPeriod = false, isLoyaltyCustomer = false });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0m, body.GetProperty("effectiveShippingCost").GetDecimal());
    }

    [Fact]
    public async Task PostDiscount_BulkItems_ReturnsFinalTotalWithDiscount()
    {
        var orderId = await CreateOrderIdAsync();
        // Add 11 products
        var product = new
        {
            name = "Widget", color = "Red", size = "Small", price = 10.0, discount = 0.0,
            material = "Glass", weightKg = 0.1, fragile = false, containsLiquids = false,
            packaging = "Boxed", dimensions = new { lengthCm = 5.0, widthCm = 5.0, heightCm = 5.0 }
        };
        for (int i = 0; i < 11; i++)
            await _client.PostAsJsonAsync($"/api/orders/{orderId}/products", product);

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/discount",
            new { isHolidayPeriod = false, isLoyaltyCustomer = false });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("discountAmount").GetDecimal() > 0m);
        Assert.True(body.GetProperty("finalTotal").GetDecimal() < body.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task PostDiscount_FreeShipping_WhenFinalTotalExceeds200()
    {
        var orderId = await CreateOrderIdAsync();
        var product = new
        {
            name = "Expensive", color = "Red", size = "Large", price = 250.0, discount = 0.0,
            material = "Metal", weightKg = 1.0, fragile = false, containsLiquids = false,
            packaging = "Boxed", dimensions = new { lengthCm = 10.0, widthCm = 10.0, heightCm = 10.0 }
        };
        await _client.PostAsJsonAsync($"/api/orders/{orderId}/products", product);
        await _client.PostAsJsonAsync($"/api/orders/{orderId}/shipping",
            new { method = "Ground", destination = "Domestic" });

        var resp = await _client.PostAsJsonAsync($"/api/orders/{orderId}/discount",
            new { isHolidayPeriod = false, isLoyaltyCustomer = false });

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0m, body.GetProperty("effectiveShippingCost").GetDecimal());
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
