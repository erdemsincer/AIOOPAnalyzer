using System;
using System.Collections.Generic;

namespace AIOOPAnalyzer.Models
{
    public class DatasetItem
    {
        // Temel bilgiler
        public string Id { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string Description { get; set; } = "";

        // Kodlar — her örnekte BİR kod var, Label ile doğru/yanlış belirtiliyor
        public string Code { get; set; } = "";

        // ── EĞİTİM ETİKETLERİ ──
        // "Good" = doğru yazılmış, "Bad" = yanlış yazılmış
        public string Label { get; set; } = "";
        // 0-100 arası gerçek kalite skoru (eğitimde hedef değer)
        public int QualityScore { get; set; }
        // Hangi kurallar ihlal ediliyor (eğitim için) — boşsa ihlal yok
        public List<string> IssueLabels { get; set; } = new();

        // Beklentiler
        public List<string> ExpectedPatterns { get; set; } = new();
        public int ExpectedMinScore { get; set; }
        public int ExpectedIssueCount { get; set; }

        // Sınıflandırma
        public string Difficulty { get; set; } = "Medium";       // Easy, Medium, Hard
        public string Category { get; set; } = "OOP";            // OOP, SOLID, DesignPattern
        public List<string> Tags { get; set; } = new();

        // AI bilgisi
        public string AIModel { get; set; } = "";                // GPT-4, Claude, Copilot vb.
        public string AIVersion { get; set; } = "";

        // Meta
        public string Author { get; set; } = "";
        public string CreatedDate { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
