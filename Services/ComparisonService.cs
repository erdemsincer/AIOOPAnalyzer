using System;
using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    public class ComparisonService
    {
        public void Compare(AnalysisResult reference, AnalysisResult ai)
        {
            foreach (var refRule in reference.RuleResults)
            {
                var aiRule = ai.RuleResults
                    .FirstOrDefault(r => r.RuleName == refRule.RuleName);

                if (aiRule == null) continue;

                int diff = aiRule.Score - refRule.Score;
                string status = diff >= 0 ? "[OK]" : "[EKSIK]";
                string diffStr = diff >= 0 ? $"+{diff}" : $"{diff}";

                Console.WriteLine($"      {status} {refRule.RuleName}: AI {aiRule.Score} vs Ref {refRule.Score} ({diffStr})");
            }
        }
    }
}
