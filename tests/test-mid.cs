public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}

public abstract class BaseRepository
{
    protected string _connectionString;

    protected BaseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual void Connect()
    {
        // connect logic
    }
}

public class UserRepository : BaseRepository
{
    public List<string> users;
    public string cacheKey;

    public UserRepository(string conn) : base(conn)
    {
        users = new List<string>();
    }

    public void Add(string user) { users.Add(user); }
    public void Remove(string user) { users.Remove(user); }
    public void Clear() { users.Clear(); }

    public override void Connect()
    {
        base.Connect();
    }

    public void SendEmail(string to)
    {
        var smtp = new SmtpClient();
        smtp.Send(to, "Users updated");
    }
}

public class SmtpClient
{
    public string host;
    public void Send(string to, string msg) { }
}
