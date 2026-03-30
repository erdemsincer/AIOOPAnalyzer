using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    /// <summary>
    /// CK Metriklerini kural bazlı analiz olarak çalıştırır.
    /// Her metrik için eşik değerleri aşan sınıflara ceza verir.
    /// </summary>
    public class CKMetricsRuleAnalyzer : IAnalyzer
    {
        private readonly CKMetricsAnalyzer _ckAnalyzer = new();

        // ── Varsayılan eşik değerleri ──
        private const int WMC_THRESHOLD = 10;
        private const int DIT_THRESHOLD = 3;
        private const int NOC_THRESHOLD = 5;
        private const int CBO_THRESHOLD = 5;
        private const int RFC_THRESHOLD = 20;
        private const int LCOM_THRESHOLD = 3;

        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();
            int max = rule.MaxScore;
            int score = max;

            var allMetrics = _ckAnalyzer.Calculate(code);

            // Her sınıf için CK metriklerini kontrol et
            foreach (var metrics in allMetrics)
            {
                // WMC kontrolü
                if (metrics.WMC > WMC_THRESHOLD)
                {
                    result.Issues.Add($"[CK-WMC] '{metrics.ClassName}' sinifinin WMC degeri cok yuksek ({metrics.WMC}, esik: {WMC_THRESHOLD}). Sinif cok karmasik.");
                    score -= rule.PenaltyPerViolation;
                }

                // DIT kontrolü
                if (metrics.DIT > DIT_THRESHOLD)
                {
                    result.Issues.Add($"[CK-DIT] '{metrics.ClassName}' sinifinin kalitim derinligi cok fazla ({metrics.DIT}, esik: {DIT_THRESHOLD}). Derin kalitim bakim zorlaştirir.");
                    score -= rule.PenaltyPerViolation;
                }

                // CBO kontrolü
                if (metrics.CBO > CBO_THRESHOLD)
                {
                    result.Issues.Add($"[CK-CBO] '{metrics.ClassName}' sinifi cok fazla diger sinifa bagimli ({metrics.CBO}, esik: {CBO_THRESHOLD}). Siki baglilik var.");
                    score -= rule.PenaltyPerViolation;
                }

                // RFC kontrolü
                if (metrics.RFC > RFC_THRESHOLD)
                {
                    result.Issues.Add($"[CK-RFC] '{metrics.ClassName}' sinifinin yanit sayisi cok yuksek ({metrics.RFC}, esik: {RFC_THRESHOLD}). Sinif cok fazla is yapiyor.");
                    score -= rule.PenaltyPerViolation;
                }

                // LCOM kontrolü
                if (metrics.LCOM > LCOM_THRESHOLD)
                {
                    result.Issues.Add($"[CK-LCOM] '{metrics.ClassName}' sinifinda uyumsuzluk yuksek ({metrics.LCOM}, esik: {LCOM_THRESHOLD}). Metotlar farkli alanlarla calisiyor, sinif bolunsun.");
                    score -= rule.PenaltyPerViolation;
                }
            }

            if (score < 0) score = 0;

            result.RuleResults.Add(new RuleResult
            {
                RuleName = rule.RuleName,
                Score = score,
                MaxScore = max
            });

            return result;
        }
    }
}
