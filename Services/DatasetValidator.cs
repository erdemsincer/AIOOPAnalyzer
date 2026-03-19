using System;
using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    public class DatasetValidator
    {
        public bool Validate(List<DatasetItem> dataset)
        {
            bool isValid = true;
            int warnings = 0;

            Console.WriteLine("[DOGRULAMA] VERI SETI KONTROLU");
            Console.WriteLine($"   Toplam kayit: {dataset.Count}\n");

            foreach (var item in dataset)
            {
                var issues = new List<string>();

                if (string.IsNullOrWhiteSpace(item.Id))
                    issues.Add("Id bos");
                if (string.IsNullOrWhiteSpace(item.Prompt))
                    issues.Add("Prompt bos");
                if (string.IsNullOrWhiteSpace(item.Code))
                    issues.Add("Code bos");
                if (string.IsNullOrWhiteSpace(item.Label) || (item.Label != "Good" && item.Label != "Bad"))
                    issues.Add("Label 'Good' veya 'Bad' olmali");
                if (item.QualityScore < 0 || item.QualityScore > 100)
                    issues.Add("QualityScore 0-100 araliginda olmali");
                if (item.ExpectedPatterns.Count == 0)
                    issues.Add("ExpectedPatterns bos");

                if (issues.Count > 0)
                {
                    Console.WriteLine($"   [UYARI] #{item.Id}: {string.Join(", ", issues)}");
                    warnings += issues.Count;
                    isValid = false;
                }
            }

            if (isValid)
                Console.WriteLine("   [GECERLI] Tum kayitlar gecerli.\n");
            else
                Console.WriteLine($"\n   Toplam uyari: {warnings}\n");

            // İstatistikler
            PrintStatistics(dataset);

            return isValid;
        }

        private void PrintStatistics(List<DatasetItem> dataset)
        {
            Console.WriteLine("[ISTATISTIK] VERI SETI DAGILIMI");

            // Label dagilimi
            var byLabel = dataset.GroupBy(d => d.Label).OrderBy(g => g.Key);
            Console.WriteLine("   Etiket Dagilimi:");
            foreach (var group in byLabel)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} ornek");
            }

            // Zorluk dagilimi
            var byDifficulty = dataset.GroupBy(d => d.Difficulty).OrderBy(g => g.Key);
            Console.WriteLine("   Zorluk Dagilimi:");
            foreach (var group in byDifficulty)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} test");
            }

            // Kategori dagilimi
            var byCategory = dataset.GroupBy(d => d.Category).OrderBy(g => g.Key);
            Console.WriteLine("   Kategori Dagilimi:");
            foreach (var group in byCategory)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} test");
            }

            // Pattern dagilimi
            var allPatterns = dataset.SelectMany(d => d.ExpectedPatterns).GroupBy(p => p).OrderByDescending(g => g.Count());
            Console.WriteLine("   Beklenen Kalip Dagilimi:");
            foreach (var group in allPatterns)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} kez");
            }

            // AI Model dagilimi
            var byModel = dataset.Where(d => !string.IsNullOrEmpty(d.AIModel)).GroupBy(d => d.AIModel).OrderByDescending(g => g.Count());
            Console.WriteLine("   AI Model Dagilimi:");
            foreach (var group in byModel)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} test");
            }

            // Ortalama kalite skoru
            var goodAvg = dataset.Where(d => d.Label == "Good").Average(d => d.QualityScore);
            var badAvg = dataset.Where(d => d.Label == "Bad").Average(d => d.QualityScore);
            Console.WriteLine($"   Ortalama Kalite Skoru: Good={goodAvg:F0}, Bad={badAvg:F0}");

            Console.WriteLine();
        }
    }
}
