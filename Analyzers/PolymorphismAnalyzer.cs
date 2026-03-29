using AIOOPAnalyzer.Models;
using System.Linq;

namespace AIOOPAnalyzer.Analyzers
{
    public class PolymorphismAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;

            // Hic virtual veya override metod yoksa polimorfizm kullanilmiyor
            bool hasAnyPolymorphism = code.Classes.Any(c =>
                c.Methods.Any(m => m.IsVirtual || m.IsOverride));

            if (!hasAnyPolymorphism)
            {
                result.Issues.Add($"[Polimorfizm] Kodda hic virtual veya override metod bulunamadi. Polimorfizm kullanilmiyor.");
                score -= rule.PenaltyPerViolation;
            }

            // Base class'i olan ama override kullanmayan siniflar
            foreach (var cls in code.Classes)
            {
                if (!string.IsNullOrEmpty(cls.BaseClassName))
                {
                    bool hasOverride = cls.Methods.Any(m => m.IsOverride);
                    if (!hasOverride)
                    {
                        result.Issues.Add($"[Polimorfizm] '{cls.Name}' sinifi '{cls.BaseClassName}' siniftan turetilmis ama hicbir metodu override etmiyor.");
                        score -= rule.PenaltyPerViolation;
                    }
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