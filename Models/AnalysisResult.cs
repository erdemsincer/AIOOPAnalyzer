using System.Collections.Generic;

namespace AIOOPAnalyzer.Models
{
    public class AnalysisResult
    {
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }

        public List<string> Issues { get; set; } = new();
        public List<RuleResult> RuleResults { get; set; } = new();
    }
}