using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    /// <summary>
    /// Veri setinden ozellik cikarip ML modeli egitir ve JSON olarak kaydeder.
    /// Algoritma: Normalize + Feature Importance + k-NN tabanli.
    /// </summary>
    public class ModelTrainer
    {
        private readonly CodeParserService _parser;
        private readonly FeatureExtractor _extractor;

        public ModelTrainer()
        {
            _parser = new CodeParserService();
            _extractor = new FeatureExtractor();
        }

        /// <summary>
        /// Veri setini egitir ve modeli belirtilen dosyaya kaydeder.
        /// </summary>
        public TrainedModel Train(List<DatasetItem> dataset, string modelPath = "models/model.json")
        {
            Console.WriteLine("[EGITIM] MODEL EGITIMI BASLADI\n");

            // 1) Her örnekten feature çıkar
            var rawSamples = new List<(string Id, double[] Features, int Label, int Score)>();

            foreach (var item in dataset)
            {
                if (string.IsNullOrEmpty(item.Code) || string.IsNullOrEmpty(item.Label))
                {
                    Console.WriteLine($"   [UYARI] Atlaniyor: #{item.Id} (Code veya Label bos)");
                    continue;
                }

                var code = _parser.Parse(item.Code);
                var features = _extractor.ExtractLabeled(code, item);
                rawSamples.Add((item.Id, features.ToArray(), features.LabelNumeric, features.QualityScore));
            }

            if (rawSamples.Count < 2)
            {
                throw new InvalidOperationException("En az 2 egitim ornegi gerekli!");
            }

            Console.WriteLine($"   [BILGI] {rawSamples.Count} ornek islendi");

            // 2) Normalizasyon: z-score (ortalama=0, std=1)
            int featureCount = rawSamples[0].Features.Length;
            var means = new double[featureCount];
            var stdDevs = new double[featureCount];

            for (int f = 0; f < featureCount; f++)
            {
                var values = rawSamples.Select(s => s.Features[f]).ToArray();
                means[f] = values.Average();
                double variance = values.Average(v => (v - means[f]) * (v - means[f]));
                stdDevs[f] = Math.Sqrt(variance);
                if (stdDevs[f] < 0.0001) stdDevs[f] = 1.0; // sabit özelliği koru
            }

            Console.WriteLine("   [BILGI] Normalizasyon tamamlandi");

            // 3) Feature Importance hesapla: her ozelligin Good vs Bad ayrimina katkisi
            var weights = CalculateFeatureWeights(rawSamples, means, stdDevs, featureCount);

            Console.WriteLine("   [BILGI] Ozellik agirliklari hesaplandi:");
            var featureNames = TrainingFeatures.FeatureNames;
            for (int i = 0; i < featureCount; i++)
            {
                string bar = new string('#', (int)(Math.Abs(weights[i]) * 20));
                Console.WriteLine($"      {featureNames[i],-35} {weights[i]:+0.000;-0.000} {bar}");
            }

            // 4) Normalize edilmiş örnekleri modele yaz
            var model = new TrainedModel
            {
                ModelName = "AIOOPAnalyzer-Model-v1",
                TrainedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TrainingSampleCount = rawSamples.Count,
                FeatureNames = featureNames.ToList(),
                FeatureMeans = means.ToList(),
                FeatureStdDevs = stdDevs.ToList(),
                FeatureWeights = weights.ToList(),
                Samples = new List<TrainingSample>()
            };

            foreach (var sample in rawSamples)
            {
                var normalized = Normalize(sample.Features, means, stdDevs);
                model.Samples.Add(new TrainingSample
                {
                    Id = sample.Id,
                    Features = normalized.ToList(),
                    Label = sample.Label,
                    QualityScore = sample.Score
                });
            }

            // 5) Modeli kaydet
            var dir = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(modelPath, json);

            Console.WriteLine($"\n   [TAMAM] Model kaydedildi: {modelPath}");
            Console.WriteLine($"   [BILGI] Egitim ornekleri: {model.TrainingSampleCount}");
            Console.WriteLine($"      Good: {rawSamples.Count(s => s.Label == 1)}");
            Console.WriteLine($"      Bad:  {rawSamples.Count(s => s.Label == 0)}");

            // 6) Cross-validation (leave-one-out)
            RunCrossValidation(model);

            return model;
        }

        /// <summary>
        /// Her ozelligin Good/Bad ayrimina katkisini hesaplar.
        /// Basit yontem: Good ve Bad ortalamalari arasindaki fark.
        /// </summary>
        private double[] CalculateFeatureWeights(
            List<(string Id, double[] Features, int Label, int Score)> samples,
            double[] means, double[] stdDevs, int featureCount)
        {
            var weights = new double[featureCount];
            var goodSamples = samples.Where(s => s.Label == 1).ToList();
            var badSamples = samples.Where(s => s.Label == 0).ToList();

            if (goodSamples.Count == 0 || badSamples.Count == 0)
            {
                // Tek sinif varsa esit agirlik
                for (int i = 0; i < featureCount; i++)
                    weights[i] = 1.0 / featureCount;
                return weights;
            }

            for (int f = 0; f < featureCount; f++)
            {
                double goodMean = goodSamples.Average(s => (s.Features[f] - means[f]) / stdDevs[f]);
                double badMean = badSamples.Average(s => (s.Features[f] - means[f]) / stdDevs[f]);
                weights[f] = goodMean - badMean; // pozitif = Good icin yuksek olan ozellik
            }

            // Normalize et (toplam = 1)
            double totalAbs = weights.Sum(w => Math.Abs(w));
            if (totalAbs > 0)
            {
                for (int i = 0; i < featureCount; i++)
                    weights[i] /= totalAbs;
            }

            return weights;
        }

        private double[] Normalize(double[] features, double[] means, double[] stdDevs)
        {
            var result = new double[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                result[i] = (features[i] - means[i]) / stdDevs[i];
            }
            return result;
        }

        /// <summary>
        /// Leave-one-out cross validation -- her ornegi bir kez disarida birakip tahmin yap.
        /// </summary>
        private void RunCrossValidation(TrainedModel model)
        {
            Console.WriteLine("\n   [DOGRULAMA] CAPRAZ GECERLILIK (Leave-One-Out):");
            int correct = 0;
            int total = model.Samples.Count;
            double totalError = 0;

            for (int i = 0; i < total; i++)
            {
                var testSample = model.Samples[i];
                var trainSamples = model.Samples.Where((_, idx) => idx != i).ToList();

                // k-NN tahmin (k = min(3, trainSamples.Count))
                int k = Math.Min(3, trainSamples.Count);
                var distances = trainSamples
                    .Select(s => new
                    {
                        Sample = s,
                        Distance = EuclideanDistance(testSample.Features, s.Features, model.FeatureWeights)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(k)
                    .ToList();

                double avgLabel = distances.Average(d => d.Sample.Label);
                int predictedLabel = avgLabel >= 0.5 ? 1 : 0;
                double predictedScore = distances.Average(d => d.Sample.QualityScore);

                if (predictedLabel == testSample.Label) correct++;
                totalError += Math.Abs(predictedScore - testSample.QualityScore);
            }

            double accuracy = (double)correct / total;
            double avgError = totalError / total;
            Console.WriteLine($"      Dogruluk (Accuracy): {accuracy:P0} ({correct}/{total})");
            Console.WriteLine($"      Ortalama Skor Hatasi: {avgError:F1} puan");
        }

        private double EuclideanDistance(List<double> a, List<double> b, List<double> weights)
        {
            double sum = 0;
            for (int i = 0; i < a.Count && i < b.Count; i++)
            {
                double w = i < weights.Count ? Math.Abs(weights[i]) : 1.0;
                double diff = a[i] - b[i];
                sum += w * diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
