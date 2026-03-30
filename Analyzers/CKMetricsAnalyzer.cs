using System;
using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Analyzers
{
    /// <summary>
    /// Chidamber & Kemerer (CK) Nesne Yönelimli Metriklerini hesaplar.
    /// 6 metrik: WMC, DIT, NOC, CBO, RFC, LCOM
    /// </summary>
    public class CKMetricsAnalyzer
    {
        /// <summary>
        /// Tüm sınıflar için CK metriklerini hesaplar.
        /// </summary>
        public List<CKMetrics> Calculate(CodeStructure code)
        {
            var results = new List<CKMetrics>();

            foreach (var cls in code.Classes)
            {
                var metrics = new CKMetrics
                {
                    ClassName = cls.Name,
                    WMC = CalculateWMC(cls),
                    DIT = CalculateDIT(cls, code),
                    NOC = CalculateNOC(cls, code),
                    CBO = CalculateCBO(cls),
                    RFC = CalculateRFC(cls),
                    LCOM = CalculateLCOM(cls)
                };
                results.Add(metrics);
            }

            return results;
        }

        /// <summary>
        /// Kod yapısından ortalama CK metriklerini hesaplar.
        /// </summary>
        public CKMetrics CalculateAverage(CodeStructure code)
        {
            var all = Calculate(code);
            if (all.Count == 0)
                return new CKMetrics { ClassName = "(ortalama)" };

            return new CKMetrics
            {
                ClassName = "(ortalama)",
                WMC = (int)Math.Round(all.Average(m => m.WMC)),
                DIT = all.Max(m => m.DIT), // DIT için max alıyoruz
                NOC = (int)Math.Round(all.Average(m => m.NOC)),
                CBO = (int)Math.Round(all.Average(m => m.CBO)),
                RFC = (int)Math.Round(all.Average(m => m.RFC)),
                LCOM = (int)Math.Round(all.Average(m => m.LCOM))
            };
        }

        // ══════════════════════════════════════════
        // 1. WMC — Weighted Methods per Class
        // ══════════════════════════════════════════
        /// <summary>
        /// Sınıf başına ağırlıklı metod: Tüm metotların complexity toplamı.
        /// </summary>
        private int CalculateWMC(ClassInfo cls)
        {
            if (cls.Methods.Count == 0) return 0;
            return cls.Methods.Sum(m => Math.Max(1, m.Complexity));
        }

        // ══════════════════════════════════════════
        // 2. DIT — Depth of Inheritance Tree
        // ══════════════════════════════════════════
        /// <summary>
        /// Kalıtım ağacının derinliği. Recursive olarak parent zincirini takip eder.
        /// </summary>
        private int CalculateDIT(ClassInfo cls, CodeStructure code)
        {
            int depth = 0;
            string currentBase = cls.BaseClassName;

            // Maximum 10 seviye (sonsuz döngü koruması)
            int maxDepth = 10;
            while (!string.IsNullOrEmpty(currentBase) && depth < maxDepth)
            {
                depth++;
                var parentClass = code.Classes.FirstOrDefault(c => c.Name == currentBase);
                if (parentClass == null) break;
                currentBase = parentClass.BaseClassName;
            }

            return depth;
        }

        // ══════════════════════════════════════════
        // 3. NOC — Number of Children
        // ══════════════════════════════════════════
        /// <summary>
        /// Bu sınıftan doğrudan türeyen sınıf sayısı.
        /// </summary>
        private int CalculateNOC(ClassInfo cls, CodeStructure code)
        {
            return code.Classes.Count(c => c.BaseClassName == cls.Name);
        }

        // ══════════════════════════════════════════
        // 4. CBO — Coupling Between Object Classes
        // ══════════════════════════════════════════
        /// <summary>
        /// Sınıfın bağımlı olduğu benzersiz tip sayısı.
        /// Field tipleri, parametre tipleri, new ile oluşturulan tipler, interface/base class.
        /// </summary>
        private int CalculateCBO(ClassInfo cls)
        {
            var coupledTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Base class
            if (!string.IsNullOrEmpty(cls.BaseClassName))
                coupledTypes.Add(cls.BaseClassName);

            // Interfaces
            foreach (var iface in cls.Interfaces)
                coupledTypes.Add(iface);

            // Object creations (new X())
            foreach (var creation in cls.ObjectCreations)
                coupledTypes.Add(creation);

            // Referenced types (parametreler, field tipleri vb.)
            foreach (var refType in cls.ReferencedTypes)
                coupledTypes.Add(refType);

            // Kendi adını çıkar
            coupledTypes.Remove(cls.Name);

            // Primitive tipleri çıkar
            var primitives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "string", "int", "double", "float", "bool", "decimal",
                "long", "short", "byte", "char", "void", "object",
                "String", "Int32", "Int64", "Double", "Boolean", "Decimal"
            };
            coupledTypes.ExceptWith(primitives);

            return coupledTypes.Count;
        }

        // ══════════════════════════════════════════
        // 5. RFC — Response for a Class
        // ══════════════════════════════════════════
        /// <summary>
        /// Sınıftaki metod sayısı + bu metotların çağırdığı benzersiz metod sayısı.
        /// </summary>
        private int CalculateRFC(ClassInfo cls)
        {
            // Sınıfın kendi metot sayısı
            int ownMethods = cls.Methods.Count;

            // Tüm metotların çağırdığı benzersiz metotlar
            var calledMethods = new HashSet<string>();
            foreach (var method in cls.Methods)
            {
                foreach (var called in method.CalledMethods)
                {
                    // Kendi metotlarını çıkar (sadece dış çağrılar)
                    if (!cls.Methods.Any(m => m.Name == called))
                        calledMethods.Add(called);
                }
            }

            return ownMethods + calledMethods.Count;
        }

        // ══════════════════════════════════════════
        // 6. LCOM — Lack of Cohesion of Methods
        // ══════════════════════════════════════════
        /// <summary>
        /// LCOM = max(0, P - Q)
        /// P: Ortak alan paylaşmayan metot çiftlerinin sayısı
        /// Q: Ortak alan paylaşan metot çiftlerinin sayısı
        /// </summary>
        private int CalculateLCOM(ClassInfo cls)
        {
            var methods = cls.Methods;
            if (methods.Count <= 1) return 0;

            int p = 0; // ortak alanı olmayan çiftler
            int q = 0; // ortak alanı olan çiftler

            for (int i = 0; i < methods.Count; i++)
            {
                for (int j = i + 1; j < methods.Count; j++)
                {
                    var fieldsI = new HashSet<string>(methods[i].AccessedFields);
                    var fieldsJ = new HashSet<string>(methods[j].AccessedFields);

                    // Kesişim var mı?
                    if (fieldsI.Overlaps(fieldsJ))
                        q++;
                    else
                        p++;
                }
            }

            return Math.Max(0, p - q);
        }
    }
}
