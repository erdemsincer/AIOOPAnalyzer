using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    /// <summary>
    /// Eğitilmiş modeli yükler ve yeni bir koda tahmin yapar.
    /// Algoritma: Weighted k-NN — en yakın k örneğe bakıp ağırlıklı ortalama alır.
    /// </summary>
    public class ModelPredictor
    {
        private readonly TrainedModel _model;
        private readonly CodeParserService _parser;
        private readonly FeatureExtractor _extractor;
        private readonly int _k;

        public ModelPredictor(string modelPath = "models/model.json", int k = 3)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model dosyası bulunamadı: {modelPath}");

            var json = File.ReadAllText(modelPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _model = JsonSerializer.Deserialize<TrainedModel>(json, options)
                     ?? throw new InvalidOperationException("Model yüklenemedi!");

            _parser = new CodeParserService();
            _extractor = new FeatureExtractor();
            _k = Math.Min(k, _model.Samples.Count);
        }

        /// <summary>
        /// C# kaynak kodunu analiz edip tahmin döndürür.
        /// </summary>
        public PredictionResult Predict(string sourceCode)
        {
            // 1) Kodu parse et → özellik çıkar
            var code = _parser.Parse(sourceCode);
            var features = _extractor.Extract(code);
            var rawFeatures = features.ToArray();

            // 2) Normalize et (eğitimde kullanılan ortalama/std ile)
            var normalized = Normalize(rawFeatures);

            // 3) k-NN: tüm eğitim örneklerine mesafe hesapla
            var distances = _model.Samples
                .Select(s => new
                {
                    Sample = s,
                    Distance = WeightedEuclideanDistance(normalized, s.Features.ToArray())
                })
                .OrderBy(x => x.Distance)
                .Take(_k)
                .ToList();

            // 4) Ağırlıklı oylama (mesafeye ters ağırlık)
            double totalWeight = 0;
            double weightedLabelSum = 0;
            double weightedScoreSum = 0;
            var neighborIds = new List<string>();

            foreach (var neighbor in distances)
            {
                double weight = 1.0 / (neighbor.Distance + 0.0001); // 0'a bölmeyi önle
                totalWeight += weight;
                weightedLabelSum += weight * neighbor.Sample.Label;
                weightedScoreSum += weight * neighbor.Sample.QualityScore;
                neighborIds.Add(neighbor.Sample.Id);
            }

            double labelProbability = weightedLabelSum / totalWeight; // 0.0 - 1.0
            double predictedScore = weightedScoreSum / totalWeight;

            // 5) Sonuç
            return new PredictionResult
            {
                PredictedLabel = labelProbability >= 0.5 ? "Good" : "Bad",
                Confidence = labelProbability >= 0.5 ? labelProbability : 1.0 - labelProbability,
                PredictedScore = Math.Round(predictedScore, 1),
                K = _k,
                NearestNeighbors = neighborIds.ToArray()
            };
        }

        /// <summary>
        /// Özellikleri detaylı gösterir (debug/raporlama için).
        /// </summary>
        public TrainingFeatures ExtractFeatures(string sourceCode)
        {
            var code = _parser.Parse(sourceCode);
            return _extractor.Extract(code);
        }

        private double[] Normalize(double[] features)
        {
            var result = new double[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                double mean = i < _model.FeatureMeans.Count ? _model.FeatureMeans[i] : 0;
                double std = i < _model.FeatureStdDevs.Count ? _model.FeatureStdDevs[i] : 1;
                result[i] = (features[i] - mean) / std;
            }
            return result;
        }

        private double WeightedEuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                double w = i < _model.FeatureWeights.Count ? Math.Abs(_model.FeatureWeights[i]) : 1.0;
                double diff = a[i] - b[i];
                sum += w * diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
