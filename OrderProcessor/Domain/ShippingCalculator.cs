namespace OrderProcessor.Domain;

public class ShippingCalculator
{
    private static readonly Dictionary<ShippingMethod, decimal> BaseRates = new()
    {
        [ShippingMethod.Ground]    = 5m,
        [ShippingMethod.Express]   = 15m,
        [ShippingMethod.Overnight] = 30m,
    };

    public decimal Calculate(IEnumerable<Product> products, ShippingMethod method, Destination destination)
    {
        var baseRate = BaseRates[method];
        var total = products.Sum(p => baseRate + p.ShippingSurcharge);
        var multiplier = destination == Destination.International ? 1.5m : 1.0m;
        return Math.Round(total * multiplier, 2);
    }
}
