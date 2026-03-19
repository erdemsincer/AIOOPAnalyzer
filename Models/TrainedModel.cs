using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AIOOPAnalyzer.Models
{
    /// <summary>
    /// Eğitilmiş model verisi — JSON'a kaydedilir / yüklenir.
    /// Her eğitim örneği feature vektörü + label + score olarak saklanır.
    /// Tahmin: Weighted k-NN ile en benzer örnekleri bulup ağırlıklı ortalama alır.
    /// </summary>
    public class TrainedModel
    {
        public string ModelName { get; set; } = "AIOOPAnalyzer-Model";
        public string TrainedDate { get; set; } = "";
        public int TrainingSampleCount { get; set; }
        public List<string> FeatureNames { get; set; } = new();

        /// <summary>Eğitim örnekleri — her biri bir feature vektörü</summary>
        public List<TrainingSample> Samples { get; set; } = new();

        /// <summary>Her özellik için ortalama (normalizasyon)</summary>
        public List<double> FeatureMeans { get; set; } = new();
        /// <summary>Her özellik için standart sapma (normalizasyon)</summary>
        public List<double> FeatureStdDevs { get; set; } = new();

        /// <summary>Her özelliğin kalite skoruna katkısını gösteren ağırlıklar</summary>
        public List<double> FeatureWeights { get; set; } = new();
    }

    public class TrainingSample
    {
        public string Id { get; set; } = "";
        /// <summary>Normalize edilmiş özellik vektörü</summary>
        public List<double> Features { get; set; } = new();
        /// <summary>"Good" = 1, "Bad" = 0</summary>
        public int Label { get; set; }
        /// <summary>0-100 arası kalite skoru</summary>
        public int QualityScore { get; set; }
    }
}
