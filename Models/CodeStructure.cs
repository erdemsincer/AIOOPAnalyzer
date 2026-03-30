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
        /// <summary>Base class ismi (kalitim). Bos ise turetilmemis.</summary>
        public string BaseClassName { get; set; } = "";
        public List<MethodInfo> Methods { get; set; } = new();

        // ── CK METRİKLERİ İÇİN ──
        /// <summary>Bu sınıfın bağımlı olduğu diğer sınıf/tip isimleri (CBO hesabı için)</summary>
        public List<string> ReferencedTypes { get; set; } = new();
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

        // ── CK METRİKLERİ İÇİN ──
        /// <summary>Cyclomatic complexity (if/else/for/while/switch/case/&amp;&amp;/|| sayısı + 1)</summary>
        public int Complexity { get; set; } = 1;
        /// <summary>Parametre sayısı</summary>
        public int ParameterCount { get; set; }
        /// <summary>Bu metod içinde çağrılan metod isimleri</summary>
        public List<string> CalledMethods { get; set; } = new();
        /// <summary>Bu metod içinde erişilen alan (field) isimleri</summary>
        public List<string> AccessedFields { get; set; } = new();
    }
}