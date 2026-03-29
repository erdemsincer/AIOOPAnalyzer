public class StudentManager
{
    public List<string> students;
    public string schoolName;
    public int maxCapacity;
    public string dbConnection;

    public void AddStudent(string name)
    {
        students.Add(name);
    }

    public void RemoveStudent(string name)
    {
        students.Remove(name);
    }

    public void PrintAll()
    {
        foreach (var s in students)
            Console.WriteLine(s);
    }

    public void SaveToDb()
    {
        var db = new DatabaseHelper();
        db.Save(students);
    }

    public void SendReport()
    {
        var mailer = new EmailClient();
        mailer.Send("admin@school.com", "Report", string.Join(",", students));
    }

    public double CalculateAverage(List<int> grades)
    {
        double sum = 0;
        foreach (var g in grades) sum += g;
        return sum / grades.Count;
    }
}

public class DatabaseHelper
{
    public string connStr;
    public void Save(List<string> data) { }
}

public class EmailClient
{
    public string host;
    public int port;
    public void Send(string to, string subject, string body) { }
}
