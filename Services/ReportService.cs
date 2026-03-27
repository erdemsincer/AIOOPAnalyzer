using System;
using System.Collections.Generic;
using System.Linq;

namespace AIOOPAnalyzer.Services
{
    // --- Interfaces (Abstraction + DI) ---
    public interface IReportRepository
    {
        void Save(Report report);
        IReadOnlyList<Report> GetAll();
    }

    public interface IReportGenerator
    {
        Report Generate(string title);
    }

    public interface IReportNotifier
    {
        void Notify(string recipient, Report report);
    }

    // --- Model (Encapsulation) ---
    public enum ReportType
    {
        Summary,
        Detailed
    }

    public class Report
    {
        private readonly string _id;
        private string _title;

        public string Id => _id;
        public string Title => _title;

        public Report(string id, string title)
        {
            _id = id;
            _title = title;
        }

        public void UpdateTitle(string newTitle)
        {
            _title = newTitle;
        }
    }

    // --- Base class + Polymorphism ---
    public abstract class BaseReportGenerator : IReportGenerator
    {
        protected virtual string FormatTitle(string title)
        {
            return $"[Rapor] {title}";
        }

        public Report Generate(string title)
        {
            var formatted = FormatTitle(title);
            return new Report(Guid.NewGuid().ToString("N")[..8], formatted);
        }
    }

    public class SummaryReportGenerator : BaseReportGenerator
    {
        protected override string FormatTitle(string title)
        {
            return $"[OZET] {title}";
        }
    }

    public class DetailedReportGenerator : BaseReportGenerator
    {
        protected override string FormatTitle(string title)
        {
            return $"[DETAY] {title}";
        }
    }

    // --- Repository (SRP) ---
    public class InMemoryReportRepository : IReportRepository
    {
        private readonly List<Report> _reports = new();

        public void Save(Report report)
        {
            _reports.Add(report);
        }

        public IReadOnlyList<Report> GetAll() => _reports.AsReadOnly();
    }

    // --- Notifier (SRP) ---
    public class EmailReportNotifier : IReportNotifier
    {
        private readonly string _smtpHost;

        public EmailReportNotifier(string smtpHost)
        {
            _smtpHost = smtpHost;
        }

        public void Notify(string recipient, Report report)
        {
            Console.WriteLine($"[EMAIL] '{report.Title}' -> {recipient} via {_smtpHost}");
        }
    }

    // --- Service (DI ile bagimliliklari disaridan alir) ---
    public class ReportService
    {
        private readonly IReportRepository _repository;
        private readonly IReportGenerator _generator;

        public ReportService(IReportRepository repository, IReportGenerator generator)
        {
            _repository = repository;
            _generator = generator;
        }

        public Report CreateReport(string title)
        {
            var report = _generator.Generate(title);
            _repository.Save(report);
            return report;
        }

        public IReadOnlyList<Report> ListReports() => _repository.GetAll();
    }
}
