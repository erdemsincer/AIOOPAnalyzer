using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AIOOPAnalyzer.Models;
using AIOOPAnalyzer.Services;

class Program
{
    static int Main(string[] args)
    {
        // Pipeline modunda banner basma
        bool isPipeline = args.Any(a => a == "--pipeline" || a == "--json" || a == "--ci");
        bool jsonOutput = args.Any(a => a == "--json");
        var hybridDefaults = ConfigLoader.LoadHybrid("config/hybrid.json");
        int minScore = hybridDefaults.QualityThreshold;

        // --min-score=75 gibi parametre destegi
        var minScoreArg = args.FirstOrDefault(a => a.StartsWith("--min-score="));
        if (minScoreArg != null && int.TryParse(minScoreArg.Split('=')[1], out int parsed))
            minScore = parsed;

        // Pipeline degilse banner goster
        if (!isPipeline)
        {
            Console.WriteLine("+------------------------------------------+");
            Console.WriteLine("|         AI OOP ANALYZER v2.0             |");
            Console.WriteLine("|    Kural Bazli + ML Hibrit Analiz         |");
            Console.WriteLine("+------------------------------------------+\n");
        }

        string mode = args.Length > 0 ? args[0].ToLower() : "help";

        switch (mode)
        {
            case "train":
                RunTrain();
                return 0;
            case "analyze":
                string codePath = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "";
                if (isPipeline)
                    return RunPipeline(codePath, jsonOutput, minScore);
                else
                    RunAnalyze(codePath);
                return 0;
            case "batch":
                RunBatch();
                return 0;
            case "pipeline":
                string pipeFile = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "";
                return RunPipeline(pipeFile, jsonOutput, minScore);
            case "pr-check":
                // Degisen dosyalari toplu analiz et (GitHub Actions icin)
                var files = args.Skip(1).Where(a => !a.StartsWith("--")).ToList();
                string reportPath = args.FirstOrDefault(a => a.StartsWith("--report="))?.Split('=')[1] ?? "";
                return RunPRCheck(files, minScore, reportPath);
            default:
                PrintHelp();
                return 0;
        }
    }

    // ════════════════════════════════════════════
    // MOD 1: TRAIN — Dataset'ten model eğit
    // ════════════════════════════════════════════
    static void RunTrain()
    {
        Console.WriteLine("[MOD: EGITIM]\n");

        // 1) Dataset yukle ve dogrula
        var dataset = DatasetLoader.Load("data/dataset.json");
        var validator = new DatasetValidator();
        validator.Validate(dataset);

        // 2) Modeli egit
        var trainer = new ModelTrainer();
        var model = trainer.Train(dataset, "models/model.json");

        Console.WriteLine("\n[TAMAMLANDI] Egitim basariyla tamamlandi. Artik 'analyze' komutu ile kod analiz edebilirsiniz.");
    }

    // ════════════════════════════════════════════
    // MOD 2: ANALYZE — Tek bir kodu analiz et
    // ════════════════════════════════════════════
    static void RunAnalyze(string codePath)
    {
        Console.WriteLine("[MOD: ANALIZ]\n");

        // Model var mi kontrol et
        if (!File.Exists("models/model.json"))
        {
            Console.WriteLine("[HATA] Egitilmis model bulunamadi.");
            Console.WriteLine("       Once 'dotnet run train' komutu ile modeli egitin.");
            return;
        }

        // Kodu al
        string sourceCode;
        if (!string.IsNullOrEmpty(codePath) && File.Exists(codePath))
        {
            sourceCode = File.ReadAllText(codePath);
            Console.WriteLine($"Dosya: {codePath}\n");
        }
        else
        {
            Console.WriteLine("Analiz edilecek C# kodunu yapistirin (bitirmek icin bos satirda 'END' yazin):\n");
            var lines = new List<string>();
            string? line;
            while ((line = Console.ReadLine()) != null && line.Trim() != "END")
            {
                lines.Add(line);
            }
            sourceCode = string.Join("\n", lines);
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Console.WriteLine("[HATA] Kod bos, analiz yapilamadi.");
            return;
        }

        // Hibrit analiz
        var config = ConfigLoader.LoadRules("config/rules.json");
        var hybridCfg = ConfigLoader.LoadHybrid("config/hybrid.json");
        var hybrid = new HybridAnalyzer(config, hybridCfg, "models/model.json");
        var result = hybrid.Analyze(sourceCode);
        hybrid.PrintReport(result);
    }

