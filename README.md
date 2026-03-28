# AI OOP Analyzer

C# kodlarinin OOP/SOLID kalitesini analiz eden hibrit (kural bazli + ML) arac.
PR acildiginda otomatik calisir, kod kalitesini olcer ve sonucu PR yorumu olarak yazar.

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

```
## AI OOP Analyzer - PR Kontrol Sonucu: KALDI

### Genel Ozet

| Metrik | Deger |
|--------|-------|
| Analiz edilen dosya | 3 |
| Gecen dosya | 1 |
| Kalan dosya | 2 |
| Ortalama skor | 40.6/100 |

### Dosya Detaylari

| Dosya | Skor | Kural | ML | Karar |
|-------|------|-------|----|-------|
| OrderService.cs | 24.6/100 | 30% | Bad (17) | KALDI |
| Order.cs | 72.5/100 | 60% | Good (91) | GECTI |
```

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
