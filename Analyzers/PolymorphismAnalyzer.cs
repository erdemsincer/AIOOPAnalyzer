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

            foreach (var cls in code.Classes)
            {
                foreach (var method in cls.Methods)
                {
                    if (method.IsOverride)
                    {
                        result.Issues.Add($"[Polimorfizm] '{cls.Name}' sinifindaki '{method.Name}' metodu override kullaniyor. Base siniftaki virtual metod dogrulanmali.");
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