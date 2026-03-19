using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public class InterfaceAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;

            foreach (var cls in code.Classes)
            {
                if (cls.Interfaces.Count == 0)
                {
                    result.Issues.Add($"[Arayuz] '{cls.Name}' sinifi hicbir interface implement etmiyor.");
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