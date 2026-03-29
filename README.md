# AI OOP Analyzer

C# kodlarinin OOP/SOLID kalitesini analiz eden hibrit (kural bazli + ML) arac.
PR acildiginda otomatik calisir, kod kalitesini olcer ve sonucu PR yorumu olarak yazar.

## Proje Yapisi

```
AIOOPAnalyzer/
|-- Program.cs                  # Ana giris noktasi (train, analyze, batch, pipeline, pr-check)
|-- AIOOPAnalyzer.csproj        # Proje dosyasi
|
|-- Analyzers/                  # OOP/SOLID kural analizciileri
|   |-- Analyzers.cs            # IAnalyzer arayuzu
|   |-- EncapsulationAnalyzer.cs
|   |-- SRPAnalyzer.cs
|   |-- DIAnalyzer.cs
|   |-- InterfaceAnalyzer.cs
|   |-- InheritanceAnalyzer.cs
|   +-- PolymorphismAnalyzer.cs
|
|-- Models/                     # Veri modelleri
|   |-- CodeStructure.cs        # Roslyn parse sonucu (ClassInfo, MethodInfo)
|   |-- TrainingFeatures.cs     # 20 ozellik vektoru
|   |-- AnalysisResult.cs       # Kural bazli analiz sonucu
|   |-- PredictionResult.cs     # ML tahmin sonucu
|   |-- RuleResult.cs           # Tekil kural sonucu
|   |-- RulesConfig.cs          # rules.json modeli
|   |-- HybridConfig.cs         # hybrid.json modeli
|   |-- DatasetItem.cs          # Veri seti ogesi
|   +-- TrainedModel.cs         # Egitilmis model yapisi
|
|-- Services/                   # Is mantigi servisleri
|   |-- CodeParserService.cs    # Roslyn AST parser
|   |-- FeatureExtractor.cs     # 20 ozellik cikarimi
|   |-- ModelTrainer.cs         # k-NN model egitimi + LOO cross-validation
|   |-- ModelPredictor.cs       # k-NN tahmin (k=3, agirlikli)
|   |-- HybridAnalyzer.cs      # Kural + ML hibrit birlestirme
|   |-- AnalyzerService.cs      # 6 analizci kayit/yonetimi
|   |-- ConfigLoader.cs         # JSON config yukleyici
|   |-- DatasetLoader.cs        # Veri seti yukleyici
|   |-- DatasetValidator.cs     # Veri seti dogrulayici
|   +-- BatchEvaluationMetrics.cs # Karisiklik matrisi, dogruluk metrikleri
|
|-- config/                     # Yapilandirma dosyalari
|   |-- rules.json              # 6 kural tanimlari (maks puan, ceza)
|   +-- hybrid.json             # Hibrit agirliklar ve esik degerleri
|
|-- data/
|   +-- dataset.json            # 40 etiketli egitim ornegi (20 Good + 20 Bad)
|
|-- tests/                      # Ornek test dosyalari (derlemeye dahil degil)
|   |-- test-good.cs            # Iyi OOP ornegi
|   |-- test-good-report.cs     # Iyi OOP ornegi (ReportService)
|   |-- test-bad-shopping.cs    # Kotu OOP ornegi (ShoppingCart)
|   |-- test-bad-student.cs     # Kotu OOP ornegi (StudentManager)
|   |-- test-bad-payment.cs     # Kotu OOP ornegi (PaymentService)
|   +-- test-mid.cs             # Orta seviye ornek
|
+-- .github/workflows/
    +-- oop-analyzer.yml        # GitHub Actions PR kontrol workflow'u
```

## Nasil Calisir?

```
  C# Kodu
     |
     v
  Roslyn Parser (AST olusturur)
     |
     +------------------------+
     |                        |
     v                        v
  6 Kural Kontrolu       20 Ozellik Cikarimi
  (Encapsulation, SRP,    (sinif/alan/metod
   DI, Interface,          sayilari, oranlar)
   Inheritance,                  |
   Polymorphism)                 v
     |                    k-NN ML Tahmini
     |                    (en yakin 3 ornege bak)
     |                        |
     +------------------------+
     |
     v
  Hibrit Skor Q = w_k * K + w_m * M  (agirliklar: config/hybrid.json)
     |
     v
  Final Karar: Good / Bad
```

## Kurulum

```bash
# 1. Repo'yu klonla
git clone <repo-url>
cd AIOOPAnalyzer

# 2. Derle
dotnet build

# 3. Modeli egit (ilk seferde gerekli)
dotnet run train
```

## Kullanim

### Yerel Kullanim

```bash
# Tek dosya analiz et
dotnet run analyze dosya.cs

# Interaktif mod (kod yapistir)
dotnet run analyze

# Tum veri setini test et (k-NN / yalniz kural / hibrit karsilastirmasi, karisiklik matrisi)
dotnet run batch
```

### CI/CD Pipeline Kullanimi