    // ════════════════════════════════════════════
    // MOD 3: BATCH — Tüm dataset'i test et
    // ════════════════════════════════════════════
    static void RunBatch()
    {
        Console.WriteLine("[MOD: TOPLU TEST]\n");

        // Model var mi kontrol et
        if (!File.Exists("models/model.json"))
        {
            Console.WriteLine("[HATA] Egitilmis model bulunamadi.");
            Console.WriteLine("       Once 'dotnet run train' komutu ile modeli egitin.");
            return;
        }

        var config = ConfigLoader.LoadRules("config/rules.json");
        var hybridCfg = ConfigLoader.LoadHybrid("config/hybrid.json");
        var dataset = DatasetLoader.Load("data/dataset.json");
        var hybrid = new HybridAnalyzer(config, hybridCfg, "models/model.json");

        int totalCorrect = 0;
        int totalWrong = 0;
        var results = new List<(DatasetItem Item, HybridResult Result, bool MlMatchesLabel)>();

        foreach (var item in dataset)
        {
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($"  #{item.Id}: {item.Prompt}");
            Console.WriteLine($"  Etiket: {item.Label} | Kalite: {item.QualityScore} | Zorluk: {item.Difficulty}");

            var hybridResult = hybrid.Analyze(item.Code);

            double rulePercentFrac = hybridResult.RuleBasedResult.MaxScore > 0
                ? (double)hybridResult.RuleBasedResult.TotalScore / hybridResult.RuleBasedResult.MaxScore
                : 0;
            double rulePercent100 = rulePercentFrac * 100;

            bool mlOk = hybridResult.MLResult.PredictedLabel == item.Label;
            if (mlOk) totalCorrect++; else totalWrong++;
            results.Add((item, hybridResult, mlOk));

            string ruleOnly = BatchEvaluationMetrics.PredictRuleOnly(rulePercent100, hybridCfg);
            string matchDurum = mlOk ? "DOGRU" : "YANLIS";
            Console.WriteLine($"  Kural Skoru: {hybridResult.RuleBasedResult.TotalScore}/{hybridResult.RuleBasedResult.MaxScore} ({rulePercentFrac:P0})");
            Console.WriteLine($"  ML Tahmin: {hybridResult.MLResult.PredictedLabel} (guven: {hybridResult.MLResult.Confidence:P0}, skor: {hybridResult.MLResult.PredictedScore:F0})");
            Console.WriteLine($"  [{matchDurum}] ML Tahmin={hybridResult.MLResult.PredictedLabel}, Gercek={item.Label}");
            Console.WriteLine($"  Birlesik Skor: {hybridResult.CombinedScore:F1}/100 -> Hibrit: {hybridResult.FinalVerdict} | Yalniz kural: {ruleOnly}");

            if (hybridResult.RuleBasedResult.Issues.Count > 0)
            {
                Console.WriteLine($"  Sorunlar ({hybridResult.RuleBasedResult.Issues.Count}):");
                foreach (var issue in hybridResult.RuleBasedResult.Issues.Take(3))
                    Console.WriteLine($"      - {issue}");
                if (hybridResult.RuleBasedResult.Issues.Count > 3)
                    Console.WriteLine($"      ... ve {hybridResult.RuleBasedResult.Issues.Count - 3} sorun daha");
            }
            Console.WriteLine();
        }

        int n = dataset.Count;
        var mlRows = results.Select(r => (r.Item.Label, r.Result.MLResult.PredictedLabel)).ToList();
        var ruleRows = results.Select(r =>
        {
            double k = r.Result.RuleBasedResult.MaxScore > 0
                ? (double)r.Result.RuleBasedResult.TotalScore / r.Result.RuleBasedResult.MaxScore * 100
                : 0;
            return (Actual: r.Item.Label, Predicted: BatchEvaluationMetrics.PredictRuleOnly(k, hybridCfg));
        }).ToList();
        var hybridRows = results.Select(r => (r.Item.Label, r.Result.FinalVerdict)).ToList();
        int ruleCorrect = ruleRows.Count(r => r.Actual == r.Predicted);

        var mlM = BatchEvaluationMetrics.ConfusionMatrix(mlRows);
        var ruleM = BatchEvaluationMetrics.ConfusionMatrix(ruleRows.Select(r => (r.Actual, r.Predicted)));
        var hybM = BatchEvaluationMetrics.ConfusionMatrix(hybridRows);

        // -- GENEL OZET --
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("[TOPLU TEST OZETI]\n");

        BatchEvaluationMetrics.PrintHybridFormulaNote(hybridCfg);

        Console.WriteLine("  [ISTATISTIKSEL DEGERLENDIRME — Gercek etikete gore]\n");
        BatchEvaluationMetrics.PrintBlock("Yalniz k-NN (ML)", mlM, n);
        BatchEvaluationMetrics.PrintBlock($"Yalniz kural (K >= {hybridCfg.QualityThreshold})", ruleM, n);
        BatchEvaluationMetrics.PrintBlock("Hibrit (FinalVerdict)", hybM, n);

        Console.WriteLine($"  Toplam ornek     : {n}");
        Console.WriteLine($"  ML dogruluk      : {totalCorrect}/{n} ({(double)totalCorrect / n:P0})");
        Console.WriteLine($"  Hibrit dogruluk  : {results.Count(r => r.Result.FinalVerdict == r.Item.Label)}/{n} ({(double)results.Count(r => r.Result.FinalVerdict == r.Item.Label) / n:P0})");
        Console.WriteLine($"  Yalniz kural dogr.: {ruleCorrect}/{n}");

        var goodItems = results.Where(r => r.Item.Label == "Good").ToList();
        var badItems = results.Where(r => r.Item.Label == "Bad").ToList();
        Console.WriteLine($"\n  Good orneklerde ML dogruluk : {goodItems.Count(r => r.MlMatchesLabel)}/{goodItems.Count}");
        Console.WriteLine($"  Bad orneklerde ML dogruluk  : {badItems.Count(r => r.MlMatchesLabel)}/{badItems.Count}");

        Console.WriteLine($"\n  [SKOR KARSILASTIRMASI]");
        if (goodItems.Count > 0)
            Console.WriteLine($"      Good ort. ML skoru : {goodItems.Average(r => r.Result.MLResult.PredictedScore):F0}");
        if (badItems.Count > 0)
            Console.WriteLine($"      Bad ort. ML skoru  : {badItems.Average(r => r.Result.MLResult.PredictedScore):F0}");
        if (goodItems.Count > 0)
            Console.WriteLine($"      Good ort. kural     : {goodItems.Average(r => (double)r.Result.RuleBasedResult.TotalScore / r.Result.RuleBasedResult.MaxScore):P0}");
        if (badItems.Count > 0)
            Console.WriteLine($"      Bad ort. kural      : {badItems.Average(r => (double)r.Result.RuleBasedResult.TotalScore / r.Result.RuleBasedResult.MaxScore):P0}");

        var wrongMl = results.Where(r => !r.MlMatchesLabel).ToList();
        if (wrongMl.Count > 0)
        {
            Console.WriteLine($"\n  [ML ILE YANLIS TAHMIN]");
            foreach (var w in wrongMl)
                Console.WriteLine($"      #{w.Item.Id}: Tahmin={w.Result.MLResult.PredictedLabel}, Gercek={w.Item.Label} (guven: {w.Result.MLResult.Confidence:P0})");
        }

        Console.WriteLine("------------------------------------------");
    }

    static void PrintHelp()
    {
        Console.WriteLine("Kullanim:");
        Console.WriteLine("  dotnet run train                          -> Veri setinden model egit");
        Console.WriteLine("  dotnet run analyze                        -> Kod yapistirarak analiz et");
        Console.WriteLine("  dotnet run analyze dosya.cs               -> Dosyadan kod analiz et");
        Console.WriteLine("  dotnet run batch                          -> Tum veri setini test et");
        Console.WriteLine("  dotnet run pipeline dosya.cs              -> CI/CD pipeline modu");
        Console.WriteLine("  dotnet run pr-check f1.cs f2.cs           -> PR kontrol modu");
        Console.WriteLine();
        Console.WriteLine("PR Check (GitHub Actions):");
        Console.WriteLine("  dotnet run pr-check f1.cs f2.cs           -> Degisen dosyalari analiz et");
        Console.WriteLine("  dotnet run pr-check f1.cs --min-score=80  -> Ozel esik degeri");
        Console.WriteLine("  dotnet run pr-check f1.cs --report=r.md   -> Markdown rapor dosyasina yaz");
        Console.WriteLine();
        Console.WriteLine("Pipeline Parametreleri:");
        Console.WriteLine("  --json                 JSON cikti uret");
        Console.WriteLine("  --ci                   Sessiz mod (banner yok)");
        Console.WriteLine("  --min-score=75         Minimum basari esigi (varsayilan: config/hybrid.json qualityThreshold)");
        Console.WriteLine("  --report=rapor.md      PR raporu dosyasina yaz (pr-check modu)");
        Console.WriteLine();
        Console.WriteLine("Exit Kodlari:");
        Console.WriteLine("  0 = Tum dosyalar kaliteli (Good)");
        Console.WriteLine("  1 = En az bir dosya kalitesiz (Bad)");
        Console.WriteLine("  2 = Hata (model yok, dosya bulunamadi vb.)");
    }

