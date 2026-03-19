using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public class DIAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;

            foreach (var cls in code.Classes)
            {
                foreach (var creation in cls.ObjectCreations)
                {
                    result.Issues.Add($"[Bagimlilik Enjeksiyonu] '{cls.Name}' sinifi '{creation}' nesnesini 'new' ile olusturuyor. Constructor injection kullanilmali.");
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