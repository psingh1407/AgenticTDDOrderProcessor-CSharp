namespace OrderProcessor.Domain;

public record DiscountResult(decimal DiscountAmount, decimal FinalTotal, decimal ShippingCost);

public record DiscountContext(bool IsHolidayPeriod, bool IsLoyaltyCustomer);

public class DiscountCalculator
{
    public DiscountResult Calculate(decimal orderTotal, int itemCount,
        DiscountContext context, decimal shippingCost)
    {
        var rate = 0m;
        if (itemCount > 10) rate += 0.05m;
        if (context.IsHolidayPeriod) rate += 0.10m;
        if (context.IsLoyaltyCustomer) rate += 0.08m;

        var discountAmount = Math.Round(orderTotal * rate, 2);
        var finalTotal = Math.Round(orderTotal - discountAmount, 2);
        var effectiveShipping = finalTotal > 200m ? 0m : shippingCost;

        return new(discountAmount, finalTotal, effectiveShipping);
    }
}

