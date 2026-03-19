namespace AIOOPAnalyzer.Models
{
    /// <summary>
    /// Bir koddan çıkarılan sayısal özellikler — ML eğitimi için kullanılır.
    /// Her özellik 0 veya pozitif sayı. Boolean özellikler 0/1.
    /// </summary>
    public class TrainingFeatures
    {
        // ── SINIF BİLGİLERİ ──
        public int ClassCount { get; set; }
        public int TotalMethodCount { get; set; }
        public int TotalFieldCount { get; set; }
        public int TotalPropertyCount { get; set; }

        // ── ENCAPSULATION ──
        public int PublicFieldCount { get; set; }
        public int PrivateFieldCount { get; set; }
        /// <summary>0.0 - 1.0 arası: PrivateFieldCount / TotalFieldCount</summary>
        public double EncapsulationRatio { get; set; }

        // ── SRP ──
        /// <summary>Sınıf başına ortalama metod sayısı</summary>
        public double AvgMethodsPerClass { get; set; }
        /// <summary>Threshold'u aşan sınıf sayısı</summary>
        public int ClassesExceedingMethodThreshold { get; set; }

        // ── DEPENDENCY INJECTION ──
        public int ObjectCreationCount { get; set; }
        /// <summary>new ile nesne oluşturuyor mu? 1 = evet (kötü), 0 = hayır (iyi)</summary>
        public int HasDirectInstantiation { get; set; }

        // ── INTERFACES ──
        public int InterfaceImplementationCount { get; set; }
        /// <summary>En az bir interface implement eden sınıf sayısı</summary>
        public int ClassesWithInterface { get; set; }
        /// <summary>Interface implement etmeyen sınıf sayısı</summary>
        public int ClassesWithoutInterface { get; set; }
        /// <summary>0.0 - 1.0 arası: ClassesWithInterface / ClassCount</summary>
        public double InterfaceRatio { get; set; }

        // ── INHERITANCE ──
        public int InheritanceCount { get; set; }
        /// <summary>BaseList'i olan sınıf sayısı</summary>
        public int ClassesWithInheritance { get; set; }

        // ── POLYMORPHISM ──
        public int VirtualMethodCount { get; set; }
        public int OverrideMethodCount { get; set; }
        /// <summary>virtual veya override kullanan sınıf sayısı</summary>
        public int ClassesWithPolymorphism { get; set; }

        // ── EĞİTİM HEDEFLERİ (dataset'ten gelir, tahmin sırasında boş) ──
        /// <summary>"Good" = 1, "Bad" = 0 — sınıflandırma hedefi</summary>
        public int LabelNumeric { get; set; }
        /// <summary>0-100 arası kalite skoru — regresyon hedefi</summary>
        public int QualityScore { get; set; }

        /// <summary>Özellikleri double[] olarak döndürür (ML hesaplamaları için)</summary>
        public double[] ToArray()
        {
            return new double[]
            {
                ClassCount,
                TotalMethodCount,
                TotalFieldCount,
                TotalPropertyCount,
                PublicFieldCount,
                PrivateFieldCount,
                EncapsulationRatio,
                AvgMethodsPerClass,
                ClassesExceedingMethodThreshold,
                ObjectCreationCount,
                HasDirectInstantiation,
                InterfaceImplementationCount,
                ClassesWithInterface,
                ClassesWithoutInterface,
                InterfaceRatio,
                InheritanceCount,
                ClassesWithInheritance,
                VirtualMethodCount,
                OverrideMethodCount,
                ClassesWithPolymorphism
            };
        }

        /// <summary>Özellik isimlerini döndürür (raporlama için)</summary>
        public static string[] FeatureNames => new[]
        {
            "ClassCount",
            "TotalMethodCount",
            "TotalFieldCount",
            "TotalPropertyCount",
            "PublicFieldCount",
            "PrivateFieldCount",
            "EncapsulationRatio",
            "AvgMethodsPerClass",
            "ClassesExceedingMethodThreshold",
            "ObjectCreationCount",
            "HasDirectInstantiation",
            "InterfaceImplementationCount",
            "ClassesWithInterface",
            "ClassesWithoutInterface",
            "InterfaceRatio",
            "InheritanceCount",
            "ClassesWithInheritance",
            "VirtualMethodCount",
            "OverrideMethodCount",
            "ClassesWithPolymorphism"
        };
    }
}
