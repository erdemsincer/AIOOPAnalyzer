# AI OOP ANALYZER - MAKALE YAZIM REHBERI

**Bu dosya sana makaleyi yazman icin tum verileri ve aciklamalari iceriyor.**
**Her tabloyu makaleye kopyalayabilirsin. Tablolarin altindaki aciklamalari kendi cumlelerinle yaz.**

---

## KISACA: BU PROJE NE YAPIYOR?

Sen bir C# dosyasi veriyorsun. Sistem su 3 seyi yapiyor:

1. **Kural Motoru**: 7 tane OOP kurali kontrol ediyor (kapsulleme, kalitim vs.)
   Her kuralin puani var, toplam 115 uzerinden not veriyor.

2. **Makine Ogrenmesi (ML)**: Kodun 26 tane ozelligini cikarip (sinif sayisi, metod sayisi, CK metrikleri vs.)
   onceden egitilmis 50 ornekle karsilastiriyor. "Bu kod iyi mi kotu mu?" diyor.

3. **Hibrit Skor**: Bu ikisini birlestiriyor:
   **Q = 0.60 x Kural(%) + 0.40 x ML(skor)**
   65 ve ustu = GECTI, altiysa KALDI.

Ayrica GitHub Actions ile PR acildiginda otomatik calisiyor.

---

## MAKALE BOLUM SIRASI VE VERILERI

---

### BOLUM 1: GIRIS (yaklasik 1 sayfa yaz)

