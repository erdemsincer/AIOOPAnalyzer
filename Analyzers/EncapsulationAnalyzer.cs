using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public class EncapsulationAnalyzer : IAnalyzer
    {
        public AnalysisResult Analyze(CodeStructure code, RuleDefinition rule)
        {
            var result = new AnalysisResult();

            int max = rule.MaxScore;
            int score = max;

            foreach (var cls in code.Classes)
            {
                foreach (var field in cls.Fields)
                {
                    if (field.IsPublic)
                    {
                        result.Issues.Add($"[Kapsulleme] '{cls.Name}' sinifindaki '{field.Name}' alani public tanimlanmis. Private veya property olmali.");
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