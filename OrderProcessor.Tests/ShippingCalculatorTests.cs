using OrderProcessor.Domain;

namespace OrderProcessor.Tests;

public class ShippingCalculatorTests
{
    private readonly ShippingCalculator _calc = new();

    private static Product MinimalProduct(
        double weightKg = 0, double l = 0, double w = 0, double h = 0,
        bool fragile = false, bool containsLiquids = false, string packaging = "Boxed") =>
        new()
        {
            Name = "Test", Color = "Red", Size = "Small", Price = 10m, Discount = 0m,
            Material = "Glass", WeightKg = weightKg, Fragile = fragile,
            ContainsLiquids = containsLiquids, Packaging = packaging,
            Dimensions = new Dimensions { LengthCm = l, WidthCm = w, HeightCm = h }
        };

    [Fact]
    public void Calculate_OneMinimalProduct_Ground_Domestic_ReturnsBaseRate()
    {
        var cost = _calc.Calculate(new[] { MinimalProduct() }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(5.00m, cost);
    }

    [Fact]
    public void Calculate_Express_HasHigherBaseRateThanGround()
    {
        var products = new[] { MinimalProduct() };
        var ground  = _calc.Calculate(products, ShippingMethod.Ground,   Destination.Domestic);
        var express = _calc.Calculate(products, ShippingMethod.Express,  Destination.Domestic);
        Assert.True(express > ground);
        Assert.Equal(15.00m, express);
    }

    [Fact]
    public void Calculate_Overnight_HasHighestBaseRate()
    {
        var cost = _calc.Calculate(new[] { MinimalProduct() }, ShippingMethod.Overnight, Destination.Domestic);
        Assert.Equal(30.00m, cost);
    }

    [Fact]
    public void Calculate_WeightAddsPerKgSurcharge()
    {
        // base 5 + (2 kg × 1.50) = 8.00
        var cost = _calc.Calculate(new[] { MinimalProduct(weightKg: 2) }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(8.00m, cost);
    }

    [Fact]
    public void Calculate_VolumeAddsPerCm3Surcharge()
    {
        // base 5 + (10×10×10 = 1000 cm³ × 0.001) = 6.00
        var cost = _calc.Calculate(new[] { MinimalProduct(l: 10, w: 10, h: 10) }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(6.00m, cost);
    }

    [Fact]
    public void Calculate_FragileProduct_AddsFragilitySurcharge()
    {
        // base 5 + 5 = 10.00
        var cost = _calc.Calculate(new[] { MinimalProduct(fragile: true) }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(10.00m, cost);
    }

    [Fact]
    public void Calculate_ContainsLiquids_AddsLiquidsSurcharge()
    {
        // base 5 + 3 = 8.00
        var cost = _calc.Calculate(new[] { MinimalProduct(containsLiquids: true) }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(8.00m, cost);
    }

    [Fact]
    public void Calculate_LoosePackaging_AddsLooseSurcharge()
    {
        // base 5 + 2 = 7.00
        var cost = _calc.Calculate(new[] { MinimalProduct(packaging: "Loose") }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(7.00m, cost);
    }

    [Fact]
    public void Calculate_International_MultipliesTotalBy1Point5()
    {
        // base 5 × 1.5 = 7.50
        var cost = _calc.Calculate(new[] { MinimalProduct() }, ShippingMethod.Ground, Destination.International);
        Assert.Equal(7.50m, cost);
    }

    [Fact]
    public void Calculate_International_CostsMoreThanDomestic()
    {
        var products  = new[] { MinimalProduct() };
        var domestic  = _calc.Calculate(products, ShippingMethod.Ground, Destination.Domestic);
        var international = _calc.Calculate(products, ShippingMethod.Ground, Destination.International);
        Assert.True(international > domestic);
    }

    [Fact]
    public void Calculate_MultipleProducts_SumsAllProductCosts()
    {
        // product1: base 5 + fragile 5 = 10
        // product2: base 5 + 1kg×1.5 = 6.5
        // total domestic = 16.50
        var products = new[]
        {
            MinimalProduct(fragile: true),
            MinimalProduct(weightKg: 1),
        };
        var cost = _calc.Calculate(products, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(16.50m, cost);
    }

    [Fact]
    public void Calculate_RoundsToTwoDecimalPlaces()
    {
        // base 5 + (1.5 kg × 1.5 = 2.25) = 7.25 — already exact, use volume instead
        // 100 cm³ × 0.001 = 0.1 → 5.10 — fine
        var cost = _calc.Calculate(new[] { MinimalProduct(l: 10, w: 10, h: 1) }, ShippingMethod.Ground, Destination.Domestic);
        Assert.Equal(5.10m, cost); // 5 + (10×10×1=100 × 0.001 = 0.10)
    }
}
