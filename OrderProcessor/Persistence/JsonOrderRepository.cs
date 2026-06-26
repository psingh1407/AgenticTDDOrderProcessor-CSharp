using System.Text.Json;
using System.Text.Json.Serialization;
using OrderProcessor.Domain;

namespace OrderProcessor.Persistence;

public class JsonOrderRepository : IOrderRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public JsonOrderRepository(string filePath)
    {
        _filePath = filePath;
    }

    private List<Order> Load()
    {
        if (!File.Exists(_filePath)) return new();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<Order>>(json, _options) ?? new();
    }

    private void Persist(List<Order> orders) =>
        File.WriteAllText(_filePath, JsonSerializer.Serialize(orders, _options));

    public Order Create()
    {
        var orders = Load();
        var order = new Order();
        orders.Add(order);
        Persist(orders);
        return order;
    }

    public IEnumerable<Order> GetAll() => Load();

    public Order? GetById(Guid id) => Load().FirstOrDefault(o => o.Id == id);

    public void Save(Order order)
    {
        var orders = Load();
        var idx = orders.FindIndex(o => o.Id == order.Id);
        if (idx >= 0) orders[idx] = order;
        Persist(orders);
    }

    public void Clear() => Persist(new List<Order>());
}
