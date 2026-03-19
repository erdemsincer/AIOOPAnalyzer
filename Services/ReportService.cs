using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    public class ReportService
    {
        public string dbConnection;
        public string apiKey;
        public List<string> reports;

        public void GenerateReport(string type)
        {
            var db = new DatabaseConnector();
            var data = db.GetData(type);
            Console.WriteLine(data);
        }

        public void SendReport(string to)
        {
            var mailer = new SmtpMailer();
            mailer.Send(to, "Report", "data");
        }

        public void SaveReport(string name)
        {
            Console.WriteLine($"Saving {name}");
        }

        public void DeleteReport(string id)
        {
            Console.WriteLine($"Deleting {id}");
        }

        public void ListReports()
        {
            foreach (var r in reports)
                Console.WriteLine(r);
        }
    }

    public class DatabaseConnector
    {
        public string connStr;
        public string GetData(string query) { return "data"; }
    }

    public class SmtpMailer
    {
        public string host;
        public int port;
        public void Send(string to, string subject, string body) { }
    }
}
