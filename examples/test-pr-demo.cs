// PR Demo: CK Metrikleri test dosyasi
// Bu dosya GitHub Actions OOP Analyzer testini tetiklemek icin olusturuldu

using System;
using System.Collections.Generic;

// ---- INTERFACE ----
public interface IProductRepository
{
    void Add(Product product);
    Product GetById(int id);
    List<Product> GetAll();
}

public interface INotificationService
{
    void Notify(string message);
}

// ---- MODEL ----
public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(int id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0) throw new ArgumentException("Fiyat negatif olamaz");
        Price = newPrice;
    }
}

// ---- REPOSITORY ----
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = new();

    public void Add(Product product) => _products.Add(product);

    public Product GetById(int id) => _products.Find(p => p.Id == id);

    public List<Product> GetAll() => new(_products);
}

// ---- SERVICE ----
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly INotificationService _notifier;

    public ProductService(IProductRepository repository, INotificationService notifier)
    {
        _repository = repository;
        _notifier = notifier;
    }

    public void AddProduct(Product product)
    {
        _repository.Add(product);
        _notifier.Notify($"Yeni urun eklendi: {product.Name}");
    }

    public Product GetProduct(int id)
    {
        return _repository.GetById(id);
    }
}

// ---- NOTIFICATION ----
public class EmailNotifier : INotificationService
{
    public void Notify(string message)
    {
        Console.WriteLine($"[EMAIL] {message}");
    }
}
