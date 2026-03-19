namespace AIOOPAnalyzer.Models
{
    /// <summary>
    /// ML modelinin tahmin sonucu.
    /// </summary>
    public class PredictionResult
    {
        /// <summary>"Good" veya "Bad"</summary>
        public string PredictedLabel { get; set; } = "";
        /// <summary>0.0 - 1.0 arası güven skoru</summary>
        public double Confidence { get; set; }
        /// <summary>0-100 arası tahmin edilen kalite skoru</summary>
        public double PredictedScore { get; set; }
        /// <summary>k-NN'de kullanılan en yakın örnek sayısı</summary>
        public int K { get; set; }
        /// <summary>En yakın örneklerin Id'leri</summary>
        public string[] NearestNeighbors { get; set; } = System.Array.Empty<string>();
    }
}
