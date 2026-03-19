using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    public interface IAnalyzer
    {
        AnalysisResult Analyze(CodeStructure code, RuleDefinition rule);
    }
}