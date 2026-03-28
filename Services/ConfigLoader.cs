using System.IO;
using System.Text.Json;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    public static class ConfigLoader
    {
        public static RulesConfig LoadRules(string path = "config/rules.json")
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RulesConfig>(json, options)
                   ?? new RulesConfig();
        }

        public static HybridConfig LoadHybrid(string path = "config/hybrid.json")
        {
            if (!File.Exists(path))
                return new HybridConfig();
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<HybridConfig>(json, options)
                   ?? new HybridConfig();
        }
    }
}
