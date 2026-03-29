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
        bool tamamenGecti = kalan == 0;

        // ── BASLIK VE BADGE'LER ──
        string durumRenk = tamamenGecti ? "brightgreen" : "red";
        string durumText = tamamenGecti ? "GECTI" : "KALDI";
        string skorRenk = ortSkor >= 80 ? "brightgreen" : ortSkor >= 65 ? "yellow" : ortSkor >= 40 ? "orange" : "red";

        sb.AppendLine($"# AI OOP Analyzer - Kod Kalite Raporu");
        sb.AppendLine();
        sb.AppendLine($"![Sonuc](https://img.shields.io/badge/Sonuc-{durumText}-{durumRenk}?style=for-the-badge)");
        sb.AppendLine($"![Skor](https://img.shields.io/badge/Skor-{ortSkor:F0}%2F100-{skorRenk}?style=for-the-badge)");
        sb.AppendLine($"![Dosya](https://img.shields.io/badge/Dosya-{toplam}-blue?style=for-the-badge)");
        sb.AppendLine($"![Esik](https://img.shields.io/badge/Esik-{minScore}-lightgrey?style=for-the-badge)");
        sb.AppendLine();

        // ── GENEL OZET KUTUSU ──
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Genel Ozet");
        sb.AppendLine();

        // Progress bar (ASCII bloklar ile)
        int barLen = 20;
        int filled = (int)(ortSkor / 100.0 * barLen);
        if (filled > barLen) filled = barLen;
        string progressBar = new string('#', filled) + new string('-', barLen - filled);
        sb.AppendLine($"```");
        sb.AppendLine($"Ortalama Skor: [{progressBar}] {ortSkor:F1}/100");
        sb.AppendLine($"```");
        sb.AppendLine();

        sb.AppendLine("| Metrik | Deger |");
        sb.AppendLine("|:-------|------:|");
        sb.AppendLine($"| Analiz edilen dosya | **{toplam}** |");
        sb.AppendLine($"| Gecen dosya | **{gecen}** |");
        sb.AppendLine($"| Kalan dosya | **{kalan}** |");
        sb.AppendLine($"| Ortalama skor | **{ortSkor:F1}/100** |");
        sb.AppendLine($"| Minimum esik | **{minScore}/100** |");
        sb.AppendLine($"| Basari orani | **%{(toplam > 0 ? (double)gecen / toplam * 100 : 0):F0}** |");
        sb.AppendLine();

        // ── DOSYA BAZLI DETAY TABLOSU ──
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Dosya Detaylari");
        sb.AppendLine();
        sb.AppendLine("| Durum | Dosya | Skor | Kural | ML Tahmin | Sorun |");
        sb.AppendLine("|:-----:|-------|-----:|------:|-----------|------:|");

        foreach (var (dosya, sonuc, gecti) in sonuclar)
        {
            double kuralYuzde = sonuc.RuleBasedResult.MaxScore > 0
                ? (double)sonuc.RuleBasedResult.TotalScore / sonuc.RuleBasedResult.MaxScore * 100
                : 0;

            string durumIcon = gecti ? "[GECTI]" : "[KALDI]";
            int sorunSayisi = sonuc.RuleBasedResult.Issues.Count;

            sb.AppendLine($"| {durumIcon} | `{dosya}` | **{sonuc.CombinedScore:F1}** | %{kuralYuzde:F0} | {sonuc.MLResult.PredictedLabel} ({sonuc.MLResult.PredictedScore:F0}) | {sorunSayisi} |");
        }
        sb.AppendLine();

        // ── HER DOSYANIN SKOR CUBUGU ──
        sb.AppendLine("### Skor Dagilimi");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var (dosya, sonuc, gecti) in sonuclar)
        {
            string kısaDosya = dosya.Length > 35 ? "..." + dosya[^32..] : dosya;
            int bar = (int)(sonuc.CombinedScore / 100.0 * 25);
            if (bar > 25) bar = 25;
            string skorBar = new string('=', bar) + new string(' ', 25 - bar);
            string durumMark = gecti ? "OK" : "XX";
            sb.AppendLine($"  {kısaDosya,-35} |{skorBar}| {sonuc.CombinedScore,5:F1} [{durumMark}]");
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // ── KALAN DOSYALAR — DETAYLI SORUNLAR ──
        var kalanDosyalar = sonuclar.Where(s => !s.Gecti).ToList();
        if (kalanDosyalar.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Duzeltilmesi Gereken Dosyalar");
            sb.AppendLine();

            foreach (var (dosya, sonuc, _) in kalanDosyalar)
            {
                double kuralYuzde = sonuc.RuleBasedResult.MaxScore > 0
                    ? (double)sonuc.RuleBasedResult.TotalScore / sonuc.RuleBasedResult.MaxScore * 100
                    : 0;

                sb.AppendLine($"### `{dosya}`");
                sb.AppendLine();

                // Skor ozet satiri
                string dosyaSkorRenk = sonuc.CombinedScore >= 80 ? "brightgreen" : sonuc.CombinedScore >= 65 ? "yellow" : sonuc.CombinedScore >= 40 ? "orange" : "red";
                sb.AppendLine($"![Skor](https://img.shields.io/badge/Skor-{sonuc.CombinedScore:F0}%2F100-{dosyaSkorRenk}) ");
                sb.AppendLine($"![Kural](https://img.shields.io/badge/Kural-%25{kuralYuzde:F0}-blue) ");
                sb.AppendLine($"![ML](https://img.shields.io/badge/ML-{sonuc.MLResult.PredictedLabel}-{(sonuc.MLResult.PredictedLabel == "Good" ? "green" : "red")})");
                sb.AppendLine();

                // Kural bazli tablo
                sb.AppendLine("| Kural | Skor | Maks | Durum |");
                sb.AppendLine("|:------|-----:|-----:|:-----:|");
                foreach (var rule in sonuc.RuleBasedResult.RuleResults)
                {
                    string rd = rule.Score == rule.MaxScore ? "Uygun" : "**IHLAL**";
                    sb.AppendLine($"| {rule.RuleName} | {rule.Score} | {rule.MaxScore} | {rd} |");
                }
                sb.AppendLine();

                // Sorunlar
                if (sonuc.RuleBasedResult.Issues.Count > 0)
                {
                    sb.AppendLine($"<details>");
                    sb.AppendLine($"<summary><b>Tespit edilen sorunlar ({sonuc.RuleBasedResult.Issues.Count} adet)</b></summary>");
                    sb.AppendLine();

                    // Sorunlari kategoriye gore grupla
                    var grouped = sonuc.RuleBasedResult.Issues
                        .GroupBy(i => {
                            if (i.StartsWith("[Kapsulleme]")) return "Kapsulleme";
                            if (i.StartsWith("[Tek Sorumluluk]")) return "Tek Sorumluluk (SRP)";
                            if (i.StartsWith("[Bagimlilik Enjeksiyonu]")) return "Bagimlilik Enjeksiyonu (DI)";
                            if (i.StartsWith("[Arayuz]")) return "Arayuz (Interface)";
                            if (i.StartsWith("[Kalitim]")) return "Kalitim (Inheritance)";
                            if (i.StartsWith("[Polimorfizm]")) return "Polimorfizm";
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

                // Teknik detaylar (collapse)
                sb.AppendLine("<details>");
                sb.AppendLine("<summary>Teknik detaylar</summary>");
                sb.AppendLine();
                sb.AppendLine("| Ozellik | Deger |");
                sb.AppendLine("|:--------|------:|");
                sb.AppendLine($"| Sinif sayisi | {sonuc.Features.ClassCount} |");
                sb.AppendLine($"| Toplam metod | {sonuc.Features.TotalMethodCount} |");
                sb.AppendLine($"| Public alan | {sonuc.Features.PublicFieldCount} |");
                sb.AppendLine($"| Private alan | {sonuc.Features.PrivateFieldCount} |");
                sb.AppendLine($"| Kapsulleme orani | %{sonuc.Features.EncapsulationRatio * 100:F0} |");
                sb.AppendLine($"| Interface orani | %{sonuc.Features.InterfaceRatio * 100:F0} |");
                sb.AppendLine($"| new kullanimi | {sonuc.Features.ObjectCreationCount} |");
                sb.AppendLine($"| virtual metod | {sonuc.Features.VirtualMethodCount} |");
                sb.AppendLine($"| override metod | {sonuc.Features.OverrideMethodCount} |");
                sb.AppendLine($"| ML guven | %{sonuc.MLResult.Confidence * 100:F0} |");
                sb.AppendLine($"| En yakin ornekler | {string.Join(", ", sonuc.MLResult.NearestNeighbors)} |");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }

        // ── GECEN DOSYALAR ──
        var gecenDosyalar = sonuclar.Where(s => s.Gecti).ToList();
        if (gecenDosyalar.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Basarili Dosyalar");
            sb.AppendLine();

            foreach (var (dosya, sonuc, _) in gecenDosyalar)
            {
                int sorunSayisi = sonuc.RuleBasedResult.Issues.Count;
                string ek = sorunSayisi > 0 ? $" -- {sorunSayisi} kucuk sorun" : " -- sorun yok";
                string dSkorRenk = sonuc.CombinedScore >= 80 ? "brightgreen" : "yellow";
                sb.AppendLine($"- ![ok](https://img.shields.io/badge/-{sonuc.CombinedScore:F0}%2F100-{dSkorRenk}?style=flat-square) `{dosya}`{ek}");
            }
            sb.AppendLine();

            // Gecen dosyalarin da kurallarini goster (collapse)
            foreach (var (dosya, sonuc, _) in gecenDosyalar)
            {
                if (sonuc.RuleBasedResult.Issues.Count > 0)
                {
                    sb.AppendLine($"<details>");
                    sb.AppendLine($"<summary><code>{dosya}</code> - kucuk sorunlar ({sonuc.RuleBasedResult.Issues.Count})</summary>");
                    sb.AppendLine();
                    foreach (var issue in sonuc.RuleBasedResult.Issues)
                    {
                        sb.AppendLine($"- {issue}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }
            }
        }

        // ── FOOTER ──
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"> **AI OOP Analyzer v2.0** -- Kural bazli + ML hibrit analiz");
        sb.AppendLine($"> ");
        sb.AppendLine($"> Analiz: {DateTime.Now:yyyy-MM-dd HH:mm} | Esik: {minScore}/100 | Model: k-NN (k=3)");

        // Dosyaya yaz
        var dir = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(reportPath, sb.ToString());
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