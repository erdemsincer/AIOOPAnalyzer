using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    /// <summary>
    /// CodeStructure'dan ML için sayısal özellikler çıkarır.
    /// </summary>
    public class FeatureExtractor
    {
        private readonly int _methodThreshold;

        public FeatureExtractor(int methodThreshold = 2)
        {
            _methodThreshold = methodThreshold;
        }

        /// <summary>
        /// Bir CodeStructure'dan TrainingFeatures çıkarır.
        /// </summary>
        public TrainingFeatures Extract(CodeStructure code)
        {
            var features = new TrainingFeatures();
            var classes = code.Classes;

            // ── SINIF BİLGİLERİ ──
            features.ClassCount = classes.Count;
            features.TotalMethodCount = classes.Sum(c => c.Methods.Count);
            features.TotalFieldCount = classes.Sum(c => c.Fields.Count);
            features.TotalPropertyCount = classes.Sum(c => c.Properties.Count);

            // ── ENCAPSULATION ──
            features.PublicFieldCount = classes.Sum(c => c.Fields.Count(f => f.IsPublic));
            features.PrivateFieldCount = features.TotalFieldCount - features.PublicFieldCount;
            features.EncapsulationRatio = features.TotalFieldCount > 0
                ? (double)features.PrivateFieldCount / features.TotalFieldCount
                : 1.0; // alan yoksa sorun yok → 1.0

            // ── SRP ──
            features.AvgMethodsPerClass = features.ClassCount > 0
                ? (double)features.TotalMethodCount / features.ClassCount
                : 0;
            features.ClassesExceedingMethodThreshold = classes
                .Count(c => c.Methods.Count > _methodThreshold);

            // ── DEPENDENCY INJECTION ──
            features.ObjectCreationCount = classes.Sum(c => c.ObjectCreations.Count);
            features.HasDirectInstantiation = features.ObjectCreationCount > 0 ? 1 : 0;

            // ── INTERFACES ──
            features.InterfaceImplementationCount = classes.Sum(c => c.Interfaces.Count);
            features.ClassesWithInterface = classes.Count(c => c.Interfaces.Count > 0);
            features.ClassesWithoutInterface = features.ClassCount - features.ClassesWithInterface;
            features.InterfaceRatio = features.ClassCount > 0
                ? (double)features.ClassesWithInterface / features.ClassCount
                : 0;

            // ── INHERITANCE ──
            features.InheritanceCount = classes.Count(c => c.Interfaces.Count > 0); // BaseList
            features.ClassesWithInheritance = features.InheritanceCount;

            // ── POLYMORPHISM ──
            features.VirtualMethodCount = classes.Sum(c => c.Methods.Count(m => m.IsVirtual));
            features.OverrideMethodCount = classes.Sum(c => c.Methods.Count(m => m.IsOverride));
            features.ClassesWithPolymorphism = classes
                .Count(c => c.Methods.Any(m => m.IsVirtual || m.IsOverride));

            return features;
        }

        /// <summary>
        /// Bir DatasetItem'dan etiketli TrainingFeatures çıkarır (eğitim için).
        /// </summary>
        public TrainingFeatures ExtractLabeled(CodeStructure code, DatasetItem item)
        {
            var features = Extract(code);
            features.LabelNumeric = item.Label == "Good" ? 1 : 0;
            features.QualityScore = item.QualityScore;
            return features;
        }
    }
}
