using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    public class PaymentService
    {
        public string apiKey;
        public string dbConnection;
        public List<string> transactions;
        public double totalAmount;
        public string lastError;

        public void ProcessPayment(string cardNumber, double amount)
        {
            var db = new DatabaseHelper();
            db.Save("payment", cardNumber + ":" + amount);
            totalAmount += amount;
            Console.WriteLine("Odeme islendi: " + amount);
        }

        public void Refund(string transactionId, double amount)
        {
            var db = new DatabaseHelper();
            db.Delete(transactionId);
            totalAmount -= amount;
            Console.WriteLine("Iade yapildi: " + amount);
        }

        public void SendReceipt(string email, string transactionId)
        {
            var mailer = new MailSender();
            mailer.Send(email, "Odeme Makbuzu", "Islem: " + transactionId);
        }

        public void GenerateReport()
        {
            foreach (var t in transactions)
                Console.WriteLine(t);
            Console.WriteLine("Toplam: " + totalAmount);
        }

        public void ValidateCard(string cardNumber)
        {
            if (cardNumber.Length != 16)
                lastError = "Gecersiz kart";
            else
                lastError = "";
        }

        public double CalculateTax(double amount)
        {
            return amount * 0.18;
        }

        public void LogTransaction(string message)
        {
            Console.WriteLine("[LOG] " + message);
        }
    }

    public class DatabaseHelper
    {
        public string connectionString;
        public void Save(string table, string data) { }
        public void Delete(string id) { }
    }

    public class MailSender
    {
        public string smtpHost;
        public int port;
        public void Send(string to, string subject, string body) { }
    }
}
