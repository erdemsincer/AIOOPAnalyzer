using System;
using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;
using AIOOPAnalyzer.Analyzers;

namespace AIOOPAnalyzer.Services
{
    /// <summary>
    /// Hibrit analiz sonucu — kural bazlı + ML tahmini yan yana.
    /// </summary>
    public class HybridResult
    {
        public AnalysisResult RuleBasedResult { get; set; } = new();
        public PredictionResult MLResult { get; set; } = new();
        public TrainingFeatures Features { get; set; } = new();

        /// <summary>Sınıf bazlı CK metrikleri</summary>
        public List<CKMetrics> CKMetricsPerClass { get; set; } = new();
        /// <summary>Ortalama CK metrikleri</summary>
        public CKMetrics CKMetricsAverage { get; set; } = new();

        /// <summary>Birleşik skor: kural bazlı %60 + ML %40 ağırlıkla</summary>
        public double CombinedScore { get; set; }
        /// <summary>Birleşik sonuç: "Good" veya "Bad"</summary>
        public string FinalVerdict { get; set; } = "";
    }

    /// <summary>
    /// Kullanıcı kodunu hem kural bazlı hem ML ile analiz eder ve birleşik rapor üretir.
    /// </summary>
    public class HybridAnalyzer
    {
        private readonly CodeParserService _parser;
        private readonly AnalyzerService _ruleAnalyzer;
        private readonly ModelPredictor _mlPredictor;
        private readonly FeatureExtractor _featureExtractor;
        private readonly CKMetricsAnalyzer _ckAnalyzer;
        private readonly double _ruleWeight;
        private readonly double _mlWeight;
        private readonly HybridConfig _hybrid;

        public HybridAnalyzer(
            RulesConfig rulesConfig,
            HybridConfig hybridConfig,
            string modelPath = "models/model.json")
        {
            _parser = new CodeParserService();
            _ruleAnalyzer = new AnalyzerService(rulesConfig);
            _mlPredictor = new ModelPredictor(modelPath);
            _featureExtractor = new FeatureExtractor();
            _ckAnalyzer = new CKMetricsAnalyzer();
            _hybrid = hybridConfig ?? new HybridConfig();
            double rw = _hybrid.RuleWeight;
            double mw = _hybrid.MLWeight;
            double sum = rw + mw;
            if (sum > 0)
            {
                _ruleWeight = rw / sum;
                _mlWeight = mw / sum;
            }
            else
            {
                _ruleWeight = 0.6;
                _mlWeight = 0.4;
            }
        }

        /// <summary>
        /// Bir C# kaynak kodunu hibrit olarak analiz eder.
        /// </summary>
        public HybridResult Analyze(string sourceCode)
        {
            var code = _parser.Parse(sourceCode);

            // 1) Kural bazlı analiz
            var ruleResult = _ruleAnalyzer.Run(code);

            // 2) ML tahmini
            var mlResult = _mlPredictor.Predict(sourceCode);

            // 3) Özellikler
            var features = _featureExtractor.Extract(code);

            // 4) CK Metrikleri
            var ckPerClass = _ckAnalyzer.Calculate(code);
            var ckAvg = _ckAnalyzer.CalculateAverage(code);

            // 5) Birleşik skor (kural bazlı yüzde * ruleWeight + ML score * mlWeight)
            double rulePercent = ruleResult.MaxScore > 0
                ? (double)ruleResult.TotalScore / ruleResult.MaxScore * 100
                : 0;

            double combinedScore = ComputeCombinedScore(rulePercent, mlResult, _hybrid);

            string verdict = ComputeFinalVerdict(rulePercent, mlResult, _hybrid);

            return new HybridResult
            {
                RuleBasedResult = ruleResult,
                MLResult = mlResult,
                Features = features,
                CKMetricsPerClass = ckPerClass,
                CKMetricsAverage = ckAvg,
                CombinedScore = Math.Round(combinedScore, 1),
                FinalVerdict = verdict
            };
        }

