using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    public class BadTestClass
    {
        // Encapsulation ihlali: public alanlar
        public string apiKey;
        public string dbConnection;
        public List<string> transactions;
        public double totalAmount;
        public string lastError;
        public int counter;
        public bool isActive;

        // Çok fazla metod (SRP ihlali)
        public void ProcessPayment(string cardNumber, double amount)
        {
            // DI ihlali: new ile nesne oluşturma
            var db = new DatabaseHelper();
            db.Save("payment", cardNumber + ":" + amount);
            totalAmount += amount;
            Console.WriteLine("Odeme islendi: " + amount);
        }

        public void Refund(string transactionId, double amount)
        {
            // DI ihlali
            var db = new DatabaseHelper();
            db.Delete(transactionId);
            totalAmount -= amount;
            Console.WriteLine("Iade yapildi: " + amount);
        }

        public void SendReceipt(string email, string transactionId)
        {
            // DI ihlali
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

        public void LogTransaction(string message)
        {
            // DI ihlali
            var logger = new Logger();
            logger.Write(message);
        }

        public void CalculateTax(double amount)
        {
            double tax = amount * 0.18;
            Console.WriteLine("Vergi: " + tax);
        }

        public void UpdateStatus(string status)
        {
            isActive = status == "active";
        }

        public void ResetCounter()
        {
            counter = 0;
        }

        public void IncrementCounter()
        {
            counter++;
        }

        // Interface implement etmeme (Interfaces kuralı ihlali)
        // Inheritance yok (Inheritance kuralı ihlali)
        // Polymorphism yok (Polymorphism kuralı ihlali)

        // Yardımcı sınıflar (DI ihlali için)
        private class DatabaseHelper
        {
            public void Save(string table, string data) { }
            public void Delete(string id) { }
        }

        private class MailSender
        {
            public void Send(string to, string subject, string body) { }
        }

        private class Logger
        {
            public void Write(string message) { }
        }
    }
}