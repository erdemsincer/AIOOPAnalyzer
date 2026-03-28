using System;
using System.Collections.Generic;
using System.Linq;
using AIOOPAnalyzer.Models;

namespace AIOOPAnalyzer.Services
{
    /// <summary>
    /// Batch veri seti uzerinde ML / kural / hibrit tahminlerinin istatistiksel raporu.
    /// </summary>
    public static class BatchEvaluationMetrics
    {
        public const string Good = "Good";
        public const string Bad = "Bad";

        /// <summary>
        /// Gercek etiket (Good/Bad) ve tahmin ikililerinden TP, FP, FN, TN.
        /// TP: Gercek Good, tahmin Good | FN: Gercek Good, tahmin Bad | FP: Gercek Bad, tahmin Good | TN: Gercek Bad, tahmin Bad
        /// </summary>
        public static (int TP, int FP, int FN, int TN) ConfusionMatrix(IEnumerable<(string Actual, string Predicted)> rows)
        {
            int tp = 0, fp = 0, fn = 0, tn = 0;
            foreach (var (actual, predicted) in rows)
            {
                bool aGood = actual == Good;
                bool pGood = predicted == Good;
                if (aGood && pGood) tp++;
                else if (aGood && !pGood) fn++;
                else if (!aGood && pGood) fp++;
                else tn++;
            }
            return (tp, fp, fn, tn);
        }

        public static double Accuracy(int tp, int fp, int fn, int tn)
        {
            int n = tp + fp + fn + tn;
            return n == 0 ? 0 : (double)(tp + tn) / n;
        }

        /// <summary>Gercek Good orneklerinin ne kadarini Good bulduk (duyarlilik - Good sinifi).</summary>
        public static double RecallGood(int tp, int fn)
        {
            int d = tp + fn;
            return d == 0 ? 0 : (double)tp / d;
        }

        /// <summary>Gercek Bad orneklerinin ne kadarini Bad bulduk.</summary>
        public static double RecallBad(int fp, int tn)
        {
            int d = fp + tn;
            return d == 0 ? 0 : (double)tn / d;
        }

        public static double PrecisionGood(int tp, int fp)
        {
            int d = tp + fp;
            return d == 0 ? 0 : (double)tp / d;
        }

        public static double PrecisionBad(int fn, int tn)
        {
            int d = fn + tn;
            return d == 0 ? 0 : (double)tn / d;
        }

        public static void PrintBlock(string title, (int TP, int FP, int FN, int TN) m, int total)
        {
            var (tp, fp, fn, tn) = m;
            double acc = Accuracy(tp, fp, fn, tn);
            Console.WriteLine($"  --- {title} ---");
            Console.WriteLine($"                  Tahmin Good    Tahmin Bad");
            Console.WriteLine($"  Gercek Good     {tp,6}         {fn,6}");
            Console.WriteLine($"  Gercek Bad      {fp,6}         {tn,6}");
            Console.WriteLine($"  Dogruluk (accuracy): {acc:P1}  ({tp + tn}/{total})");
            Console.WriteLine($"  Good — Anma (recall): {RecallGood(tp, fn):P1}  Kesinlik (precision): {PrecisionGood(tp, fp):P1}");
            Console.WriteLine($"  Bad  — Anma (recall): {RecallBad(fp, tn):P1}  Kesinlik (precision): {PrecisionBad(fn, tn):P1}");
            Console.WriteLine();
        }

        /// <summary>Kural yuzdesi (0-100) ile tek basina siniflama: K &gt;= esik ise Good.</summary>
        public static string PredictRuleOnly(double rulePercent100, HybridConfig h)
        {
            return rulePercent100 >= h.QualityThreshold ? Good : Bad;
        }

        public static void PrintHybridFormulaNote(HybridConfig h)
        {
            Console.WriteLine("  [KALITE TANIMI — config/hybrid.json]");
            Console.WriteLine($"  Q = {h.RuleWeight} * K + {h.MLWeight} * M   (K: kural %, M: k-NN skoru 0-100)");
            Console.WriteLine($"  Esik (qualityThreshold): {h.QualityThreshold}");
            Console.WriteLine($"  Guclu uyum: K >= {h.StrongAgreementHighRulePercent} ve ML Good => Good; K < {h.StrongAgreementLowRulePercent} ve ML Bad => Bad;");
            Console.WriteLine($"              aksi halde Q >= {h.QualityThreshold} ise Good, degilse Bad.");
            Console.WriteLine($"  Ablation — yalniz kural: Good <=> K >= {h.QualityThreshold}");
            Console.WriteLine($"  Ablation — yalniz ML:   Good <=> k-NN etiketi Good");
            Console.WriteLine();
        }
    }
}
