namespace OrderProcessor.Domain;

public record DiscountResult(decimal DiscountAmount, decimal FinalTotal, decimal ShippingCost);

public class DiscountCalculator
{
    public DiscountResult Calculate(decimal orderTotal, int itemCount,
        bool isHolidayPeriod, bool isLoyaltyCustomer, decimal shippingCost)
    {
        var rate = 0m;
        if (itemCount > 10)    rate += 0.05m;
        if (isHolidayPeriod)   rate += 0.10m;
        if (isLoyaltyCustomer) rate += 0.08m;

        var discountAmount = Math.Round(orderTotal * rate, 2);
        var finalTotal = Math.Round(orderTotal - discountAmount, 2);
        var effectiveShipping = finalTotal > 200m ? 0m : shippingCost;

        return new(discountAmount, finalTotal, effectiveShipping);
    }
}