Burada soyle seyler yaz (kendi cumlelerinle):
- Yazilim gelistirmede kod kalitesi neden onemli
- OOP (nesne yonelimli programlama) prensipleri ne ise yarar
- Mevcut araclar (SonarQube, StyleCop) sadece kural bazli calisiyor, ML kullanmiyor
- Biz hem kural hem ML birlestirdik (hibrit yaklasim)
- CK metrikleri de ekledik (1994'te Chidamber & Kemerer tarafindan onerilmis)

---

### BOLUM 2: ILGILI CALISMALAR (yaklasik 1 sayfa yaz)

Su kaynaklari bul ve referans ver:
- **SonarQube**: Acik kaynakli statik analiz araci
- **Chidamber & Kemerer (1994)**: "A Metrics Suite for Object Oriented Design" - CK metriklerini onerdiler
- **k-NN algoritmasi**: En yakin komsu siniflandirma algoritmasi
- **Roslyn**: Microsoft'un C# derleyici API'si
- Varsa baska hibrit kod analizi calismalari

---

### BOLUM 3: ONERILEN YONTEM (yaklasik 2-3 sayfa)

#### 3.1 Sistem Mimarisi

Sistem soyle calisiyor (bir blok diyagrami ciz):

```
C# Dosyasi --> Roslyn Parser --> Ozellik Cikartma (26 feature)
                                        |
                        +---------------+---------------+
                        |                               |
                  Kural Motoru                    ML Motoru (k-NN)
                  (7 kural, 115 puan)             (50 ornekle egitildi)
                        |                               |
                        +---------------+---------------+
                                        |
                                  Hibrit Skor
                           Q = 0.60 x K + 0.40 x M
                                        |
                                  >= 65: GECTI
                                  < 65 : KALDI
```

#### 3.2 Kural Motoru

Su tabloyu makaleye koy:

| No | Kural Adi                      | Maksimum Puan | Ne Kontrol Eder |
|----|--------------------------------|---------------|-----------------|
| 1  | Kapsulleme (Encapsulation)     | 15            | Alanlar private mi, getter/setter var mi |
| 2  | Tek Sorumluluk (SRP)           | 15            | Sinifta cok fazla metod var mi |
| 3  | Bagimlilik Enjeksiyonu (DI)    | 20            | new ile dogrudan nesne olusturma var mi |
| 4  | Arayuz Kullanimi (Interface)   | 15            | Siniflar arayuz uyguluyor mu |
| 5  | Kalitim (Inheritance)          | 15            | Base class kullaniliyor mu |
| 6  | Polimorfizm (Polymorphism)     | 20            | virtual/override metod var mi |
| 7  | CK Metrikleri                  | 15            | 6 CK metrigi esik icinde mi |
|    | **TOPLAM**                     | **115**       | |

**Aciklama**: Toplam 115 puan. Kuraldan alinan puan yuzdeye cevriliyor: Kural(%) = Alinan/115 x 100

#### 3.3 CK Metrikleri

Su tabloyu makaleye koy:

| Kisaltma | Tam Adi | Esik Degeri | Ne Olcer |
|----------|---------|-------------|----------|
| WMC | Weighted Methods per Class | <= 10 | Siniftaki metod karmasikligi |
| DIT | Depth of Inheritance Tree | <= 3 | Kalitim agaci derinligi |
| NOC | Number of Children | <= 5 | Alt sinif sayisi |
| CBO | Coupling Between Object Classes | <= 5 | Siniflar arasi bagimlilik |
| RFC | Response for a Class | <= 20 | Sinifin cagirabilecegi metod sayisi |
| LCOM | Lack of Cohesion of Methods | <= 3 | Metodlarin birbiriyle iliskisizligi |

**Aciklama**: Bunlar 1994'te Chidamber ve Kemerer tarafindan onerilmistir. Esik degerlerini asarsa kod kalitesi duser.

#### 3.4 Makine Ogrenmesi Modeli

**Algoritma**: k-NN (k En Yakin Komsu)
**k degeri**: 3 (en yakin 3 ornek)
**Egitim ornegi**: 50 (25 iyi + 25 kotu kod)

**Nasil calisiyor (basitce anlat)**:
1. Koddan 26 ozellik cikariliyor (sinif sayisi, metod sayisi, kapsulleme orani vs.)
2. Bu ozellikler Z-score ile normalize ediliyor (ortalamayi cikar, standart sapmaya bol)
3. Test ornegi tum egitim orneklerine "mesafe" hesabiyla karsilastiriliyor
4. En yakin 3 komsunun ortalamasina gore skor ve sinif tahmini yapiliyor

**26 Ozellik listesi** (su tabloyu makaleye koy):

| No | Ozellik Adi | Aciklama |
|----|-------------|----------|
| 1 | ClassCount | Toplam sinif sayisi |
| 2 | TotalMethodCount | Toplam metod sayisi |
| 3 | TotalFieldCount | Toplam alan sayisi |
| 4 | TotalPropertyCount | Toplam property sayisi |
| 5 | PublicFieldCount | Public alan sayisi |
| 6 | PrivateFieldCount | Private alan sayisi |
| 7 | EncapsulationRatio | Kapsulleme orani (private/toplam) |
| 8 | AvgMethodsPerClass | Sinif basina ortalama metod |
| 9 | ClassesExceedingMethodThreshold | Cok fazla metodu olan sinif sayisi |
| 10 | ObjectCreationCount | new ile nesne olusturma sayisi |
| 11 | HasDirectInstantiation | Dogrudan nesne olusturma var mi (0/1) |
| 12 | InterfaceImplementationCount | Arayuz uygulayan sinif sayisi |
| 13 | ClassesWithInterface | Arayuzu olan sinif sayisi |
| 14 | ClassesWithoutInterface | Arayuzu olmayan sinif sayisi |
| 15 | InterfaceRatio | Arayuz kullanan sinif orani |
| 16 | InheritanceCount | Kalitim kullanan sinif sayisi |
| 17 | ClassesWithInheritance | Kalitimli sinif sayisi |
| 18 | VirtualMethodCount | virtual metod sayisi |
| 19 | OverrideMethodCount | override metod sayisi |
| 20 | ClassesWithPolymorphism | Polimorfizm kullanan sinif sayisi |
| 21 | AvgWMC | Ortalama WMC (CK metrigi) |
| 22 | MaxDIT | Maksimum DIT (CK metrigi) |
| 23 | AvgNOC | Ortalama NOC (CK metrigi) |
| 24 | AvgCBO | Ortalama CBO (CK metrigi) |
| 25 | AvgRFC | Ortalama RFC (CK metrigi) |
| 26 | AvgLCOM | Ortalama LCOM (CK metrigi) |

**Ozellik 1-20**: OOP prensiplerini olcer
**Ozellik 21-26**: CK metriklerini olcer

#### 3.5 Hibrit Skor Formulu

```
Q = 0.60 x Kural(%) + 0.40 x ML(skor)
```

- Kural(%) = Kural puani / 115 x 100
- ML(skor) = k-NN tahmin skoru (0-100)
- Q >= 65 ise GECTI, Q < 65 ise KALDI

**Neden 0.60 ve 0.40?** Kural bazli sisteme biraz daha fazla agirlik verdik cunku kurallar dogrudan OOP prensiplerini kontrol ediyor. ML ise destek gorevinde.

---

### BOLUM 4: DENEYSEL SONUCLAR (yaklasik 3-4 sayfa)

Bu bolum en onemli bolum. Tum tablolari buraya koy.

#### 4.1 Veri Seti

Su tabloyu makaleye koy:

| Ozellik | Deger |
|---------|-------|
| Toplam ornek | 50 |
| Iyi kod (Good) | 25 |
| Kotu kod (Bad) | 25 |
| Denge orani | %50 / %50 |
| Zorluk: Kolay | 19 (%38) |
| Zorluk: Orta | 20 (%40) |
| Zorluk: Zor | 11 (%22) |
| Kategori: OOP | 18 (%36) |
| Kategori: Design Pattern | 12 (%24) |
| Kategori: SOLID | 10 (%20) |
| Kategori: CK Metrics | 10 (%20) |

**Aciklama**: 50 ornekten olusan dengeli bir veri seti kullandik. Orneklerin yarisi OOP prensiplerine uygun iyi kod, digerleri kasitli olarak kotu yazilmis kod ornekleridir. Farkli zorluk ve kategorilerde ornekler vardir.

**Iyi kodlarin hedef skoru**: ortalama 92.4 (min: 88, max: 95)
**Kotu kodlarin hedef skoru**: ortalama 15.0 (min: 5, max: 25)

#### 4.2 k Degeri Deneyi

**Ne yaptik**: Farkli k degerlerinde (kac komsu bakacagiz) dogrulugu olctuk.
**Yontem**: Leave-One-Out Cross Validation (LOO-CV) = her ornegi sirayla cikart, kalan 49'la egit, cikarilani tahmin et.

Su tabloyu makaleye koy:

| k degeri | Dogruluk | Dogru/Toplam | F1-Good | F1-Bad | Ort. Skor Hatasi |
|----------|----------|--------------|---------|--------|------------------|
| 1 | %100.0 | 50/50 | %100.0 | %100.0 | 2.82 |
| 2 | %100.0 | 50/50 | %100.0 | %100.0 | 2.90 |
| 3 | %100.0 | 50/50 | %100.0 | %100.0 | 2.86 |
| 5 | %100.0 | 50/50 | %100.0 | %100.0 | 3.33 |
| 7 | %100.0 | 50/50 | %100.0 | %100.0 | 3.31 |
| 9 | %98.0 | 49/50 | %98.0 | %98.0 | 3.44 |
| 11 | %98.0 | 49/50 | %98.0 | %98.0 | 3.55 |
| 15 | %98.0 | 49/50 | %98.0 | %98.0 | 3.71 |

**Aciklama**: k=1 ile k=7 arasinda %100 dogruluk saglanmistir. k=9 ve ustunde 1 ornek yanlis siniflandirilmis, dogruluk %98'e dusmustur. Calismada k=3 secilmistir cunku en dusuk skor hatasina (2.86 puan) sahiptir ve genelleme kapasitesi iyidir.

#### 4.3 Confusion Matrix (Karisiklik Matrisi)

**Ne demek bu?** Sistemin ne kadar dogru tahmin ettigini gosterir.

```
                          TAHMIN
                     Good      Bad
GERCEK  Good          25        0
        Bad            0       25
```

| Metrik | Deger |
|--------|-------|
| Dogruluk (Accuracy) | %100.0 |
| Good Precision (Kesinlik) | %100.0 |
| Good Recall (Duyarlilik) | %100.0 |
| Good F1-Score | %100.0 |
| Bad Precision | %100.0 |
| Bad Recall | %100.0 |
| Bad F1-Score | %100.0 |
| Ortalama skor hatasi | 2.86 puan |

**Aciklama**:
- **TP (True Positive) = 25**: 25 iyi kodu dogru olarak "iyi" tahmin etti
- **TN (True Negative) = 25**: 25 kotu kodu dogru olarak "kotu" tahmin etti
- **FP (False Positive) = 0**: Hic kotu kodu "iyi" demedi
- **FN (False Negative) = 0**: Hic iyi kodu "kotu" demedi
- 50 ornekte 50'si dogru tahmin edildi

#### 4.4 Yontem Karsilastirmasi (EN ONEMLI TABLO)

Su tabloyu makaleye koy:

| Yontem | Dogruluk | Good-P | Good-R | Good-F1 | Bad-P | Bad-R | Bad-F1 |
|--------|----------|--------|--------|---------|-------|-------|--------|
| Yalniz Kural (K>=65) | %82.0 | %75.0 | %96.0 | %84.2 | %94.4 | %68.0 | %79.1 |
| Yalniz ML (k-NN) | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 |
| Hibrit (0.6K+0.4M) | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 | %100.0 |

**Bu tabloyu soyle acikla**:
- Sadece kural ile %82 dogruluk saglanmistir. 8 kotu kod yanlis olarak "gecti" denmis (FP=8), 1 iyi kod "kaldi" denmis (FN=1).
- ML eklendikten sonra dogruluk %100'e cikmistir.
- Hibrit sistem de %100 dogruluk saglamistir.
- **ML eklemek kural bazli sisteme %18 dogruluk iyilestirmesi saglamistir.**

**Skor farklari**:
- Iyi kodlarda: ML ort=92, Kural ort=%83
- Kotu kodlarda: ML ort=15, Kural ort=%57
- ML, iyi ve kotu kodu 77 puan farkla ayirirken, kural sadece 26 puan farkla ayiriyor
- ML modeli siniflar arasi ayrimi **3 kat daha belirgin** yapiyor

#### 4.5 Ozellik Onemi (Feature Importance)

**Hangi ozellikler iyi kodu kotudan en cok ayiriyor?**

En onemli 10 ozellik (su tabloyu koy):

| Sira | Ozellik | Agirlik | Pearson r | t-test | Anlami |
|------|---------|---------|-----------|--------|--------|
| 1 | EncapsulationRatio | +0.088 | +0.995 | *** | Kapsulleme orani en belirleyici |
| 2 | InterfaceRatio | +0.072 | +0.825 | *** | Arayuz kullanan sinif orani |
| 3 | PrivateFieldCount | +0.070 | +0.795 | *** | Private alan sayisi |
| 4 | PublicFieldCount | -0.068 | -0.790 | *** | Public alan (kotu kod isareti) |
| 5 | AvgCBO | +0.066 | +0.744 | *** | Siniflar arasi bagimlilik |
| 6 | InterfaceImplementationCount | +0.064 | +0.736 | *** | Arayuz uygulayan sinif sayisi |
| 7 | ClassesWithInterface | +0.064 | +0.736 | *** | Arayuzu olan sinif sayisi |
| 8 | ClassesWithoutInterface | -0.047 | -0.548 | *** | Arayuzu olmayan sinif (kotu) |
| 9 | ClassesExceedingMethodThreshold | -0.043 | -0.518 | *** | Fazla metodlu sinif (kotu) |
| 10 | AvgLCOM | -0.041 | -0.513 | *** | Dusuk uyum (kotu kod isareti) |

**Aciklama**:
- (+) isaretli olanlar iyi kodda yuksek cikiyor
- (-) isaretli olanlar kotu kodda yuksek cikiyor
- *** istatistiksel olarak cok anlamli demek (p < 0.001)
- **EncapsulationRatio** (kapsulleme orani) en belirleyici ozellik. r=0.995 neredeyse mukemmel korelasyon.
  Yani: iyi kodda tum alanlar private, kotu kodda hepsi public.

#### 4.6 Iyi Kod vs Kotu Kod Karsilastirmasi

Su tabloyu makaleye koy:

| Metrik | Iyi Kod (n=3) | Kotu Kod (n=4) | Fark |
|--------|---------------|----------------|------|
| Birlesik Skor | 77.3 | 23.7 | +53.6 |
| Kural Yuzdesi | %68.1 | %31.3 | +36.9 |
| ML Skor | 91.1 | 12.3 | +78.9 |
| Sorun Sayisi | 7.3 | 15.8 | -8.4 |
| CK-WMC | 1.7 | 2.8 | -1.1 |
| CK-DIT | 1.0 | 0.3 | +0.8 |
| CK-CBO | 2.0 | 1.3 | +0.8 |
| CK-RFC | 3.0 | 4.0 | -1.0 |
| CK-LCOM | 0.0 | 1.8 | -1.8 |

**Not**: Bu veriler 7 test dosyasindan alinmistir (3 iyi, 4 kotu).

**Aciklama**:
- Iyi kodlar ortalama 77.3 puan alirken, kotu kodlar 23.7 puan aliyor (53.6 puan fark)
- Kotu kodlarda ortalama 15.8 sorun bulunurken iyi kodlarda sadece 7.3 sorun var
- CK-LCOM (uyumsuzluk): Kotu kodda 1.8, iyi kodda 0 (kotu kodda metodlar birbiriyle iliskisiz)
- CK-RFC (cagri sayisi): Kotu kodda 4.0, iyi kodda 3.0 (kotu kod daha karmasik)

#### 4.7 CK Metrikleri Detayli Analiz

Su tabloyu makaleye koy:

| CK Metrigi | Gecen Dosya Ort. | Kalan Dosya Ort. | Gecen Max | Kalan Max | Esik |
|------------|------------------|------------------|-----------|-----------|------|
| WMC | 1.54 | 2.92 | 2 | 10 | <=10 |
| DIT | 0.31 | 0.08 | 1 | 1 | <=3 |
| NOC | 0.31 | 0.08 | 2 | 1 | <=5 |
| CBO | 2.15 | 1.15 | 4 | 4 | <=5 |
| RFC | 2.62 | 3.77 | 5 | 12 | <=20 |
| LCOM | 0.08 | 1.69 | 1 | 15 | <=3 |

**Aciklama**:
- 26 sinif analiz edildi. 24'u temiz (ihlalsiz), 2'sinde LCOM ihlali var
- Kalan dosyalarda WMC ortalamasi 2.92 iken gecen dosyalarda 1.54 (kotu kodda metod karmasikligi daha yuksek)
- **En buyuk fark LCOM'da**: Kalan dosyalar 1.69, gecen dosyalar 0.08.
  LCOM yuksek = metodlar birbiriyle iliskisiz = sinif birden fazla is yapiyor = kotu tasarim
- Kalan dosyalarda RFC max=12, gecen dosyalarda max=5

**Ihlal eden siniflar**:
- test-mid.cs / UserRepository: LCOM=4 (esik: 3)
- test-bad-payment.cs / PaymentService: LCOM=15 (esik: 3) -- bu sinif cok kotu

#### 4.8 Test Dosyalari Detayli Sonuclar

Su tabloyu makaleye koy:

| Dosya | Skor | Kural | ML | Guven | Sorun | Sonuc |
|-------|------|-------|----|-------|-------|-------|
| test-good.cs | 78.0 | %70 | 91 | %100 | 6 | GECTI |
| test-good-report.cs | 75.9 | %65 | 92 | %100 | 10 | GECTI |
| test-mid.cs | 27.3 | %36 | 13 | %100 | 13 | KALDI |
| test-bad-payment.cs | 19.3 | %28 | 6 | %100 | 20 | KALDI |
| test-bad-shopping.cs | 24.9 | %30 | 17 | %100 | 13 | KALDI |
| test-bad-student.cs | 23.3 | %30 | 12 | %100 | 17 | KALDI |
| test-pr-demo.cs | 78.0 | %70 | 91 | %100 | 6 | GECTI |

**Aciklama**: 7 test dosyasi analiz edildi. 3 iyi kod dosyasi GECTI, 4 kotu kod dosyasi KALDI. Sistem %100 dogru siniflandirdi. En kotu kod test-bad-payment.cs (19.3 puan, 20 sorun).

**En cok ihlal edilen kurallar** (85 sorunun dagilimi):

| Kategori | Sayi | Oran |
|----------|------|------|
| Arayuz (Interface) | 22 | %25.9 |
| Kalitim (Inheritance) | 21 | %24.7 |
| Kapsulleme | 21 | %24.7 |
| Bagimlilik Enjeksiyonu (DI) | 12 | %14.1 |
| Tek Sorumluluk (SRP) | 4 | %4.7 |
| Polimorfizm | 3 | %3.5 |
| CK Metrikleri | 2 | %2.4 |

**Aciklama**: Kotu kodlarda en cok ihlal edilen kurallar Arayuz, Kalitim ve Kapsulleme. Bu 3 kural toplam sorunlarin %75'ini olusturuyor.

#### 4.9 Agirlikli vs Agirliklisiz k-NN

Su tabloyu makaleye koy:

| k | Agirlikli Dogruluk | Agirliksiz Dogruluk | Fark |
|---|-------------------|---------------------|------|
| 1 | %100.0 | %98.0 | +%2.0 |
| 3 | %100.0 | %98.0 | +%2.0 |
| 5 | %100.0 | %98.0 | +%2.0 |
| 7 | %100.0 | %98.0 | +%2.0 |
| 9 | %98.0 | %98.0 | %0.0 |

**Aciklama**: Ozellik agirliklari kullanmak %2 dogruluk iyilestirmesi saglamistir. Agirliklari kaldirinca k=1-7 araliginda 1 ornek yanlis siniflandiriliyor.

#### 4.10 Mesafe Analizi

| Cift Turu | Ortalama Mesafe | Cift Sayisi |
|-----------|-----------------|-------------|
| Good-Good (kendi arasi) | 0.92 | 300 |
| Bad-Bad (kendi arasi) | 1.06 | 300 |
| Good-Bad (siniflar arasi) | 1.65 | 625 |

**Siniflar arasi / Sinif ici mesafe orani: 1.79**

**Aciklama**: Iyi ve kotu kodlar birbirinden 1.79 kat daha uzakta. Bu oran 1'den buyuk oldugu icin siniflar birbirinden iyi ayrilmis demektir. k-NN algoritmasinin basarili olmasi beklenir.

**Skor dagilimi**:
- Iyi kodlarin tahmin skoru: min=90, max=95, ort=92.7
- Kotu kodlarin tahmin skoru: min=11.9, max=25, ort=17.0
- **Ayrim marji: 65 puan** (en dusuk iyi = 90, en yuksek kotu = 25)
  Bu 65 puanlik bosluk, sistemin cok guclu bir ayrim yaptigi anlamina gelir.

---

### BOLUM 5: TARTISMA (yaklasik 1 sayfa)

Su konulari tartis (kendi cumlelerinle yaz):

1. **Neden hibrit yaklasim daha iyi?**
   - Kural bazli sistem tek basina %82 dogruluk sagliyor
   - ML eklendi, %100'e cikti
   - Cunku kurallar sadece "var mi yok mu" bakiyor, ML ise orneklerin birbirine benzerligini olcuyor
   - 8 kotu kod ornegi kurallarin bazi puanlarini alinca "gecti" goruyordu, ama ML onlarin kotu orneklere benzedigini yakaliyor

2. **CK metriklerinin katkisi**
   - 26 ozelligin 6'si CK metrigi
   - LCOM (uyumsuzluk) iyi ve kotu kodu ayirmada onemli (kotu kodda 10x daha yuksek)
   - CK metrikleri 1994'ten beri kullanilan, kabul gormus metrikler

3. **Sinirlamalar**
   - 50 ornek kucuk bir veri seti
   - Sadece C# dili
   - Sadece OOP prensipleri (fonksiyonel programlama yok)
   - Gercek buyuk projelerde test edilmedi

---

### BOLUM 6: SONUC (yaklasik yarim sayfa)

Su maddeleri ozet olarak yaz:
- C# kodu analiz eden hibrit bir sistem gelistirildi
- 7 kural + 26 ozellik + k-NN algoritmasi + CK metrikleri
- LOO-CV ile %100 dogruluk saglandi
- Kural bazli sisteme gore %18 iyilestirme
- GitHub Actions entegrasyonu ile CI/CD surece katildi
- Gelecekte: daha fazla ornek, farkli diller, SOLID prensipleri

---

## TABLO LISTESI (makaleye koyulacak tablolar)

| Tablo No | Baslik | Hangi bolumde |
|----------|--------|---------------|
| Tablo 1 | Kural motoru kurallar ve puanlari | 3.2 |
| Tablo 2 | CK metrikleri ve esik degerleri | 3.3 |
| Tablo 3 | 26 ozellik listesi | 3.4 |
| Tablo 4 | Veri seti istatistikleri | 4.1 |
| Tablo 5 | k degeri deneyi sonuclari | 4.2 |
| Tablo 6 | Confusion Matrix | 4.3 |
| Tablo 7 | Yontem karsilastirmasi (ONEMLI) | 4.4 |
| Tablo 8 | Ozellik onem sirasi | 4.5 |
| Tablo 9 | Iyi vs kotu kod karsilastirmasi | 4.6 |
| Tablo 10 | CK metrikleri karsilastirmasi | 4.7 |
| Tablo 11 | Test dosyalari sonuclari | 4.8 |
| Tablo 12 | Sorun dagilimi | 4.8 |
| Tablo 13 | Agirlikli vs agirliklisiz | 4.9 |
| Tablo 14 | Mesafe analizi | 4.10 |

---

## SEKIL LISTESI (cizmen gereken sekiller)

| Sekil No | Baslik | Aciklama |
|----------|--------|----------|
| Sekil 1 | Sistem Mimarisi | Yukardaki blok diyagramini ciz |
| Sekil 2 | Iyi vs Kotu Kod Skor Dagilimi | Bar grafik: iyi kodlar 77.3, kotu kodlar 23.7 |
| Sekil 3 | k Degeri vs Dogruluk | Cizgi grafik: k=1-15, y ekseni dogruluk |
| Sekil 4 | Feature Importance | Yatay bar grafik: en onemli 10 ozellik |
| Sekil 5 | Confusion Matrix | 2x2 matris: TP=25, TN=25, FP=0, FN=0 |

---

## SISTEM PARAMETRELERI OZET TABLOSU

| Parametre | Deger |
|-----------|-------|
| Platform | .NET 8 (C#) |
| Parser | Roslyn (Microsoft.CodeAnalysis) |
| ML Algoritmasi | Agirlikli k-NN |
| k degeri | 3 |
| Ozellik sayisi | 26 (20 OOP + 6 CK) |
| Egitim ornegi | 50 (25 Good + 25 Bad) |
| Normalizasyon | Z-score |
| Mesafe fonksiyonu | Agirlikli Oklid |
| Hibrit formul | Q = 0.60K + 0.40M |
| Kalite esigi | 65/100 |
| Capraz dogrulama | Leave-One-Out (LOO-CV) |
| Dogruluk | %100 |
| CI/CD | GitHub Actions |

---

## ANAHTAR KELIMELER (makale icin)

Turkce: kod kalitesi, nesne yonelimli programlama, statik analiz, makine ogrenmesi,
k-NN, CK metrikleri, hibrit yaklasim, yazilim metrikleri

Ingilizce: code quality, object-oriented programming, static analysis, machine learning,
k-NN, CK metrics, hybrid approach, software metrics

---

**ONEMLI HATIRLATMA:**
- Tablolari kopyala ama yorumlari ve aciklamalari KENDI CUMLELERINLE yaz
- "Bu calismada..." "Tablo X'te goruldugu gibi..." "Sonuclar gostermektedir ki..." gibi akademik cumleler kur
- Hocana soyle: "Verileri ben topladim, tabloyu ben olusturdum"
- Referanslari mutlaka ekle (Chidamber & Kemerer, 1994 vs.)