        /// <summary>
        /// K (0-100), ML tahmini ve config ile nihai Good/Bad karari.
        /// </summary>
        public static string ComputeFinalVerdict(double rulePercent100, PredictionResult ml, HybridConfig h)
        {
            if (rulePercent100 >= h.StrongAgreementHighRulePercent && ml.PredictedLabel == "Good")
                return "Good";
            if (rulePercent100 < h.StrongAgreementLowRulePercent && ml.PredictedLabel == "Bad")
                return "Bad";
            double q = ComputeCombinedScore(rulePercent100, ml, h);
            return q >= h.QualityThreshold ? "Good" : "Bad";
        }

        /// <summary>
        /// <see cref="Analyze"/> ile ayni birlesik Q degerini hesaplar (agirliklar normalize).
        /// </summary>
        public static double ComputeCombinedScore(double rulePercent100, PredictionResult ml, HybridConfig h)
        {
            double sum = h.RuleWeight + h.MLWeight;
            if (sum <= 0)
                return 0.6 * rulePercent100 + 0.4 * ml.PredictedScore;
            return (h.RuleWeight / sum * rulePercent100) + (h.MLWeight / sum * ml.PredictedScore);
        }

        /// <summary>
        /// Hibrit analiz sonucunu detaylı yazdırır.
        /// </summary>
        public void PrintReport(HybridResult result)
        {
            double rulePercent = result.RuleBasedResult.MaxScore > 0
                ? (double)result.RuleBasedResult.TotalScore / result.RuleBasedResult.MaxScore
                : 0;

            // ═══════════════ BAŞLIK ═══════════════
            Console.WriteLine();
            WriteColor("╔══════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
            WriteColor("║              AI OOP ANALYZER — HIBRIT RAPOR                 ║", ConsoleColor.Cyan);
            WriteColor("╚══════════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);
            Console.WriteLine();

            // ═══════════════ KURAL BAZLI SONUÇLAR ═══════════════
            WriteColor("┌─────────────────────────────────────────────────────────────┐", ConsoleColor.Yellow);
            WriteColor("│  KURAL BAZLI ANALIZ                                         │", ConsoleColor.Yellow);
            WriteColor("├──────────────────────┬───────────┬───────────────────────────┤", ConsoleColor.Yellow);

            foreach (var rule in result.RuleBasedResult.RuleResults)
            {
                double pct = rule.MaxScore > 0 ? (double)rule.Score / rule.MaxScore : 0;
                string bar = MakeBar(pct, 15);
                string icon = rule.Score == rule.MaxScore ? "✅" : "⚠️ ";
                var barColor = pct >= 0.8 ? ConsoleColor.Green : pct >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red;

                Console.Write("│ ");
                WriteColor($"{icon} {rule.RuleName,-18}", rule.Score == rule.MaxScore ? ConsoleColor.Green : ConsoleColor.Red);
                Console.Write("│ ");
                WriteColor($"{rule.Score,3}/{rule.MaxScore,-3}", barColor);
                Console.Write("   │ ");
                WriteColor(bar, barColor);
                Console.Write($" {pct:P0}");
                Console.WriteLine(new string(' ', Math.Max(0, 10 - $" {pct:P0}".Length)) + "│");
            }

            WriteColor("├──────────────────────┴───────────┴───────────────────────────┤", ConsoleColor.Yellow);
            var totalColor = rulePercent >= 0.7 ? ConsoleColor.Green : rulePercent >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.Write("│  TOPLAM: ");
            WriteColor($"{result.RuleBasedResult.TotalScore}/{result.RuleBasedResult.MaxScore} ({rulePercent:P0})", totalColor);
            Console.Write("  ");
            WriteColor(MakeBar(rulePercent, 20), totalColor);
            Console.WriteLine(new string(' ', Math.Max(0, 14 - MakeBar(rulePercent, 20).Length + 14)) + "│");
            WriteColor("└─────────────────────────────────────────────────────────────┘", ConsoleColor.Yellow);

            // Sorunlar
            if (result.RuleBasedResult.Issues.Count > 0)
            {
                Console.WriteLine();
                WriteColor($"  ⚠  Tespit Edilen Sorunlar ({result.RuleBasedResult.Issues.Count} adet):", ConsoleColor.Red);
                foreach (var issue in result.RuleBasedResult.Issues)
                {
                    Console.Write("     ");
                    WriteColor("• ", ConsoleColor.Red);
                    Console.WriteLine(issue);
                }
            }

            // ═══════════════ ML TAHMİNİ ═══════════════
            Console.WriteLine();
            WriteColor("┌─────────────────────────────────────────────────────────────┐", ConsoleColor.Magenta);
            WriteColor("│  ML MODELI TAHMINI (k-NN)                                   │", ConsoleColor.Magenta);
            WriteColor("├─────────────────────────────────────────────────────────────┤", ConsoleColor.Magenta);

            string mlIcon = result.MLResult.PredictedLabel == "Good" ? "✅" : "❌";
            var mlColor = result.MLResult.PredictedLabel == "Good" ? ConsoleColor.Green : ConsoleColor.Red;

            Console.Write("│  Tahmin: ");
            WriteColor($"{mlIcon} {result.MLResult.PredictedLabel}", mlColor);
            Console.Write($"   Guven: ");
            WriteColor($"{result.MLResult.Confidence:P0}", ConsoleColor.White);
            Console.Write($"   Skor: ");
            WriteColor($"{result.MLResult.PredictedScore}/100", mlColor);
            Console.WriteLine(new string(' ', 5) + "│");

            Console.Write("│  Yakin ornekler: ");
            WriteColor($"[{string.Join(", ", result.MLResult.NearestNeighbors)}]", ConsoleColor.DarkGray);
            int pad = Math.Max(0, 41 - $"[{string.Join(", ", result.MLResult.NearestNeighbors)}]".Length);
            Console.WriteLine(new string(' ', pad) + "│");
            WriteColor("└─────────────────────────────────────────────────────────────┘", ConsoleColor.Magenta);

            // ═══════════════ CK METRİKLERİ SKORKART ═══════════════
            Console.WriteLine();
            WriteColor("╔═════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
            WriteColor("║     CK METRIKLERI — Chidamber & Kemerer Skorkart           ║", ConsoleColor.Cyan);
            WriteColor("╠═════════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);

            if (result.CKMetricsPerClass.Count > 0)
            {
                // Her sınıf için dashboard
                foreach (var ck in result.CKMetricsPerClass)
                {
                    Console.Write("║ ");
                    WriteColor($"  {ck.ClassName}", ConsoleColor.White);
                    Console.WriteLine(new string(' ', Math.Max(0, 57 - ck.ClassName.Length)) + "║");
                    WriteColor("║  ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄║", ConsoleColor.DarkGray);

                    PrintCKMetricRow("WMC", "Agirlikli Metod   ", ck.WMC, 10, 20);
                    PrintCKMetricRow("DIT", "Kalitim Derinligi ", ck.DIT, 3, 6);
                    PrintCKMetricRow("NOC", "Alt Sinif Sayisi  ", ck.NOC, 5, 10);
                    PrintCKMetricRow("CBO", "Bagimlilik        ", ck.CBO, 5, 10);
                    PrintCKMetricRow("RFC", "Yanit Sayisi      ", ck.RFC, 20, 40);
                    PrintCKMetricRow("LCOM", "Uyumsuzluk       ", ck.LCOM, 3, 10);

                    // Sınıf notu
                    int ckIssues = (ck.WMC > 10 ? 1 : 0) + (ck.DIT > 3 ? 1 : 0) + (ck.CBO > 5 ? 1 : 0) +
                                   (ck.RFC > 20 ? 1 : 0) + (ck.LCOM > 3 ? 1 : 0);
                    string grade = ckIssues == 0 ? "A+" : ckIssues == 1 ? "B" : ckIssues == 2 ? "C" : ckIssues <= 3 ? "D" : "F";
                    string gradeIcon = ckIssues == 0 ? "🏆" : ckIssues <= 1 ? "👍" : ckIssues <= 2 ? "⚠️ " : "❌";
                    var gradeColor = ckIssues == 0 ? ConsoleColor.Green : ckIssues <= 1 ? ConsoleColor.Yellow : ConsoleColor.Red;

                    Console.Write("║     ");
                    WriteColor($"Not: {gradeIcon} {grade}", gradeColor);
                    Console.Write("  ");
                    string gradeText = ckIssues == 0 ? "Mukemmel tasarim!" :
                                       ckIssues == 1 ? "Iyi, kucuk iyilestirme" :
                                       ckIssues == 2 ? "Orta, refactor onerilir" : "Kotu, yeniden tasarlanmali";
                    WriteColor(gradeText, gradeColor);
                    Console.WriteLine(new string(' ', Math.Max(0, 35 - gradeText.Length)) + "║");

                    WriteColor("║                                                             ║", ConsoleColor.Cyan);
                }

                // Ortalama özet
                var avg = result.CKMetricsAverage;
                WriteColor("╠═════════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);
                Console.Write("║  ");
                WriteColor("ORTALAMA OZET", ConsoleColor.White);
                Console.WriteLine("                                            ║");
                Console.Write("║  WMC=");
                WriteColorValue(avg.WMC, 10);
                Console.Write("  DIT=");
                WriteColorValue(avg.DIT, 3);
                Console.Write("  NOC=");
                WriteColorValue(avg.NOC, 5);
                Console.Write("  CBO=");
                WriteColorValue(avg.CBO, 5);
                Console.Write("  RFC=");
                WriteColorValue(avg.RFC, 20);
                Console.Write("  LCOM=");
                WriteColorValue(avg.LCOM, 3);
                Console.WriteLine("    ║");
            }
            else
            {
                Console.WriteLine("║  Sinif bulunamadi, CK metrikleri hesaplanamadi.             ║");
            }
            WriteColor("╚═════════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);

            // ═══════════════ ÖZELLIK ÖZETİ ═══════════════
            Console.WriteLine();
            WriteColor("┌─────────────────────────────────────────────────────────────┐", ConsoleColor.DarkCyan);
            WriteColor("│  OZELLIK OZETI                                              │", ConsoleColor.DarkCyan);
            WriteColor("├─────────────────────────────────────────────────────────────┤", ConsoleColor.DarkCyan);

            Console.Write("│  Sinif sayisi          : ");
            WriteColor($"{result.Features.ClassCount}", ConsoleColor.White);
            Console.WriteLine(new string(' ', Math.Max(0, 34 - $"{result.Features.ClassCount}".Length)) + "│");

            Console.Write("│  Kapsulleme orani      : ");
            var encColor = result.Features.EncapsulationRatio >= 0.8 ? ConsoleColor.Green : result.Features.EncapsulationRatio >= 0.5 ? ConsoleColor.Yellow : ConsoleColor.Red;
            WriteColor($"{result.Features.EncapsulationRatio:P0}", encColor);
            Console.Write($" (pub:{result.Features.PublicFieldCount} priv:{result.Features.PrivateFieldCount})");
            Console.WriteLine(new string(' ', Math.Max(0, 18 - $" (pub:{result.Features.PublicFieldCount} priv:{result.Features.PrivateFieldCount})".Length)) + "│");

            Console.Write("│  Interface kullanimi   : ");
            var ifColor = result.Features.InterfaceRatio >= 0.5 ? ConsoleColor.Green : ConsoleColor.Red;
            WriteColor($"{result.Features.InterfaceRatio:P0}", ifColor);
            Console.Write($" ({result.Features.ClassesWithInterface}/{result.Features.ClassCount} sinif)");
            Console.WriteLine(new string(' ', Math.Max(0, 22 - $" ({result.Features.ClassesWithInterface}/{result.Features.ClassCount} sinif)".Length)) + "│");

            string diText = result.Features.HasDirectInstantiation == 0
                ? "✅ Uygun (new yok)"
                : $"❌ {result.Features.ObjectCreationCount} adet new";
            Console.Write("│  DI Durumu             : ");
            WriteColor(diText, result.Features.HasDirectInstantiation == 0 ? ConsoleColor.Green : ConsoleColor.Red);
            Console.WriteLine(new string(' ', Math.Max(0, 34 - diText.Length)) + "│");

            Console.Write("│  Polimorfizm           : ");
            string polyText = $"virtual={result.Features.VirtualMethodCount} override={result.Features.OverrideMethodCount}";
            var polyColor = (result.Features.VirtualMethodCount + result.Features.OverrideMethodCount) > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray;
            WriteColor(polyText, polyColor);
            Console.WriteLine(new string(' ', Math.Max(0, 34 - polyText.Length)) + "│");

            WriteColor("└─────────────────────────────────────────────────────────────┘", ConsoleColor.DarkCyan);

            // ═══════════════ FİNAL KARAR ═══════════════
            Console.WriteLine();
            var finalColor = result.FinalVerdict == "Good" ? ConsoleColor.Green : ConsoleColor.Red;
            string finalIcon = result.FinalVerdict == "Good" ? "✅" : "❌";
            string finalText = result.FinalVerdict == "Good" ? "BASARILI" : "BASARISIZ";

            WriteColor("╔══════════════════════════════════════════════════════════════╗", finalColor);
            Console.Write("║");
            WriteColor($"  BIRLESIK SKOR: ", ConsoleColor.White);

            string scoreBar = MakeBar(result.CombinedScore / 100.0, 20);
            WriteColor(scoreBar, finalColor);
            WriteColor($" {result.CombinedScore}/100", finalColor);
            Console.WriteLine(new string(' ', Math.Max(0, 18 - $" {result.CombinedScore}/100".Length)) + "║");

            Console.Write("║  Kural ");
            WriteColor($"{rulePercent:P0}", ConsoleColor.Yellow);
            Console.Write($" (x{_ruleWeight:F2}) + ML ");
            WriteColor($"{result.MLResult.PredictedScore:F0}", ConsoleColor.Magenta);
            Console.Write($" (x{_mlWeight:F2})");
            Console.Write($"  Esik: {_hybrid.QualityThreshold}");
            Console.WriteLine(new string(' ', Math.Max(0, 10)) + "║");

            WriteColor("╠══════════════════════════════════════════════════════════════╣", finalColor);
            Console.Write("║       ");
            WriteColor($"{finalIcon}  FINAL KARAR: {result.FinalVerdict} ({finalText})", finalColor);
            Console.Write("       ");
            Console.WriteLine(new string(' ', Math.Max(0, 20 - finalText.Length)) + "║");
            WriteColor("╚══════════════════════════════════════════════════════════════╝", finalColor);
            Console.WriteLine();
        }

        // ═══════════ YARDIMCI METOTLAR ═══════════

        /// <summary>CK metrik satırını bar grafik ile yazdırır</summary>
        private static void PrintCKMetricRow(string code, string name, int value, int warnThreshold, int maxDisplay)
        {
            var color = value <= warnThreshold ? ConsoleColor.Green : ConsoleColor.Red;
            string icon = value <= warnThreshold ? "✅" : "🔴";
            double ratio = Math.Min(1.0, (double)value / Math.Max(1, maxDisplay));
            string bar = MakeBar(ratio, 12);

            Console.Write("║   ");
            WriteColor($"{code,-5}", ConsoleColor.White);
            Console.Write($"{name} ");
            WriteColor($"{value,3}", color);
            Console.Write($"/{warnThreshold,-3} ");
            WriteColor(bar, color);
            Console.Write($" {icon}");
            Console.WriteLine(new string(' ', Math.Max(0, 13 - bar.Length)) + "║");
        }

        /// <summary>Değeri eşiğe göre renkli yazar (satır içi)</summary>
        private static void WriteColorValue(int value, int threshold)
        {
            var color = value <= threshold ? ConsoleColor.Green : ConsoleColor.Red;
            WriteColor($"{value}", color);
        }

        /// <summary>Renkli konsol yazımı</summary>
        private static void WriteColor(string text, ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = prev;
        }

        /// <summary>Yüzdelik bar grafik oluşturur: ████████░░░░</summary>
        private static string MakeBar(double ratio, int width)
        {
            ratio = Math.Max(0, Math.Min(1, ratio));
            int filled = (int)(ratio * width);
            int empty = width - filled;
            return new string('█', filled) + new string('░', empty);
        }
    }
}
