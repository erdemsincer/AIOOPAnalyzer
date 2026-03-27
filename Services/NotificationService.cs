using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    // --- Interfaces ---
    public interface INotificationChannel
    {
        void Send(Notification notification);
    }

    public interface INotificationStore
    {
        void Save(Notification notification);
        IReadOnlyList<Notification> GetAll();
    }

    public interface INotificationFactory
    {
        Notification Create(string message);
    }

    // --- Model (Encapsulation) ---
    public class Notification
    {
        private readonly string _id;
        private readonly string _message;
        private bool _isRead;

        public string Id => _id;
        public string Message => _message;
        public bool IsRead => _isRead;

        public Notification(string id, string message)
        {
            _id = id;
            _message = message;
            _isRead = false;
        }

        public void MarkAsRead()
        {
            _isRead = true;
        }
    }

    // --- Base class + Polymorphism ---
    public abstract class BaseNotificationChannel : INotificationChannel
    {
        public void Send(Notification notification)
        {
            var formatted = FormatMessage(notification);
            Console.WriteLine(formatted);
        }

        protected virtual string FormatMessage(Notification notification)
        {
            return $"[Bildirim] {notification.Message}";
        }
    }

    public class EmailChannel : BaseNotificationChannel
    {
        protected override string FormatMessage(Notification notification)
        {
            return $"[EMAIL] {notification.Message}";
        }
    }

    public class SmsChannel : BaseNotificationChannel
    {
        protected override string FormatMessage(Notification notification)
        {
            return $"[SMS] {notification.Message}";
        }
    }

    // --- Store (SRP - sadece veri) ---
    public class InMemoryNotificationStore : INotificationStore
    {
        private readonly List<Notification> _notifications = new();

        public void Save(Notification notification)
        {
            _notifications.Add(notification);
        }

        public IReadOnlyList<Notification> GetAll() => _notifications.AsReadOnly();
    }

    // --- Factory (SRP - sadece olusturma) ---
    public class NotificationFactory : INotificationFactory
    {
        public Notification Create(string message)
        {
            return new Notification(Guid.NewGuid().ToString("N")[..8], message);
        }
    }

    // --- Service (DI) ---
    public class NotificationService
    {
        private readonly INotificationStore _store;
        private readonly INotificationChannel _channel;

        public NotificationService(INotificationStore store, INotificationChannel channel)
        {
            _store = store;
            _channel = channel;
        }

        public void SendAndStore(Notification notification)
        {
            _store.Save(notification);
            _channel.Send(notification);
        }

        public IReadOnlyList<Notification> GetHistory() => _store.GetAll();
    }
}
