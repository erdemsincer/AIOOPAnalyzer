using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public class InheritanceAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;

            foreach (var cls in code.Classes)
            {
                // BaseClassName bos ise bu sinif hicbir base class'tan turetilmemis
                if (string.IsNullOrEmpty(cls.BaseClassName))
                {
                    result.Issues.Add($"[Kalitim] '{cls.Name}' sinifi hicbir base siniftan turetilmemis.");
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