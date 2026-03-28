using System;
using AIOOPAnalyzer.Models;

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

            // 4) Birleşik skor (kural bazlı yüzde * ruleWeight + ML score * mlWeight)
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
            Console.WriteLine("+------------------------------------------+");
            Console.WriteLine("|         HIBRIT ANALIZ RAPORU             |");
            Console.WriteLine("+------------------------------------------+\n");

            // -- KURAL BAZLI SONUCLAR --
            Console.WriteLine("[KURAL BAZLI ANALIZ]");
            foreach (var rule in result.RuleBasedResult.RuleResults)
            {
                string durum = rule.Score == rule.MaxScore ? "TAMAM" : "UYARI";
                Console.WriteLine($"   [{durum}] {rule.RuleName}: {rule.Score}/{rule.MaxScore}");
            }
            double rulePercent = result.RuleBasedResult.MaxScore > 0
                ? (double)result.RuleBasedResult.TotalScore / result.RuleBasedResult.MaxScore
                : 0;
            Console.WriteLine($"   TOPLAM: {result.RuleBasedResult.TotalScore}/{result.RuleBasedResult.MaxScore} ({rulePercent:P0})");

            if (result.RuleBasedResult.Issues.Count > 0)
            {
                Console.WriteLine("\n   Tespit Edilen Sorunlar:");
                foreach (var issue in result.RuleBasedResult.Issues)
                    Console.WriteLine($"      - {issue}");
            }

            // -- ML TAHMINI --
            Console.WriteLine("\n[ML MODELI TAHMINI]");
            string mlDurum = result.MLResult.PredictedLabel == "Good" ? "BASARILI" : "BASARISIZ";
            Console.WriteLine($"   Tahmin: {result.MLResult.PredictedLabel} ({mlDurum})");
            Console.WriteLine($"   Guven Orani: {result.MLResult.Confidence:P0}");
            Console.WriteLine($"   Tahmini Kalite Skoru: {result.MLResult.PredictedScore}/100");
            Console.WriteLine($"   Karsilastirma: k={result.MLResult.K}, En yakin ornekler: [{string.Join(", ", result.MLResult.NearestNeighbors)}]");

            // -- OZELLIK OZETI --
            Console.WriteLine("\n[OZELLIK OZETI]");
            Console.WriteLine($"   Sinif sayisi           : {result.Features.ClassCount}");
            Console.WriteLine($"   Kapsulleme orani       : {result.Features.EncapsulationRatio:P0} (public: {result.Features.PublicFieldCount}, private: {result.Features.PrivateFieldCount})");
            Console.WriteLine($"   Interface kullanimi     : {result.Features.InterfaceRatio:P0} ({result.Features.ClassesWithInterface}/{result.Features.ClassCount} sinif)");
            Console.WriteLine($"   Bagimlilik Enjeksiyonu  : {(result.Features.HasDirectInstantiation == 0 ? "Uygun (new kullanimi yok)" : $"Uygun degil ({result.Features.ObjectCreationCount} adet new kullanimi)")}");
            Console.WriteLine($"   Polimorfizm            : virtual={result.Features.VirtualMethodCount}, override={result.Features.OverrideMethodCount}");

            // -- BIRLESIK SONUC --
            Console.WriteLine("\n+------------------------------------------+");
            Console.WriteLine($"  BIRLESIK SKOR: {result.CombinedScore}/100");
            Console.WriteLine($"  Hesaplama: Kural {rulePercent:P0} (x{_ruleWeight:F2}) + ML {result.MLResult.PredictedScore:F0} (x{_mlWeight:F2})");
            Console.WriteLine($"  Esik (qualityThreshold): {_hybrid.QualityThreshold}");
            string finalDurum = result.FinalVerdict == "Good" ? "BASARILI" : "BASARISIZ";
            Console.WriteLine($"  FINAL KARAR: {result.FinalVerdict} ({finalDurum})");
            Console.WriteLine("+------------------------------------------+");
        }
    }
}