```bash
# PR'daki degisen dosyalari analiz et
dotnet run pr-check Services/OrderService.cs Models/Order.cs

# Markdown rapor olustur
dotnet run pr-check dosya.cs --report=rapor.md

# Ozel esik degeri (varsayilan: config/hybrid.json -> qualityThreshold)
dotnet run pr-check dosya.cs --min-score=80

# Tek dosya pipeline
dotnet run pipeline dosya.cs --json
```

### Exit Kodlari

| Kod | Anlam |
|-----|-------|
| 0 | Basarili — Tum dosyalar kaliteli |
| 1 | Basarisiz — En az bir dosya kalitesiz |
| 2 | Hata — Model yok, dosya bulunamadi vb. |

## GitHub Actions Entegrasyonu

`.github/workflows/oop-analyzer.yml` dosyasi repo'da hazir.

### Ne Yapar?

1. PR acildiginda veya guncellediginde tetiklenir
2. Sadece degisen `.cs` dosyalarini bulur
3. Her dosyayi OOP/SOLID kurallarinda analiz eder
4. ML modeliyle karsilastirir (Good/Bad tahmini)
5. Sonucu PR'a yorum olarak ekler
6. Kalitesiz kod varsa merge'u bloklar

### Ornek PR Yorumu

PR'a otomatik eklenen rapor ornegi:

- **Shield.io badge'leri** ile sonuc, skor, dosya sayisi ve esik degeri
- **ASCII progress bar** ile ortalama skor gosterimi
- **Skor dagilim cubugu** ile tum dosyalarin karsilastirmasi
- **Kategorili sorun gruplama** (Kapsulleme, SRP, DI, Arayuz, Kalitim, Polimorfizm)
- **Collapse bolumler** ile teknik detaylar

Ornek goruntu icin herhangi bir PR'in yorumlarini inceleyin.

### Merge Korumasi Ayarlama

GitHub'da merge'u bloklamak icin:

1. **Settings** > **Branches** > **Branch protection rules**
2. `main` branch'i icin kural ekle
3. **Require status checks to pass before merging** secenegini ac
4. **OOP Kalite Analizi** check'ini zorunlu yap

Boylece kalitesiz kod merge edilemez.

## Kurallar

| Kural | Maks Puan | Ceza | Kontrol |
|-------|-----------|------|---------|
| Encapsulation | 15 | -5 | Public alanlar private/property olmali |
| SRP | 15 | -5 | Sinif basi max 2 metod |
| DI | 20 | -10 | new yerine constructor injection |
| Interfaces | 15 | -5 | Siniflar interface implement etmeli |
| Inheritance | 15 | -5 | Uygun yerlerde kalitim |
| Polymorphism | 20 | -10 | virtual/override dogru kullanim |

Kurallar `config/rules.json` dosyasindan ayarlanabilir.

## Hibrit kalite tanimi (`config/hybrid.json`)

Tek bir yerde tanimlanir: birlesik skor **Q**, agirliklar ve Good/Bad kararinda kullanilan esikler.

| Alan | Aciklama |
|------|----------|
| `ruleWeight`, `mlWeight` | Q = w_k * K + w_m * M (K: kural yuzdesi 0-100, M: k-NN skoru 0-100). Toplam 1 degilse agirliklar otomatik normalize edilir. |
| `qualityThreshold` | Guclu uyum dallarindan sonra hala kararsiz kalinirsa: Q >= esik ise Good. `pr-check` ve `pipeline` icin `--min-score` verilmezse bu deger kullanilir. |
| `strongAgreementHighRulePercent` | K bu degerin uzerinde ve ML Good ise -> Good. |
| `strongAgreementLowRulePercent` | K bu degerin altinda ve ML Bad ise -> Bad. |

**Yalniz kural (ablation):** Good <=> K >= `qualityThreshold` (batch istatistiklerinde kullanilir).

### Batch ciktisi

`dotnet run batch` calistirildiginda veri setindeki gercek etiketlere gore:

- **Yalniz k-NN (ML):** Tahmin = k-NN etiketi
- **Yalniz kural:** Tahmin = K >= `qualityThreshold` ise Good
- **Hibrit:** Tahmin = `FinalVerdict` (yukaridaki kurallar + Q)

Her biri icin dogruluk, 2x2 karisiklik matrisi, sinif bazinda recall/precision ozetlenir; ayrica kalite tanimi metin olarak yazdirilir.

### Pipeline JSON

`dotnet run pipeline dosya.cs --json` ciktisinda `kalite_tanimi` nesnesi (agirliklar ve esikler) tekrarlanabilirlik icin yer alir.

## Veri Seti

`data/dataset.json` dosyasinda 40 etiketli ornek (20 Good + 20 Bad) var.
Kendi orneklerinizi ekleyerek modeli guclendirebilirsiniz.

## Teknolojiler

- **.NET 8.0** — Runtime
- **Roslyn** — C# syntax tree analizi
- **k-NN** — ML tahmini (pure C#, harici ML kutuphanesi yok)
- **GitHub Actions** — CI/CD entegrasyonu
