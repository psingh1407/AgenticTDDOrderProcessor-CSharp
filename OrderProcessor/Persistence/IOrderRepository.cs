using OrderProcessor.Domain;

namespace OrderProcessor.Persistence;

public interface IOrderRepository
{
    Order Create();
    IEnumerable<Order> GetAll();
    Order? GetById(Guid id);
    void Save(Order order);
    void Clear();
}
