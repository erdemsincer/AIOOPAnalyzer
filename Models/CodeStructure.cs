using System.Collections.Generic;

namespace AIOOPAnalyzer.Models
{
    public class CodeStructure
    {
        public List<ClassInfo> Classes { get; set; } = new();
    }

    public class ClassInfo
    {
        public string Name { get; set; } = "";
        public List<FieldInfo> Fields { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> ObjectCreations { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public List<MethodInfo> Methods { get; set; } = new();
    }

    public class FieldInfo
    {
        public string Name { get; set; } = "";
        public bool IsPublic { get; set; }
    }

    public class MethodInfo
    {
        public string Name { get; set; } = "";
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
    }
}