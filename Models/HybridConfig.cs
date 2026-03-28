namespace AIOOPAnalyzer.Models
{
    /// <summary>
    /// Hibrit kalite skoru Q = w_k * K + w_m * M ve nihai Good/Bad karari icin ayarlar.
    /// K: kural skorunun maksimuma orani (0-100), M: k-NN tahmin skoru (0-100).
    /// </summary>
    public class HybridConfig
    {
        /// <summary>Kural yuzdesinin agirligi (varsayilan 0.6).</summary>
        public double RuleWeight { get; set; } = 0.6;

        /// <summary>ML skorunun agirligi (varsayilan 0.4).</summary>
        public double MLWeight { get; set; } = 0.4;

        /// <summary>
        /// Birlesik skor Q icin esik: yuksek uyum dallarinda son kararda da kullanilir (varsayilan 65).
        /// Kural-tek basina karsilastirma (ablation) icin: K &gt;= QualityThreshold ise Good.
        /// </summary>
        public int QualityThreshold { get; set; } = 65;

        /// <summary>K ve ML'nin ikisi de "iyi" dedigi bolge: K &gt;= bu deger ve ML Good ise Good (varsayilan 70).</summary>
        public int StrongAgreementHighRulePercent { get; set; } = 70;

        /// <summary>K ve ML'nin ikisi de "kotu" dedigi bolge: K &lt; bu deger ve ML Bad ise Bad (varsayilan 50).</summary>
        public int StrongAgreementLowRulePercent { get; set; } = 50;
    }
}
