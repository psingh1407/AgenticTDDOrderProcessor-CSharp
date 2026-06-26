namespace OrderProcessor.Domain;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<Product> Products { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total => Math.Round(Products.Sum(p => p.EffectivePrice), 2);

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm an order with status {Status}.");
        Status = OrderStatus.Confirmed;
    }
}
