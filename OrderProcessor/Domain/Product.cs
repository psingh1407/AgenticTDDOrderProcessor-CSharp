namespace OrderProcessor.Domain;

public class Product
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";       // Red, Pink, White, Yellow
    public string Size { get; set; } = "";        // Small, Medium, Large
    public decimal Price { get; set; }
    public decimal Discount { get; set; }         // 0–1
    public string Material { get; set; } = "";    // Glass, Electronic, Fabric, Metal
    public double WeightKg { get; set; }
    public bool Fragile { get; set; }
    public bool ContainsLiquids { get; set; }
    public string Packaging { get; set; } = "";   // Boxed, Loose
    public Dimensions Dimensions { get; set; } = new();

    public decimal EffectivePrice => Price * (1 - Discount);

    public decimal ShippingSurcharge =>
        (decimal)WeightKg * 1.5m
        + (decimal)(Dimensions.LengthCm * Dimensions.WidthCm * Dimensions.HeightCm) * 0.001m
        + (Fragile ? 5m : 0m)
        + (ContainsLiquids ? 3m : 0m)
        + (Packaging == "Loose" ? 2m : 0m);
}

public class Dimensions
{
    public double LengthCm { get; set; }
    public double WidthCm { get; set; }
    public double HeightCm { get; set; }
}
