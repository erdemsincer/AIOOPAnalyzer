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
    }
}
