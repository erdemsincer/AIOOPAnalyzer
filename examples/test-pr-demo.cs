public interface IPaymentService
{
    bool ProcessPayment(decimal amount);
}

public interface INotificationService
{
    void SendNotification(string message);
}

public interface IOrderRepository
{
    void Save(Order order);
}

public abstract class BaseEntity
{
    private int _id;
    public int Id => _id;

    protected BaseEntity(int id)
    {
        _id = id;
    }

    public virtual string GetInfo()
    {
        return $"Entity #{_id}";
    }
}

public class Order : BaseEntity
{
    private readonly List<string> _items;
    private decimal _total;

    public IReadOnlyList<string> Items => _items.AsReadOnly();
    public decimal Total => _total;

    public Order(int id) : base(id)
    {
        _items = new List<string>();
        _total = 0;
    }

    public void AddItem(string item, decimal price)
    {
        _items.Add(item);
        _total += price;
    }

    public override string GetInfo()
    {
        return $"Order #{Id} - {_items.Count} items, Total: {_total:C}";
    }
}

public class OrderService
{
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notificationService;
    private readonly IOrderRepository _orderRepository;

    public OrderService(
        IPaymentService paymentService,
        INotificationService notificationService,
        IOrderRepository orderRepository)
    {
        _paymentService = paymentService;
        _notificationService = notificationService;
        _orderRepository = orderRepository;
    }

    public bool PlaceOrder(Order order)
    {
        if (_paymentService.ProcessPayment(order.Total))
        {
            _orderRepository.Save(order);
            _notificationService.SendNotification($"Order {order.Id} placed successfully.");
            return true;
        }
        return false;
    }
}
