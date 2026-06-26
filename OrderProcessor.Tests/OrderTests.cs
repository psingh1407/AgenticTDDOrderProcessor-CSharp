using OrderProcessor.Domain;

namespace OrderProcessor.Tests;

public class OrderTests
{
    [Fact]
    public void EmptyOrder_HasTotalOfZero()
    {
        var order = new Order();
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void OrderWithOneProduct_CalculatesTotalAfterDiscount()
    {
        var order = new Order();
        order.Products.Add(new Product { Price = 100m, Discount = 0.1m });
        Assert.Equal(90.00m, order.Total);
    }

    [Fact]
    public void OrderWithMultipleProducts_SumsAllDiscountedPrices()
    {
        var order = new Order();
        order.Products.Add(new Product { Price = 100m, Discount = 0.1m }); // 90.00
        order.Products.Add(new Product { Price = 50m,  Discount = 0.2m }); // 40.00
        Assert.Equal(130.00m, order.Total);
    }

    [Fact]
    public void OrderTotal_RoundedToTwoDecimalPlaces()
    {
        var order = new Order();
        order.Products.Add(new Product { Price = 10m, Discount = 0.333m }); // 6.67
        Assert.Equal(6.67m, order.Total);
    }

    [Fact]
    public void Confirm_ChangesOrderStatusToConfirmed()
    {
        var order = new Order();
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_Throws()
    {
        var order = new Order();
        order.Confirm();
        var ex = Assert.Throws<InvalidOperationException>(() => order.Confirm());
        Assert.Contains("Cannot confirm", ex.Message);
    }

    // --- Ship ---

    [Fact]
    public void Ship_ConfirmedOrder_ChangesStatusToShipped()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Ship_ConfirmedOrder_AssignsTrackingNumber()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        Assert.Equal("TRACK-001", order.TrackingNumber);
    }

    [Fact]
    public void Ship_PendingOrder_Throws()
    {
        var order = new Order();
        var ex = Assert.Throws<InvalidOperationException>(() => order.Ship("TRACK-001"));
        Assert.Contains("Cannot ship", ex.Message);
    }

    [Fact]
    public void Ship_ShippedOrder_Throws()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        Assert.Throws<InvalidOperationException>(() => order.Ship("TRACK-002"));
    }

    // --- Deliver ---

    [Fact]
    public void Deliver_ShippedOrder_ChangesStatusToDelivered()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        order.Deliver();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Deliver_PendingOrder_Throws()
    {
        var order = new Order();
        var ex = Assert.Throws<InvalidOperationException>(() => order.Deliver());
        Assert.Contains("Cannot deliver", ex.Message);
    }

    [Fact]
    public void Deliver_ConfirmedOrder_Throws()
    {
        var order = new Order();
        order.Confirm();
        Assert.Throws<InvalidOperationException>(() => order.Deliver());
    }

    // --- Cancel ---

    [Fact]
    public void Cancel_PendingOrder_ChangesStatusToCancelled()
    {
        var order = new Order();
        order.Cancel();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ConfirmedOrder_ChangesStatusToCancelled()
    {
        var order = new Order();
        order.Confirm();
        order.Cancel();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShippedOrder_ChangesStatusToCancelled()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        order.Cancel();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_DeliveredOrder_Throws()
    {
        var order = new Order();
        order.Confirm();
        order.Ship("TRACK-001");
        order.Deliver();
        var ex = Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Contains("Cannot cancel", ex.Message);
    }
}
