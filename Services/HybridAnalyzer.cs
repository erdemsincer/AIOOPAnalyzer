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

        /// <param name="ruleWeight">Kural bazlı skorun ağırlığı (varsayılan: 0.6)</param>
        /// <param name="mlWeight">ML tahmininin ağırlığı (varsayılan: 0.4)</param>
        public HybridAnalyzer(
            RulesConfig config,
            string modelPath = "models/model.json",
            double ruleWeight = 0.6,
            double mlWeight = 0.4)
        {
            _parser = new CodeParserService();
            _ruleAnalyzer = new AnalyzerService(config);
            _mlPredictor = new ModelPredictor(modelPath);
            _featureExtractor = new FeatureExtractor();
            _ruleWeight = ruleWeight;
            _mlWeight = mlWeight;
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

            double combinedScore = (_ruleWeight * rulePercent) + (_mlWeight * mlResult.PredictedScore);

            // 5) Final karar
            string verdict;
            if (rulePercent >= 70 && mlResult.PredictedLabel == "Good")
                verdict = "Good";
            else if (rulePercent < 50 && mlResult.PredictedLabel == "Bad")
                verdict = "Bad";
            else if (combinedScore >= 65)
                verdict = "Good";
            else
                verdict = "Bad";

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
            Console.WriteLine($"  Hesaplama: Kural {rulePercent:P0} (x{_ruleWeight}) + ML {result.MLResult.PredictedScore:F0} (x{_mlWeight})");
            string finalDurum = result.FinalVerdict == "Good" ? "BASARILI" : "BASARISIZ";
            Console.WriteLine($"  FINAL KARAR: {result.FinalVerdict} ({finalDurum})");
            Console.WriteLine("+------------------------------------------+");
        }
    }
}
