using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Services
{
    public class StudentManager
    {
        public List<string> students;
        public string schoolName;
        public string principalName;
        public int budget;
        public string dbHost;
        public string dbPassword;

        public void AddStudent(string name)
        {
            students.Add(name);
            var db = new SchoolDatabase();
            db.Insert("students", name);
            Console.WriteLine("Ogrenci eklendi: " + name);
        }

        public void RemoveStudent(string name)
        {
            students.Remove(name);
            var db = new SchoolDatabase();
            db.Delete("students", name);
        }

        public void PrintAllStudents()
        {
            foreach (var s in students)
                Console.WriteLine(s);
        }

        public void SendEmail(string to, string message)
        {
            var smtp = new SmtpClient();
            smtp.Send(to, "Okul Bildirimi", message);
        }

        public double CalculateGPA(List<int> grades)
        {
            double sum = 0;
            foreach (var g in grades)
                sum += g;
            return sum / grades.Count;
        }

        public void GenerateTranscript(string studentName)
        {
            Console.WriteLine("Transkript: " + studentName);
            Console.WriteLine("Okul: " + schoolName);
        }

        public void UpdateBudget(int amount)
        {
            budget += amount;
            Console.WriteLine("Yeni butce: " + budget);
        }

        public bool IsHonorStudent(double gpa)
        {
            return gpa > 3.5;
        }
    }

    public class SchoolDatabase
    {
        public string connectionString;
        public string host;
        public void Insert(string table, string data) { }
        public void Delete(string table, string data) { }
    }

    public class SmtpClient
    {
        public string server;
        public int port;
        public string username;
        public void Send(string to, string subject, string body) { }
    }
}