    // ════════════════════════════════════════════
    // MOD 5: PR-CHECK — GitHub Actions PR kontrolu
    // ════════════════════════════════════════════
    static int RunPRCheck(List<string> files, int minScore, string reportPath)
    {
        // Model kontrol
        if (!File.Exists("models/model.json"))
        {
            Console.Error.WriteLine("[HATA] Model bulunamadi. Once 'dotnet run train' calistirin.");
            return 2;
        }

        // Dosya listesi bos ise git diff'den al
        if (files.Count == 0)
        {
            Console.Error.WriteLine("[HATA] Analiz edilecek dosya belirtilmedi.");
            Console.Error.WriteLine("       Kullanim: dotnet run pr-check dosya1.cs dosya2.cs");
            return 2;
        }

        // Sadece var olan .cs dosyalarini filtrele
        var csFiles = files.Where(f => f.EndsWith(".cs") && File.Exists(f)).ToList();
        if (csFiles.Count == 0)
        {
            Console.Error.WriteLine("[BILGI] Analiz edilecek .cs dosyasi bulunamadi. PR gecti.");
            return 0;
        }

        var config = ConfigLoader.LoadRules("config/rules.json");
        var hybridCfg = ConfigLoader.LoadHybrid("config/hybrid.json");
        var hybrid = new HybridAnalyzer(config, hybridCfg, "models/model.json");

        var dosyaSonuclari = new List<(string Dosya, HybridResult Sonuc, bool Gecti)>();
        int gecenSayisi = 0;
        int kalanSayisi = 0;

        Console.WriteLine($"[PR KONTROL] {csFiles.Count} dosya analiz ediliyor (esik: {minScore})\n");

        foreach (var file in csFiles)
        {
            var sourceCode = File.ReadAllText(file);

            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                Console.WriteLine($"  [ATLANDI] {file} (bos dosya)");
                continue;
            }

            var result = hybrid.Analyze(sourceCode);
            bool gecti = result.CombinedScore >= minScore && result.FinalVerdict == "Good";

            if (gecti) gecenSayisi++; else kalanSayisi++;
            dosyaSonuclari.Add((file, result, gecti));

            string durum = gecti ? "GECTI" : "KALDI";
            Console.WriteLine($"  [{durum}] {file} -> {result.CombinedScore:F1}/100 ({result.FinalVerdict})");

            if (!gecti && result.RuleBasedResult.Issues.Count > 0)
            {
                foreach (var issue in result.RuleBasedResult.Issues.Take(3))
                    Console.WriteLine($"         - {issue}");
                if (result.RuleBasedResult.Issues.Count > 3)
                    Console.WriteLine($"         ... ve {result.RuleBasedResult.Issues.Count - 3} sorun daha");
            }
        }

        // Ozet
        Console.WriteLine($"\n------------------------------------------");
        Console.WriteLine($"[PR OZET]");
        Console.WriteLine($"  Toplam dosya : {dosyaSonuclari.Count}");
        Console.WriteLine($"  Gecen        : {gecenSayisi}");
        Console.WriteLine($"  Kalan        : {kalanSayisi}");
        Console.WriteLine($"  Ort. Skor    : {dosyaSonuclari.Average(d => d.Sonuc.CombinedScore):F1}/100");
        Console.WriteLine($"  Sonuc        : {(kalanSayisi == 0 ? "PR UYGUN (merge edilebilir)" : "PR UYGUN DEGIL (duzeltme gerekli)")}");
        Console.WriteLine($"------------------------------------------");

        // Markdown rapor dosyasina yaz (GitHub Actions PR yorumu icin)
        if (!string.IsNullOrEmpty(reportPath))
        {
            WriteMarkdownReport(dosyaSonuclari, minScore, reportPath);
            Console.WriteLine($"\n[BILGI] Markdown rapor yazildi: {reportPath}");
        }

