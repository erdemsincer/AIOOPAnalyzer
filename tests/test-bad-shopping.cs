// Bu kodu bir AI'dan istedik: "Bir e-ticaret sepet sistemi yaz"
// Bakalım AI iyi kod yazmış mı?

public class ShoppingCart
{
    public List<string> items;
    public decimal totalPrice;
    public string customerName;

    public void AddItem(string item)
    {
        items.Add(item);
    }

    public void RemoveItem(string item)
    {
        items.Remove(item);
    }

    public void CalculateTotal()
    {
        totalPrice = items.Count * 10;
    }

    public void Checkout()
    {
        var payment = new PaymentProcessor();
        payment.Process(totalPrice);
    }

    public void SendReceipt()
    {
        var emailer = new EmailSender();
        emailer.Send(customerName, "Receipt");
    }
}

public class PaymentProcessor
{
    public void Process(decimal amount) { }
}

public class EmailSender
{
    public void Send(string to, string body) { }
}
