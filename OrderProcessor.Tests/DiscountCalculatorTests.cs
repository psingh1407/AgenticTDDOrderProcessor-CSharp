using OrderProcessor.Domain;

namespace OrderProcessor.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    [Fact]
    public void Calculate_BulkDiscount_MoreThan10Items_Applies5Percent()
    {
        var result = _calc.Calculate(orderTotal: 100m, itemCount: 11,
            context: new DiscountContext(IsHolidayPeriod: false, IsLoyaltyCustomer: false), shippingCost: 0m);

        Assert.Equal(5m, result.DiscountAmount);
        Assert.Equal(95m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_SeasonalDiscount_HolidayPeriod_Applies10Percent()
    {
        var result = _calc.Calculate(orderTotal: 100m, itemCount: 1,
            context: new DiscountContext(IsHolidayPeriod: true, IsLoyaltyCustomer: false), shippingCost: 0m);

        Assert.Equal(10m, result.DiscountAmount);
        Assert.Equal(90m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_LoyaltyDiscount_ReturningCustomer_Applies8Percent()
    {
        var result = _calc.Calculate(orderTotal: 100m, itemCount: 1,
            context: new DiscountContext(IsHolidayPeriod: false, IsLoyaltyCustomer: true), shippingCost: 0m);

        Assert.Equal(8m, result.DiscountAmount);
        Assert.Equal(92m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_AllDiscounts_Stack_Additively()
    {
        // 5% + 10% + 8% = 23%
        var result = _calc.Calculate(orderTotal: 100m, itemCount: 11,
            context: new DiscountContext(IsHolidayPeriod: true, IsLoyaltyCustomer: true), shippingCost: 0m);

        Assert.Equal(23m, result.DiscountAmount);
        Assert.Equal(77m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_FreeShipping_WhenFinalTotalExceeds200()
    {
        var result = _calc.Calculate(orderTotal: 250m, itemCount: 1,
            context: new DiscountContext(IsHolidayPeriod: false, IsLoyaltyCustomer: false), shippingCost: 15m);

        Assert.Equal(0m, result.ShippingCost);
    }

    [Fact]
    public void Calculate_FreeShipping_ChecksDiscountedTotal_Not_OriginalTotal()
    {
        // original = 220, after 10% holiday = 198 → NOT free
        var result = _calc.Calculate(orderTotal: 220m, itemCount: 1,
            context: new DiscountContext(IsHolidayPeriod: true, IsLoyaltyCustomer: false), shippingCost: 15m);

        Assert.Equal(15m, result.ShippingCost);
        Assert.Equal(198m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_RoundsDiscountAndTotalToTwoDecimalPlaces()
    {
        // 8% of 99.99 = 7.9992 → 8.00 discount, 91.99 total
        var result = _calc.Calculate(orderTotal: 99.99m, itemCount: 1,
            context: new DiscountContext(IsHolidayPeriod: false, IsLoyaltyCustomer: true), shippingCost: 0m);

        Assert.Equal(8.00m, result.DiscountAmount);
        Assert.Equal(91.99m, result.FinalTotal);
    }

    [Fact]
    public void Calculate_BulkAndLoyalty_Stack()
    {
        // 5% + 8% = 13%
        var result = _calc.Calculate(orderTotal: 100m, itemCount: 15,
            context: new DiscountContext(IsHolidayPeriod: false, IsLoyaltyCustomer: true), shippingCost: 0m);

        Assert.Equal(13m, result.DiscountAmount);
        Assert.Equal(87m, result.FinalTotal);
    }
}