        return kalanSayisi == 0 ? 0 : 1;
    }

    /// <summary>
    /// GitHub Actions PR yorumu icin gorsel Markdown rapor uretir.
    /// </summary>
    static void WriteMarkdownReport(
        List<(string Dosya, HybridResult Sonuc, bool Gecti)> sonuclar,
        int minScore, string reportPath)
    {
        var sb = new System.Text.StringBuilder();

        int gecen = sonuclar.Count(s => s.Gecti);
        int kalan = sonuclar.Count(s => !s.Gecti);
        int toplam = sonuclar.Count;
        double ortSkor = sonuclar.Average(s => s.Sonuc.CombinedScore);
        double maxSkor = sonuclar.Max(s => s.Sonuc.CombinedScore);
        double minSkor = sonuclar.Min(s => s.Sonuc.CombinedScore);
        bool tamamenGecti = kalan == 0;

        // ═══════════════════════════════════════
        // HEADER — Logo + Badge'ler
        // ═══════════════════════════════════════
        sb.AppendLine("<div align=\"center\">");
        sb.AppendLine();
        sb.AppendLine("# AI OOP Analyzer");
        sb.AppendLine("### Kural Bazli + ML Hibrit Kod Kalite Raporu");
        sb.AppendLine();

        string durumRenk = tamamenGecti ? "brightgreen" : "red";
        string durumText = tamamenGecti ? "GECTI" : "KALDI";
        string skorRenk = ortSkor >= 80 ? "brightgreen" : ortSkor >= 65 ? "yellow" : ortSkor >= 40 ? "orange" : "red";
        string skorHarf = ortSkor >= 90 ? "A+" : ortSkor >= 80 ? "A" : ortSkor >= 70 ? "B" : ortSkor >= 60 ? "C" : ortSkor >= 40 ? "D" : "F";

        sb.AppendLine($"![Sonuc](https://img.shields.io/badge/Sonuc-{Uri.EscapeDataString(durumText)}-{durumRenk}?style=for-the-badge&logo=checkmarx)");
        sb.AppendLine($"![Skor](https://img.shields.io/badge/Skor-{ortSkor:F0}%2F100_({skorHarf})-{skorRenk}?style=for-the-badge&logo=speedtest)");
        sb.AppendLine($"![Dosya](https://img.shields.io/badge/Dosya-{toplam}-blue?style=for-the-badge&logo=files)");
        sb.AppendLine($"![Model](https://img.shields.io/badge/Model-k--NN_(k%3D3)-purple?style=for-the-badge&logo=tensorflow)");
        sb.AppendLine($"![CK](https://img.shields.io/badge/CK_Metrikleri-6_Metrik-teal?style=for-the-badge&logo=pocketcasts)");
        sb.AppendLine();
        sb.AppendLine("</div>");
        sb.AppendLine();

        // ═══════════════════════════════════════
        // GENEL DASHBOARD
        // ═══════════════════════════════════════
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Genel Dashboard");
        sb.AppendLine();

        // Buyuk skor gostergesi
        string skorBant = ortSkor >= 90 ? "Mukemmel" : ortSkor >= 80 ? "Cok Iyi" : ortSkor >= 70 ? "Iyi" : ortSkor >= 60 ? "Orta" : ortSkor >= 40 ? "Zayif" : "Yetersiz";
        int barLen = 30;
        int filled = Math.Min((int)(ortSkor / 100.0 * barLen), barLen);
        string progressBar = new string('#', filled) + new string('.', barLen - filled);

        sb.AppendLine($"> **Ortalama Skor: {ortSkor:F1}/100 ({skorHarf}) — {skorBant}**");
        sb.AppendLine($"> ");
        sb.AppendLine($"> `[{progressBar}]`");
        sb.AppendLine();

        // Genel istatistik tablosu (2 sutunlu layout)
        sb.AppendLine("| Metrik | Deger | Analiz | Deger |");
        sb.AppendLine("|:----------|:-----:|:----------|:-----:|");
        sb.AppendLine($"| Analiz edilen dosya | **{toplam}** | Minimum esik | **{minScore}/100** |");
        sb.AppendLine($"| Gecen dosya | **{gecen}** | Ortalama skor | **{ortSkor:F1}/100** |");
        sb.AppendLine($"| Kalan dosya | **{kalan}** | En yuksek skor | **{maxSkor:F1}/100** |");
        sb.AppendLine($"| Basari orani | **%{(toplam > 0 ? (double)gecen / toplam * 100 : 0):F0}** | En dusuk skor | **{minSkor:F1}/100** |");
        sb.AppendLine();

        // ═══════════════════════════════════════
        // DOSYA KARŞILAŞTIRMA TABLOSU
        // ═══════════════════════════════════════
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Dosya Bazli Sonuclar");
        sb.AppendLine();
        sb.AppendLine("| Durum | Dosya | Birlesik Skor | Kural Skoru | ML Tahmin | ML Guven | Sorun |");
        sb.AppendLine("|:-----:|-------|:-------------:|:-----------:|:---------:|:--------:|:-----:|");

        foreach (var (dosya, sonuc, gecti) in sonuclar)
        {
            double kuralYuzde = sonuc.RuleBasedResult.MaxScore > 0
                ? (double)sonuc.RuleBasedResult.TotalScore / sonuc.RuleBasedResult.MaxScore * 100
                : 0;

            string durumIcon = gecti ? "GECTI" : "KALDI";
            int sorunSayisi = sonuc.RuleBasedResult.Issues.Count;
            string mlIcon = sonuc.MLResult.PredictedLabel == "Good" ? "Good" : "Bad";
            string sorunIcon = sorunSayisi == 0 ? "0" : $"{sorunSayisi}";

            sb.AppendLine($"| {durumIcon} | `{dosya}` | **{sonuc.CombinedScore:F1}** | %{kuralYuzde:F0} ({sonuc.RuleBasedResult.TotalScore}/{sonuc.RuleBasedResult.MaxScore}) | {mlIcon} ({sonuc.MLResult.PredictedScore:F0}) | %{sonuc.MLResult.Confidence * 100:F0} | {sorunIcon} |");
        }
        sb.AppendLine();

        // Skor bar grafigi
        sb.AppendLine("### Skor Dagilimi");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var (dosya, sonuc, gecti) in sonuclar)
        {
            string kısaDosya = Path.GetFileName(dosya);
            if (kısaDosya.Length > 30) kısaDosya = kısaDosya[..27] + "...";
            int bar = Math.Min((int)(sonuc.CombinedScore / 100.0 * 30), 30);
            string skorBar = new string('#', bar) + new string('.', 30 - bar);
            string durumMark = gecti ? "PASS" : "FAIL";
            sb.AppendLine($"  {kısaDosya,-30} |{skorBar}| {sonuc.CombinedScore,5:F1} [{durumMark}]");
        }
        sb.AppendLine($"  {"-- Esik --",-30} |{"- ",-30}| {minScore,5}");
        sb.AppendLine("```");
        sb.AppendLine();

        // ═══════════════════════════════════════
        // KALAN DOSYALAR — DETAYLI SORUNLAR
        // ═══════════════════════════════════════
        var kalanDosyalar = sonuclar.Where(s => !s.Gecti).ToList();
        if (kalanDosyalar.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Duzeltilmesi Gereken Dosyalar");
            sb.AppendLine();

            foreach (var (dosya, sonuc, _) in kalanDosyalar)
            {
                WriteDosyaDetay(sb, dosya, sonuc, minScore, true);
            }
        }

        // ═══════════════════════════════════════
        // GECEN DOSYALAR
        // ═══════════════════════════════════════
        var gecenDosyalar = sonuclar.Where(s => s.Gecti).ToList();
        if (gecenDosyalar.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Basarili Dosyalar");
            sb.AppendLine();

            foreach (var (dosya, sonuc, _) in gecenDosyalar)
            {
                WriteDosyaDetay(sb, dosya, sonuc, minScore, false);
            }
        }

        // ═══════════════════════════════════════
        // GENEL CK METRİKLERİ ÖZETİ
        // ═══════════════════════════════════════
        var tumCK = sonuclar
            .Where(s => s.Sonuc.CKMetricsPerClass != null)
            .SelectMany(s => s.Sonuc.CKMetricsPerClass!)
            .ToList();

        if (tumCK.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## CK Metrikleri — Genel Ozet (Chidamber & Kemerer)");
            sb.AppendLine();

            // Tüm sınıfların genel özet tablosu
            int toplamSinif = tumCK.Count;
            int wmcIhlal = tumCK.Count(c => c.WMC > 10);
            int ditIhlal = tumCK.Count(c => c.DIT > 3);
            int cboIhlal = tumCK.Count(c => c.CBO > 5);
            int rfcIhlal = tumCK.Count(c => c.RFC > 20);
            int lcomIhlal = tumCK.Count(c => c.LCOM > 3);
            int temizSinif = tumCK.Count(c => c.WMC <= 10 && c.DIT <= 3 && c.CBO <= 5 && c.RFC <= 20 && c.LCOM <= 3);

            sb.AppendLine($"> **Toplam {toplamSinif} sinif analiz edildi** — {temizSinif} sinif tum esikleri gecti ({(toplamSinif > 0 ? (double)temizSinif / toplamSinif * 100 : 0):F0}%)");
            sb.AppendLine();

            sb.AppendLine("| Metrik | Aciklama | Esik | Ihlal | Oran | Durum |");
            sb.AppendLine("|:------:|:---------|:----:|:-----:|:----:|:-----:|");
            sb.AppendLine($"| **WMC** | Agirlikli Metod Sayisi | <= 10 | {wmcIhlal}/{toplamSinif} | %{(toplamSinif > 0 ? (double)(toplamSinif - wmcIhlal) / toplamSinif * 100 : 0):F0} | {(wmcIhlal == 0 ? "OK" : "IHLAL")} |");
            sb.AppendLine($"| **DIT** | Kalitim Derinligi | <= 3 | {ditIhlal}/{toplamSinif} | %{(toplamSinif > 0 ? (double)(toplamSinif - ditIhlal) / toplamSinif * 100 : 0):F0} | {(ditIhlal == 0 ? "OK" : "IHLAL")} |");
            sb.AppendLine($"| **NOC** | Alt Sinif Sayisi | <= 5 | — | — | - |");
            sb.AppendLine($"| **CBO** | Siniflar Arasi Bagimlilik | <= 5 | {cboIhlal}/{toplamSinif} | %{(toplamSinif > 0 ? (double)(toplamSinif - cboIhlal) / toplamSinif * 100 : 0):F0} | {(cboIhlal == 0 ? "OK" : "IHLAL")} |");
            sb.AppendLine($"| **RFC** | Sinif Yanit Sayisi | <= 20 | {rfcIhlal}/{toplamSinif} | %{(toplamSinif > 0 ? (double)(toplamSinif - rfcIhlal) / toplamSinif * 100 : 0):F0} | {(rfcIhlal == 0 ? "OK" : "IHLAL")} |");
            sb.AppendLine($"| **LCOM** | Uyumsuzluk (Cohesion) | <= 3 | {lcomIhlal}/{toplamSinif} | %{(toplamSinif > 0 ? (double)(toplamSinif - lcomIhlal) / toplamSinif * 100 : 0):F0} | {(lcomIhlal == 0 ? "OK" : "IHLAL")} |");
            sb.AppendLine();

            // En sorunlu sınıflar (varsa)
            var sorunluSiniflar = tumCK
                .Select(ck => new {
                    ck.ClassName,
                    Issues = (ck.WMC > 10 ? 1 : 0) + (ck.DIT > 3 ? 1 : 0) + (ck.CBO > 5 ? 1 : 0) + (ck.RFC > 20 ? 1 : 0) + (ck.LCOM > 3 ? 1 : 0),
                    ck.WMC, ck.DIT, ck.CBO, ck.RFC, ck.LCOM
                })
                .Where(x => x.Issues > 0)
                .OrderByDescending(x => x.Issues)
                .ToList();

            if (sorunluSiniflar.Count > 0)
            {
                sb.AppendLine("<details>");
                sb.AppendLine("<summary><b>CK esik ihlali olan siniflar</b></summary>");
                sb.AppendLine();
                sb.AppendLine("| Sinif | Ihlal Sayisi | Detay |");
                sb.AppendLine("|:------|:------------:|:------|");
                foreach (var s in sorunluSiniflar)
                {
                    var detaylar = new List<string>();
                    if (s.WMC > 10) detaylar.Add($"WMC={s.WMC}(>{10})");
                    if (s.DIT > 3) detaylar.Add($"DIT={s.DIT}(>{3})");
                    if (s.CBO > 5) detaylar.Add($"CBO={s.CBO}(>{5})");
                    if (s.RFC > 20) detaylar.Add($"RFC={s.RFC}(>{20})");
                    if (s.LCOM > 3) detaylar.Add($"LCOM={s.LCOM}(>{3})");
                    string seviye = s.Issues >= 3 ? "Kritik" : s.Issues >= 2 ? "Orta" : "Dusuk";
                    sb.AppendLine($"| `{s.ClassName}` | {s.Issues} ({seviye}) | {string.Join(", ", detaylar)} |");
                }
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }

        // ═══════════════════════════════════════
        // İYİLEŞTİRME ÖNERİLERİ
        // ═══════════════════════════════════════
        var tumSorunlar = sonuclar.SelectMany(s => s.Sonuc.RuleBasedResult.Issues).ToList();
        if (tumSorunlar.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Iyilestirme Onerileri");
            sb.AppendLine();

            var kategoriler = tumSorunlar
                .GroupBy(i => {
                    if (i.StartsWith("[Kapsulleme]")) return "encap";
                    if (i.StartsWith("[Tek Sorumluluk]")) return "srp";
                    if (i.StartsWith("[Bagimlilik Enjeksiyonu]")) return "di";
                    if (i.StartsWith("[Arayuz]")) return "iface";
                    if (i.StartsWith("[Kalitim]")) return "inherit";
                    if (i.StartsWith("[Polimorfizm]")) return "poly";
                    if (i.StartsWith("[CK-")) return "ck";
                    return "other";
                })
                .ToDictionary(g => g.Key, g => g.Count());

            if (kategoriler.ContainsKey("encap") && kategoriler["encap"] > 0)
            {
                sb.AppendLine($"### Kapsulleme ({kategoriler["encap"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("// Kotu: Public alan");
                sb.AppendLine("public string name;");
                sb.AppendLine();
                sb.AppendLine("// Iyi: Private alan + Property");
                sb.AppendLine("private string _name;");
                sb.AppendLine("public string Name { get => _name; set => _name = value; }");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("di") && kategoriler["di"] > 0)
            {
                sb.AppendLine($"### Bagimlilik Enjeksiyonu ({kategoriler["di"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("// Kotu: Siki bagimlilik");
                sb.AppendLine("public class Service {");
                sb.AppendLine("    private repo = new Repository();");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine("// Iyi: Constructor Injection");
                sb.AppendLine("public class Service {");
                sb.AppendLine("    private readonly IRepository _repo;");
                sb.AppendLine("    public Service(IRepository repo) => _repo = repo;");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("iface") && kategoriler["iface"] > 0)
            {
                sb.AppendLine($"### Interface Kullanimi ({kategoriler["iface"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("// Kotu: Concrete sinifa bagimlilik");
                sb.AppendLine("public class OrderService { }");
                sb.AppendLine();
                sb.AppendLine("// Iyi: Interface ile soyutlama");
                sb.AppendLine("public interface IOrderService { }");
                sb.AppendLine("public class OrderService : IOrderService { }");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("srp") && kategoriler["srp"] > 0)
            {
                sb.AppendLine($"### Tek Sorumluluk Prensibi ({kategoriler["srp"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("> Bir sinif yalnizca bir isten sorumlu olmalidir. Cok fazla metod varsa sinifi bolerek");
                sb.AppendLine("> her birinin tek bir sorumlulugu olmasini saglayin.");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("inherit") && kategoriler["inherit"] > 0)
            {
                sb.AppendLine($"### Kalitim ({kategoriler["inherit"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("// Ortak davranisi base sinifta toplayin");
                sb.AppendLine("public abstract class BaseEntity {");
                sb.AppendLine("    public int Id { get; set; }");
                sb.AppendLine("    public DateTime CreatedAt { get; set; }");
                sb.AppendLine("}");
                sb.AppendLine("public class User : BaseEntity { }");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("poly") && kategoriler["poly"] > 0)
            {
                sb.AppendLine($"### Polimorfizm ({kategoriler["poly"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine("// virtual + override ile polimorfizm");
                sb.AppendLine("public class Shape {");
                sb.AppendLine("    public virtual double Area() => 0;");
                sb.AppendLine("}");
                sb.AppendLine("public class Circle : Shape {");
                sb.AppendLine("    public override double Area() => Math.PI * R * R;");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (kategoriler.ContainsKey("ck") && kategoriler["ck"] > 0)
            {
                sb.AppendLine($"### CK Metrikleri ({kategoriler["ck"]} sorun)");
                sb.AppendLine();
                sb.AppendLine("> - **WMC yuksek?** Sinifi daha kucuk siniflara bolun");
                sb.AppendLine("> - **DIT yuksek?** Kalitim derinligini azaltin, composition tercih edin");
                sb.AppendLine("> - **CBO yuksek?** Interface kullanarak gevsek baglama saglayin");
                sb.AppendLine("> - **RFC yuksek?** Metod cagri zincirini kisaltin");
                sb.AppendLine("> - **LCOM yuksek?** Birbiriyle iliskisiz metotlari ayri siniflara tasiyin");
                sb.AppendLine();
            }
        }

        // ═══════════════════════════════════════
        // ANALIZ METODOLOJİSİ
        // ═══════════════════════════════════════
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("<details>");
        sb.AppendLine("<summary><b>Analiz Metodolojisi — Nasil calisir?</b></summary>");
        sb.AppendLine();
        sb.AppendLine("### Hibrit Analiz Sistemi");
        sb.AppendLine();
        sb.AppendLine("Bu rapor, iki bagimsiz analiz motorunun birlesik sonucudur:");
        sb.AppendLine();
        sb.AppendLine("| Motor | Agirlik | Aciklama |");
        sb.AppendLine("|:------|:-------:|:---------|");
        sb.AppendLine("| **Kural Bazli** | %60 | 7 OOP kurali + CK metrikleri kontrol edilir |");
        sb.AppendLine("| **ML (k-NN)** | %40 | 50 ornek uzerinde egitilmis k-NN modeli |");
        sb.AppendLine();
        sb.AppendLine("**Formul:** `Q = 0.60 x Kural(%) + 0.40 x ML(skor)`");
        sb.AppendLine();
        sb.AppendLine("### Kontrol Edilen OOP Kurallari");
        sb.AppendLine();
        sb.AppendLine("| # | Kural | Maks Puan | Ne kontrol eder? |");
        sb.AppendLine("|:-:|:------|:---------:|:-----------------|");
        sb.AppendLine("| 1 | Kapsulleme | 15 | Public alanlar private/property olmali |");
        sb.AppendLine("| 2 | Tek Sorumluluk (SRP) | 15 | Sinif basina metod sayisi |");
        sb.AppendLine("| 3 | Bagimlilik Enjeksiyonu | 20 | `new` yerine constructor injection |");
        sb.AppendLine("| 4 | Interface Kullanimi | 15 | Siniflarin interface implement etmesi |");
        sb.AppendLine("| 5 | Kalitim | 15 | Base sinif kullanimi |");
        sb.AppendLine("| 6 | Polimorfizm | 20 | virtual/override metod kullanimi |");
        sb.AppendLine("| 7 | CK Metrikleri | 15 | WMC, DIT, NOC, CBO, RFC, LCOM |");
        sb.AppendLine();
        sb.AppendLine("### CK Metrikleri Esik Degerleri");
        sb.AppendLine();
        sb.AppendLine("| Metrik | Tam Adi | Esik | Yuksekse ne olur? |");
        sb.AppendLine("|:------:|:--------|:----:|:------------------|");
        sb.AppendLine("| WMC | Weighted Methods per Class | <= 10 | Sinif cok karmasik, bolunmeli |");
        sb.AppendLine("| DIT | Depth of Inheritance Tree | <= 3 | Kalitim zinciri cok derin |");
        sb.AppendLine("| NOC | Number of Children | <= 5 | Cok fazla alt sinif, soyutlama gerekli |");
        sb.AppendLine("| CBO | Coupling Between Objects | <= 5 | Siki bagimlilik, interface kullanin |");
        sb.AppendLine("| RFC | Response for a Class | <= 20 | Cok fazla metod cagrisi |");
        sb.AppendLine("| LCOM | Lack of Cohesion of Methods | <= 3 | Metotlar iliskisiz, sinif bolunmeli |");
        sb.AppendLine();
        sb.AppendLine("</details>");
        sb.AppendLine();

        // ═══════════════════════════════════════
        // FOOTER
        // ═══════════════════════════════════════
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("<div align=\"center\">");
        sb.AppendLine();
        sb.AppendLine($"**AI OOP Analyzer v2.0** — Kural Bazli + ML Hibrit Analiz + CK Metrikleri");
        sb.AppendLine();
        sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm} | Esik: {minScore}/100 | Model: k-NN (k=3) | CK: Chidamber & Kemerer");
        sb.AppendLine();
        sb.AppendLine($"![.NET](https://img.shields.io/badge/.NET_8-512BD4?style=flat-square&logo=dotnet&logoColor=white)");
        sb.AppendLine($"![Roslyn](https://img.shields.io/badge/Roslyn-189BDD?style=flat-square&logo=visual-studio&logoColor=white)");
        sb.AppendLine($"![ML](https://img.shields.io/badge/k--NN_ML-FF6F61?style=flat-square&logo=tensorflow&logoColor=white)");
        sb.AppendLine();
        sb.AppendLine("</div>");

        // Dosyaya yaz
        var dir = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(reportPath, sb.ToString());
    }

    // ════════════════════════════════════════════
    // YARDIMCI: Dosya Detay Bölümü Yaz
    // ════════════════════════════════════════════
    static void WriteDosyaDetay(System.Text.StringBuilder sb, string dosya, HybridResult sonuc, int minScore, bool kalanMi)
    {
        double kuralYuzde = sonuc.RuleBasedResult.MaxScore > 0
            ? (double)sonuc.RuleBasedResult.TotalScore / sonuc.RuleBasedResult.MaxScore * 100
            : 0;

        string dosyaSkorRenk = sonuc.CombinedScore >= 80 ? "brightgreen" : sonuc.CombinedScore >= 65 ? "yellow" : sonuc.CombinedScore >= 40 ? "orange" : "red";
        string skorHarf = sonuc.CombinedScore >= 90 ? "A+" : sonuc.CombinedScore >= 80 ? "A" : sonuc.CombinedScore >= 70 ? "B" : sonuc.CombinedScore >= 60 ? "C" : sonuc.CombinedScore >= 40 ? "D" : "F";

        if (kalanMi)
        {
            sb.AppendLine($"### `{dosya}` — KALDI");
        }
        else
        {
            sb.AppendLine($"<details>");
            sb.AppendLine($"<summary><code>{dosya}</code> — {sonuc.CombinedScore:F1}/100 ({skorHarf}) GECTI</summary>");
        }
        sb.AppendLine();

        // Badge satırı
        sb.AppendLine($"![Skor](https://img.shields.io/badge/Skor-{sonuc.CombinedScore:F0}%2F100_({skorHarf})-{dosyaSkorRenk}?style=flat-square) ");
        sb.AppendLine($"![Kural](https://img.shields.io/badge/Kural-%25{kuralYuzde:F0}-blue?style=flat-square) ");
        sb.AppendLine($"![ML](https://img.shields.io/badge/ML-{sonuc.MLResult.PredictedLabel}_({sonuc.MLResult.PredictedScore:F0})-{(sonuc.MLResult.PredictedLabel == "Good" ? "green" : "red")}?style=flat-square) ");
        sb.AppendLine($"![Guven](https://img.shields.io/badge/Guven-%25{sonuc.MLResult.Confidence * 100:F0}-blueviolet?style=flat-square)");
        sb.AppendLine();

        // Kural bazli tablo
        sb.AppendLine("| Kural | Puan | Maks | Oran | Durum |");
        sb.AppendLine("|:------|:----:|:----:|:----:|:-----:|");
        foreach (var rule in sonuc.RuleBasedResult.RuleResults)
        {
            double oran = rule.MaxScore > 0 ? (double)rule.Score / rule.MaxScore * 100 : 0;
            string rd = rule.Score == rule.MaxScore ? "Tam" : rule.Score >= rule.MaxScore * 0.7 ? "Kismi" : "Ihlal";
            int miniBar = Math.Min((int)(oran / 100.0 * 8), 8);
            string miniBarStr = new string('#', miniBar) + new string('.', 8 - miniBar);
            sb.AppendLine($"| {rule.RuleName} | {rule.Score} | {rule.MaxScore} | `{miniBarStr}` %{oran:F0} | {rd} |");
        }
        sb.AppendLine();

        // Sorunlar
        if (sonuc.RuleBasedResult.Issues.Count > 0)
        {
            if (!kalanMi)
            {
                // Gecen dosyada sorunlar kucuk
                sb.AppendLine($"**Kucuk sorunlar ({sonuc.RuleBasedResult.Issues.Count} adet):**");
                sb.AppendLine();
                foreach (var issue in sonuc.RuleBasedResult.Issues)
                {
                    sb.AppendLine($"- {issue}");
                }
                sb.AppendLine();
            }
            else
            {
                // Kalan dosyada detayli sorunlar
                sb.AppendLine($"<details>");
                sb.AppendLine($"<summary><b>Tespit edilen sorunlar ({sonuc.RuleBasedResult.Issues.Count} adet)</b></summary>");
                sb.AppendLine();

                var grouped = sonuc.RuleBasedResult.Issues
                    .GroupBy(i => {
                        if (i.StartsWith("[Kapsulleme]")) return "Kapsulleme";
                        if (i.StartsWith("[Tek Sorumluluk]")) return "Tek Sorumluluk (SRP)";
                        if (i.StartsWith("[Bagimlilik Enjeksiyonu]")) return "Bagimlilik Enjeksiyonu (DI)";
                        if (i.StartsWith("[Arayuz]")) return "Arayuz (Interface)";
                        if (i.StartsWith("[Kalitim]")) return "Kalitim (Inheritance)";
                        if (i.StartsWith("[Polimorfizm]")) return "Polimorfizm";
                        if (i.StartsWith("[CK-")) return "CK Metrikleri";
                        return "Diger";
                    });

                foreach (var group in grouped)
                {
                    sb.AppendLine($"**{group.Key}:**");
                    foreach (var issue in group)
                    {
                        sb.AppendLine($"- {issue}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }

        // Teknik detaylar
        sb.AppendLine("<details>");
        sb.AppendLine("<summary><b>Teknik Detaylar</b></summary>");
        sb.AppendLine();
        sb.AppendLine("| Ozellik | Deger | | Ozellik | Deger |");
        sb.AppendLine("|:--------|:-----:|-|:--------|:-----:|");
        sb.AppendLine($"| Sinif sayisi | {sonuc.Features.ClassCount} | | Kapsulleme | %{sonuc.Features.EncapsulationRatio * 100:F0} |");
        sb.AppendLine($"| Toplam metod | {sonuc.Features.TotalMethodCount} | | Interface orani | %{sonuc.Features.InterfaceRatio * 100:F0} |");
        sb.AppendLine($"| Public alan | {sonuc.Features.PublicFieldCount} | | virtual metod | {sonuc.Features.VirtualMethodCount} |");
        sb.AppendLine($"| Private alan | {sonuc.Features.PrivateFieldCount} | | override metod | {sonuc.Features.OverrideMethodCount} |");
        sb.AppendLine($"| new kullanimi | {sonuc.Features.ObjectCreationCount} | | ML guven | %{sonuc.MLResult.Confidence * 100:F0} |");
        sb.AppendLine($"| | | | Yakin ornekler | {string.Join(", ", sonuc.MLResult.NearestNeighbors)} |");
        sb.AppendLine();
        sb.AppendLine("</details>");

        // CK Metrikleri tablosu
        if (sonuc.CKMetricsPerClass != null && sonuc.CKMetricsPerClass.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<details>");
            sb.AppendLine("<summary><b>CK Metrikleri — Sinif Bazli</b></summary>");
            sb.AppendLine();

            sb.AppendLine("| Sinif | WMC | DIT | NOC | CBO | RFC | LCOM | Not |");
            sb.AppendLine("|:------|:---:|:---:|:---:|:---:|:---:|:----:|:---:|");

            foreach (var ck in sonuc.CKMetricsPerClass)
            {
                int ckIssues = (ck.WMC > 10 ? 1 : 0) + (ck.DIT > 3 ? 1 : 0) + (ck.CBO > 5 ? 1 : 0) +
                               (ck.RFC > 20 ? 1 : 0) + (ck.LCOM > 3 ? 1 : 0);
                string grade = ckIssues == 0 ? "A+" : ckIssues == 1 ? "B" : ckIssues == 2 ? "C" : ckIssues <= 3 ? "D" : "F";

                string wmcFmt = ck.WMC > 10 ? $"**{ck.WMC}**" : $"{ck.WMC}";
                string ditFmt = ck.DIT > 3 ? $"**{ck.DIT}**" : $"{ck.DIT}";
                string nocFmt = $"{ck.NOC}";
                string cboFmt = ck.CBO > 5 ? $"**{ck.CBO}**" : $"{ck.CBO}";
                string rfcFmt = ck.RFC > 20 ? $"**{ck.RFC}**" : $"{ck.RFC}";
                string lcomFmt = ck.LCOM > 3 ? $"**{ck.LCOM}**" : $"{ck.LCOM}";

                sb.AppendLine($"| `{ck.ClassName}` | {wmcFmt} | {ditFmt} | {nocFmt} | {cboFmt} | {rfcFmt} | {lcomFmt} | {grade} |");
            }

            var ckAvg = sonuc.CKMetricsAverage;
            if (ckAvg != null)
            {
                sb.AppendLine($"| **Ortalama** | **{ckAvg.WMC}** | **{ckAvg.DIT}** | **{ckAvg.NOC}** | **{ckAvg.CBO}** | **{ckAvg.RFC}** | **{ckAvg.LCOM}** | — |");
            }

            sb.AppendLine();
            sb.AppendLine("</details>");
        }

        sb.AppendLine();

        if (!kalanMi)
        {
            sb.AppendLine("</details>");
            sb.AppendLine();
        }
    }

    // ════════════════════════════════════════════
    // MOD 4: PIPELINE — CI/CD entegrasyonu (tek dosya)
    // ════════════════════════════════════════════
    static int RunPipeline(string codePath, bool jsonOutput, int minScore)
    {
        // Model kontrol
        if (!File.Exists("models/model.json"))
        {
            if (jsonOutput)
                Console.WriteLine("{\"error\": \"Model dosyasi bulunamadi. Once 'dotnet run train' calistirin.\"}");
            else
                Console.Error.WriteLine("[HATA] Egitilmis model bulunamadi. Once 'dotnet run train' calistirin.");
            return 2;
        }

        // Kod al
        string sourceCode;
        if (!string.IsNullOrEmpty(codePath) && File.Exists(codePath))
        {
            sourceCode = File.ReadAllText(codePath);
        }
        else if (!string.IsNullOrEmpty(codePath))
        {
            if (jsonOutput)
                Console.WriteLine($"{{\"error\": \"Dosya bulunamadi: {codePath}\"}}");
            else
                Console.Error.WriteLine($"[HATA] Dosya bulunamadi: {codePath}");
            return 2;
        }
        else
        {
            // stdin'den oku
            sourceCode = Console.In.ReadToEnd();
        }

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            if (jsonOutput)
                Console.WriteLine("{\"error\": \"Kod bos, analiz yapilamadi.\"}");
            else
                Console.Error.WriteLine("[HATA] Kod bos, analiz yapilamadi.");
            return 2;
        }

        // Analiz
        var config = ConfigLoader.LoadRules("config/rules.json");
        var hybridCfg = ConfigLoader.LoadHybrid("config/hybrid.json");
        var hybrid = new HybridAnalyzer(config, hybridCfg, "models/model.json");
        var result = hybrid.Analyze(sourceCode);

        double rulePercent = result.RuleBasedResult.MaxScore > 0
            ? (double)result.RuleBasedResult.TotalScore / result.RuleBasedResult.MaxScore * 100
            : 0;

        bool passed = result.CombinedScore >= minScore && result.FinalVerdict == "Good";

        if (jsonOutput)
        {
            // JSON cikti
            var output = new
            {
                dosya = string.IsNullOrEmpty(codePath) ? "stdin" : codePath,
                sonuc = result.FinalVerdict,
                birlesik_skor = result.CombinedScore,
                min_esik = minScore,
                gecti = passed,
                kalite_tanimi = new
                {
                    q_formulu = $"Q = w_k*K + w_m*M (K: kural %, M: ML skoru)",
                    rule_weight = hybridCfg.RuleWeight,
                    ml_weight = hybridCfg.MLWeight,
                    quality_threshold = hybridCfg.QualityThreshold,
                    strong_agreement_high_rule_percent = hybridCfg.StrongAgreementHighRulePercent,
                    strong_agreement_low_rule_percent = hybridCfg.StrongAgreementLowRulePercent
                },
                kural_bazli = new
                {
                    skor = result.RuleBasedResult.TotalScore,
                    maks = result.RuleBasedResult.MaxScore,
                    yuzde = Math.Round(rulePercent, 1),
                    sorunlar = result.RuleBasedResult.Issues,
                    kurallar = result.RuleBasedResult.RuleResults.Select(r => new
                    {
                        kural = r.RuleName,
                        skor = r.Score,
                        maks = r.MaxScore
                    })
                },
                ml_tahmini = new
                {
                    tahmin = result.MLResult.PredictedLabel,
                    guven = Math.Round(result.MLResult.Confidence * 100, 1),
                    skor = result.MLResult.PredictedScore,
                    en_yakin = result.MLResult.NearestNeighbors
                },
                ozellikler = new
                {
                    sinif_sayisi = result.Features.ClassCount,
                    kapsulleme_orani = Math.Round(result.Features.EncapsulationRatio * 100, 1),
                    interface_orani = Math.Round(result.Features.InterfaceRatio * 100, 1),
                    public_alan = result.Features.PublicFieldCount,
                    private_alan = result.Features.PrivateFieldCount,
                    new_kullanimi = result.Features.ObjectCreationCount,
                    virtual_metod = result.Features.VirtualMethodCount,
                    override_metod = result.Features.OverrideMethodCount
                },
                ck_metrikleri = new
                {
                    sinif_bazli = result.CKMetricsPerClass?.Select(ck => new
                    {
                        sinif = ck.ClassName,
                        wmc = ck.WMC,
                        dit = ck.DIT,
                        noc = ck.NOC,
                        cbo = ck.CBO,
                        rfc = ck.RFC,
                        lcom = ck.LCOM
                    }),
                    ortalama = result.CKMetricsAverage != null ? new
                    {
                        wmc = result.CKMetricsAverage.WMC,
                        dit = result.CKMetricsAverage.DIT,
                        noc = result.CKMetricsAverage.NOC,
                        cbo = result.CKMetricsAverage.CBO,
                        rfc = result.CKMetricsAverage.RFC,
                        lcom = result.CKMetricsAverage.LCOM
                    } : null,
                    esikler = new
                    {
                        wmc = "≤ 10",
                        dit = "≤ 3",
                        noc = "≤ 5",
                        cbo = "≤ 5",
                        rfc = "≤ 20",
                        lcom = "≤ 3"
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(output, jsonOptions));
        }
        else
        {
            // Kisa ozet cikti
            string dosyaAdi = string.IsNullOrEmpty(codePath) ? "stdin" : codePath;
            string durum = passed ? "GECTI" : "KALDI";
            Console.WriteLine($"[{durum}] {dosyaAdi} | Skor: {result.CombinedScore}/100 (esik: {minScore}) | Karar: {result.FinalVerdict}");
            Console.WriteLine($"  Kural: {result.RuleBasedResult.TotalScore}/{result.RuleBasedResult.MaxScore} ({rulePercent:F0}%) | ML: {result.MLResult.PredictedLabel} ({result.MLResult.PredictedScore:F0}/100, guven: {result.MLResult.Confidence:P0})");

            if (result.RuleBasedResult.Issues.Count > 0)
            {
                Console.WriteLine($"  Sorunlar ({result.RuleBasedResult.Issues.Count}):");
                foreach (var issue in result.RuleBasedResult.Issues.Take(5))
                    Console.WriteLine($"    - {issue}");
                if (result.RuleBasedResult.Issues.Count > 5)
                    Console.WriteLine($"    ... ve {result.RuleBasedResult.Issues.Count - 5} sorun daha");
            }
        }

        return passed ? 0 : 1;
    }
}