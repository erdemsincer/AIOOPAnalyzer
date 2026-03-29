using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    public class OrderManager
    {
        public List<string> orders;
        public double totalRevenue;
        public string customerEmail;
        public string warehouseAddress;
        public int failedCount;
        public string lastStatus;
        public string apiToken;

        public void CreateOrder(string product, int qty, double price)
        {
            var db = new OrderDatabase();
            db.Save(product + ":" + qty);
            totalRevenue += qty * price;
            Console.WriteLine("Siparis olusturuldu: " + product);
        }

        public void CancelOrder(string orderId)
        {
            var db = new OrderDatabase();
            db.Remove(orderId);
            failedCount++;
            Console.WriteLine("Siparis iptal edildi: " + orderId);
        }

        public void ShipOrder(string orderId)
        {
            var cargo = new CargoService();
            cargo.Ship(orderId, warehouseAddress);
            lastStatus = "shipped";
        }

        public void SendInvoice(string email, string orderId)
        {
            var mailer = new InvoiceMailer();
            mailer.Send(email, "Fatura", "Siparis: " + orderId);
        }

        public double CalculateDiscount(double amount, int loyaltyPoints)
        {
            if (loyaltyPoints > 100)
                return amount * 0.15;
            else if (loyaltyPoints > 50)
                return amount * 0.10;
            return 0;
        }

        public void PrintReport()
        {
            Console.WriteLine("Toplam gelir: " + totalRevenue);
            Console.WriteLine("Basarisiz: " + failedCount);
            foreach (var o in orders)
                Console.WriteLine(o);
        }

        public bool ValidateStock(string product, int qty)
        {
            return qty > 0 && qty < 1000;
        }

        public void UpdateCustomer(string email)
        {
            customerEmail = email;
            Console.WriteLine("Musteri guncellendi: " + email);
        }
    }

    public class OrderDatabase
    {
        public string connStr;
        public void Save(string data) { }
        public void Remove(string id) { }
    }

    public class CargoService
    {
        public string apiUrl;
        public string trackingCode;
        public void Ship(string orderId, string address) { }
    }

    public class InvoiceMailer
    {
        public string host;
        public int port;
        public string fromAddress;
        public void Send(string to, string subject, string body) { }
    }
}
