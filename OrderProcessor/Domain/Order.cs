namespace OrderProcessor.Domain;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<Product> Products { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? TrackingNumber { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }
    public Destination? Destination { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal Total => Math.Round(Products.Sum(p => p.EffectivePrice), 2);

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm an order with status {Status}.");
        Status = OrderStatus.Confirmed;
    }

    public void Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot ship an order with status {Status}.");
        TrackingNumber = trackingNumber;
        Status = OrderStatus.Shipped;
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot deliver an order with status {Status}.");
        Status = OrderStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered order.");
        Status = OrderStatus.Cancelled;
    }
}
