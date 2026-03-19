using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    public static class DatasetLoader
    {
        public static List<DatasetItem> Load(string path = "data/dataset.json")
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<DatasetItem>>(json, options)
                   ?? new List<DatasetItem>();
        }
    }
}
