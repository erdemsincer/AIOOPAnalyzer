using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public class SRPAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;
            int threshold = rule.Threshold > 0 ? rule.Threshold : 2;

            foreach (var cls in code.Classes)
            {
                if (cls.Methods.Count > threshold)
                {
                    result.Issues.Add($"[Tek Sorumluluk] '{cls.Name}' sinifi cok fazla metod iceriyor ({cls.Methods.Count} metod, esik: {threshold}).");
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