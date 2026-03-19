using System.Collections.Generic;

namespace AIOOPAnalyzer.Models
{
    public class RulesConfig
    {
        public double PassThreshold { get; set; } = 0.7;
        public List<RuleDefinition> Rules { get; set; } = new();
    }

    public class RuleDefinition
    {
        public string RuleName { get; set; } = "";
        public int MaxScore { get; set; }
        public int PenaltyPerViolation { get; set; }
        public string Description { get; set; } = "";
        public int Threshold { get; set; } // opsiyonel (SRP gibi kurallar için)
    }
}
