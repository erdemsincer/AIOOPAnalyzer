<div align="center">

# AI OOP Analyzer
### Kural Bazli + ML Hibrit Kod Kalite Raporu

![Sonuc](https://img.shields.io/badge/Sonuc-GECTI-brightgreen?style=for-the-badge&logo=checkmarx)
![Skor](https://img.shields.io/badge/Skor-72%2F100_(B)-yellow?style=for-the-badge&logo=speedtest)
![Dosya](https://img.shields.io/badge/Dosya-1-blue?style=for-the-badge&logo=files)
![Model](https://img.shields.io/badge/Model-k--NN_(k%3D3)-purple?style=for-the-badge&logo=tensorflow)
![CK](https://img.shields.io/badge/CK_Metrikleri-6_Metrik-teal?style=for-the-badge&logo=pocketcasts)

</div>

---

## Genel Dashboard

> **Ortalama Skor: 71,8/100 (B) — Iyi**
> 
> `[#####################.........]`

| Metrik | Deger | Analiz | Deger |
|:----------|:-----:|:----------|:-----:|
| Analiz edilen dosya | **1** | Minimum esik | **65/100** |
| Gecen dosya | **1** | Ortalama skor | **71,8/100** |
| Kalan dosya | **0** | En yuksek skor | **71,8/100** |
| Basari orani | **%100** | En dusuk skor | **71,8/100** |

---

## Dosya Bazli Sonuclar

| Durum | Dosya | Birlesik Skor | Kural Skoru | ML Tahmin | ML Guven | Sorun |
|:-----:|-------|:-------------:|:-----------:|:---------:|:--------:|:-----:|
| GECTI | `examples/test-pr-demo.cs` | **71,8** | %57 (65/115) | Good (95) | %100 | 9 |

### Skor Dagilimi

```
  test-pr-demo.cs                |#####################.........|  71,8 [PASS]
  -- Esik --                     |-                             |    65
```

---

## Basarili Dosyalar

<details>
<summary><code>examples/test-pr-demo.cs</code> — 71,8/100 (B) GECTI</summary>

![Skor](https://img.shields.io/badge/Skor-72%2F100_(B)-yellow?style=flat-square) 
![Kural](https://img.shields.io/badge/Kural-%2557-blue?style=flat-square) 
![ML](https://img.shields.io/badge/ML-Good_(95)-green?style=flat-square) 
![Guven](https://img.shields.io/badge/Guven-%25100-blueviolet?style=flat-square)

| Kural | Puan | Maks | Oran | Durum |
|:------|:----:|:----:|:----:|:-----:|
| Encapsulation | 15 | 15 | `########` %100 | Tam |
| SRP | 10 | 15 | `#####...` %67 | Ihlal |
| Dependency Injection | 10 | 20 | `####....` %50 | Ihlal |
| Interfaces | 5 | 15 | `##......` %33 | Ihlal |
| Inheritance | 0 | 15 | `........` %0 | Ihlal |
| Polymorphism | 10 | 20 | `####....` %50 | Ihlal |
| CK Metrics | 15 | 15 | `########` %100 | Tam |

**Kucuk sorunlar (9 adet):**

- [Tek Sorumluluk] 'InMemoryProductRepository' sinifi cok fazla metod iceriyor (3 metod, esik: 2).
- [Bagimlilik Enjeksiyonu] 'Product' sinifi 'ArgumentException' nesnesini 'new' ile olusturuyor. Constructor injection kullanilmali.
- [Arayuz] 'Product' sinifi hicbir interface implement etmiyor.
- [Arayuz] 'ProductService' sinifi hicbir interface implement etmiyor.
- [Kalitim] 'Product' sinifi hicbir base siniftan turetilmemis.
- [Kalitim] 'InMemoryProductRepository' sinifi hicbir base siniftan turetilmemis.
- [Kalitim] 'ProductService' sinifi hicbir base siniftan turetilmemis.
- [Kalitim] 'EmailNotifier' sinifi hicbir base siniftan turetilmemis.
- [Polimorfizm] Kodda hic virtual veya override metod bulunamadi. Polimorfizm kullanilmiyor.

<details>
<summary><b>Teknik Detaylar</b></summary>

| Ozellik | Deger | | Ozellik | Deger |
|:--------|:-----:|-|:--------|:-----:|
| Sinif sayisi | 4 | | Kapsulleme | %100 |
| Toplam metod | 7 | | Interface orani | %50 |
| Public alan | 0 | | virtual metod | 0 |
| Private alan | 3 | | override metod | 0 |
| new kullanimi | 1 | | ML guven | %100 |
| | | | Yakin ornekler | good-17, good-19, good-02 |

</details>

<details>
<summary><b>CK Metrikleri — Sinif Bazli</b></summary>

| Sinif | WMC | DIT | NOC | CBO | RFC | LCOM | Not |
|:------|:---:|:---:|:---:|:---:|:---:|:----:|:---:|
| `Product` | 2 | 0 | 0 | 1 | 1 | 0 | A+ |
| `InMemoryProductRepository` | 3 | 0 | 0 | 3 | 4 | 0 | A+ |
| `ProductService` | 2 | 0 | 0 | 3 | 5 | 0 | A+ |
| `EmailNotifier` | 1 | 0 | 0 | 1 | 2 | 0 | A+ |
| **Ortalama** | **2** | **0** | **0** | **2** | **3** | **0** | — |

</details>

</details>

---

## CK Metrikleri — Genel Ozet (Chidamber & Kemerer)

> **Toplam 4 sinif analiz edildi** — 4 sinif tum esikleri gecti (100%)

| Metrik | Aciklama | Esik | Ihlal | Oran | Durum |
|:------:|:---------|:----:|:-----:|:----:|:-----:|
| **WMC** | Agirlikli Metod Sayisi | <= 10 | 0/4 | %100 | OK |
| **DIT** | Kalitim Derinligi | <= 3 | 0/4 | %100 | OK |
| **NOC** | Alt Sinif Sayisi | <= 5 | — | — | - |
| **CBO** | Siniflar Arasi Bagimlilik | <= 5 | 0/4 | %100 | OK |
| **RFC** | Sinif Yanit Sayisi | <= 20 | 0/4 | %100 | OK |
| **LCOM** | Uyumsuzluk (Cohesion) | <= 3 | 0/4 | %100 | OK |

---

## Iyilestirme Onerileri

### Bagimlilik Enjeksiyonu (1 sorun)

```csharp
// Kotu: Siki bagimlilik
public class Service {
    private repo = new Repository();
}

// Iyi: Constructor Injection
public class Service {
    private readonly IRepository _repo;
    public Service(IRepository repo) => _repo = repo;
}
```

### Interface Kullanimi (2 sorun)

```csharp
// Kotu: Concrete sinifa bagimlilik
public class OrderService { }

// Iyi: Interface ile soyutlama
public interface IOrderService { }
public class OrderService : IOrderService { }
```

### Tek Sorumluluk Prensibi (1 sorun)

> Bir sinif yalnizca bir isten sorumlu olmalidir. Cok fazla metod varsa sinifi bolerek
> her birinin tek bir sorumlulugu olmasini saglayin.

### Kalitim (4 sorun)

```csharp
// Ortak davranisi base sinifta toplayin
public abstract class BaseEntity {
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class User : BaseEntity { }
```

### Polimorfizm (1 sorun)

```csharp
// virtual + override ile polimorfizm
public class Shape {
    public virtual double Area() => 0;
}
public class Circle : Shape {
    public override double Area() => Math.PI * R * R;
}
```

---

<details>
<summary><b>Analiz Metodolojisi — Nasil calisir?</b></summary>

### Hibrit Analiz Sistemi

Bu rapor, iki bagimsiz analiz motorunun birlesik sonucudur:

| Motor | Agirlik | Aciklama |
|:------|:-------:|:---------|
| **Kural Bazli** | %60 | 7 OOP kurali + CK metrikleri kontrol edilir |
| **ML (k-NN)** | %40 | 50 ornek uzerinde egitilmis k-NN modeli |

**Formul:** `Q = 0.60 x Kural(%) + 0.40 x ML(skor)`

### Kontrol Edilen OOP Kurallari

| # | Kural | Maks Puan | Ne kontrol eder? |
|:-:|:------|:---------:|:-----------------|
| 1 | Kapsulleme | 15 | Public alanlar private/property olmali |
| 2 | Tek Sorumluluk (SRP) | 15 | Sinif basina metod sayisi |
| 3 | Bagimlilik Enjeksiyonu | 20 | `new` yerine constructor injection |
| 4 | Interface Kullanimi | 15 | Siniflarin interface implement etmesi |
| 5 | Kalitim | 15 | Base sinif kullanimi |
| 6 | Polimorfizm | 20 | virtual/override metod kullanimi |
| 7 | CK Metrikleri | 15 | WMC, DIT, NOC, CBO, RFC, LCOM |

### CK Metrikleri Esik Degerleri

| Metrik | Tam Adi | Esik | Yuksekse ne olur? |
|:------:|:--------|:----:|:------------------|
| WMC | Weighted Methods per Class | <= 10 | Sinif cok karmasik, bolunmeli |
| DIT | Depth of Inheritance Tree | <= 3 | Kalitim zinciri cok derin |
| NOC | Number of Children | <= 5 | Cok fazla alt sinif, soyutlama gerekli |
| CBO | Coupling Between Objects | <= 5 | Siki bagimlilik, interface kullanin |
| RFC | Response for a Class | <= 20 | Cok fazla metod cagrisi |
| LCOM | Lack of Cohesion of Methods | <= 3 | Metotlar iliskisiz, sinif bolunmeli |

</details>

---

<div align="center">

**AI OOP Analyzer v2.0** — Kural Bazli + ML Hibrit Analiz + CK Metrikleri

2026-03-30 23:06 | Esik: 65/100 | Model: k-NN (k=3) | CK: Chidamber & Kemerer

![.NET](https://img.shields.io/badge/.NET_8-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Roslyn](https://img.shields.io/badge/Roslyn-189BDD?style=flat-square&logo=visual-studio&logoColor=white)
![ML](https://img.shields.io/badge/k--NN_ML-FF6F61?style=flat-square&logo=tensorflow&logoColor=white)

</div>
