using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;
using AIOOPAnalyzer.Analyzers;

namespace AIOOPAnalyzer.Services
{
    public class AnalyzerService
    {
        private readonly Dictionary<string, IAnalyzer> _analyzers;
        private readonly RulesConfig _config;

        public AnalyzerService(RulesConfig config)
        {
            _config = config;

            _analyzers = new Dictionary<string, IAnalyzer>
            {
                { "Encapsulation", new EncapsulationAnalyzer() },
                { "SRP", new SRPAnalyzer() },
                { "Dependency Injection", new DIAnalyzer() },
                { "Interfaces", new InterfaceAnalyzer() },
                { "Inheritance", new InheritanceAnalyzer() },
                { "Polymorphism", new PolymorphismAnalyzer() }
            };
        }

        public AnalysisResult Run(CodeStructure code)
        {
            var finalResult = new AnalysisResult();

            foreach (var ruleDef in _config.Rules)
            {
                if (_analyzers.TryGetValue(ruleDef.RuleName, out var analyzer))
                {
                    var result = analyzer.Analyze(code, ruleDef);
                    finalResult.Issues.AddRange(result.Issues);
                    finalResult.RuleResults.AddRange(result.RuleResults);
                }
            }

            // toplam hesapla
            foreach (var rule in finalResult.RuleResults)
            {
                finalResult.TotalScore += rule.Score;
                finalResult.MaxScore += rule.MaxScore;
            }

            return finalResult;
        }
    }
}