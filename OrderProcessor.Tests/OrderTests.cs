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
        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }
}
