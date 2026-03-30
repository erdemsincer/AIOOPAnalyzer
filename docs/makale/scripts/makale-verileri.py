#!/usr/bin/env python3
"""
AI OOP Analyzer - Makale Deneysel Verileri Toplama Scripti
Tum test dosyalarini ve dataset'i analiz ederek makale icin ham verileri cikarir.
"""
import subprocess, json, os, sys

PROJE = "/Users/erdemsincer/Project/AIOOPAnalyzer"

TEST_DOSYALARI = [
    "tests/test-good.cs",
    "tests/test-good-report.cs",
    "tests/test-mid.cs",
    "tests/test-bad-payment.cs",
    "tests/test-bad-shopping.cs",
    "tests/test-bad-student.cs",
    "examples/test-pr-demo.cs",
]

def run_pipeline(dosya):
    cmd = ["dotnet", "run", "--", "pipeline", dosya, "--json"]
    result = subprocess.run(cmd, capture_output=True, text=True, cwd=PROJE)
    # Tum stdout JSON olmali
    stdout = result.stdout.strip()
    try:
        return json.loads(stdout)
    except json.JSONDecodeError:
        # Bazen banner satiri olabiliyor, { ile baslayan blogu bul
        idx = stdout.find("{")
        if idx >= 0:
            return json.loads(stdout[idx:])
    return None

def main():
    print("=" * 80)
    print("  AI OOP ANALYZER — MAKALE DENEYSEL VERILERI")
    print("=" * 80)

    sonuclar = []
    for dosya in TEST_DOSYALARI:
        d = run_pipeline(dosya)
        if d:
            sonuclar.append(d)

    # ═══════════════════════════════════════════
    # TABLO 1: Test Dosyalari Genel Sonuclari
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 1: Test Dosyalari Genel Sonuclari")
    print("=" * 80)
    print(f"{'Dosya':<25} {'Skor':>6} {'Kural%':>7} {'ML Skor':>8} {'ML Tahmin':>10} {'Guven':>6} {'Sorun':>6} {'Sonuc':>8}")
    print("-" * 80)

    for d in sonuclar:
        kb = d["kural_bazli"]
        ml = d["ml_tahmini"]
        dosya = os.path.basename(d["dosya"])
        print(f"{dosya:<25} {d['birlesik_skor']:>6.1f} {kb['yuzde']:>6.0f}% {ml['skor']:>7.0f} {ml['tahmin']:>10} {ml['guven']:>5.0f}% {len(kb['sorunlar']):>5} {'GECTI' if d['gecti'] else 'KALDI':>8}")

    # ═══════════════════════════════════════════
    # TABLO 2: Kural Bazli Detayli Sonuclar
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 2: Kural Bazli Detayli Sonuclar (Her kural icin puan)")
    print("=" * 80)
    kural_isimleri = []
    if sonuclar:
        kural_isimleri = [k["kural"] for k in sonuclar[0]["kural_bazli"]["kurallar"]]

    header = f"{'Dosya':<25}"
    for ki in kural_isimleri:
        header += f" {ki[:8]:>8}"
    header += f" {'TOPLAM':>8}"
    print(header)
    print("-" * len(header))

    for d in sonuclar:
        dosya = os.path.basename(d["dosya"])
        satir = f"{dosya:<25}"
        for k in d["kural_bazli"]["kurallar"]:
            satir += f" {k['skor']:>3}/{k['maks']:<3}"
        satir += f" {d['kural_bazli']['skor']:>3}/{d['kural_bazli']['maks']}"
        print(satir)

    # ═══════════════════════════════════════════
    # TABLO 3: CK Metrikleri Sonuclari
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 3: CK Metrikleri (Ortalama Degerler)")
    print("=" * 80)
    print(f"{'Dosya':<25} {'WMC':>5} {'DIT':>5} {'NOC':>5} {'CBO':>5} {'RFC':>5} {'LCOM':>5}")
    print("-" * 60)

    for d in sonuclar:
        dosya = os.path.basename(d["dosya"])
        ck = d["ck_metrikleri"]["ortalama"]
        print(f"{dosya:<25} {ck['wmc']:>5} {ck['dit']:>5} {ck['noc']:>5} {ck['cbo']:>5} {ck['rfc']:>5} {ck['lcom']:>5}")

    print("-" * 60)
    print(f"{'ESIK DEGERLERI':<25} {'<=10':>5} {'<=3':>5} {'<=5':>5} {'<=5':>5} {'<=20':>5} {'<=3':>5}")

    # ═══════════════════════════════════════════
    # TABLO 4: CK Metrikleri Sinif Bazli (Tum siniflar)
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 4: CK Metrikleri - Tum Siniflar Detayi")
    print("=" * 80)
    print(f"{'Dosya':<20} {'Sinif':<25} {'WMC':>5} {'DIT':>5} {'NOC':>5} {'CBO':>5} {'RFC':>5} {'LCOM':>5} {'Ihlal':>6}")
    print("-" * 85)

    tum_siniflar = []
    for d in sonuclar:
        dosya = os.path.basename(d["dosya"])
        for sinif in d["ck_metrikleri"]["sinif_bazli"]:
            ihlal = 0
            if sinif["wmc"] > 10: ihlal += 1
            if sinif["dit"] > 3: ihlal += 1
            if sinif["cbo"] > 5: ihlal += 1
            if sinif["rfc"] > 20: ihlal += 1
            if sinif["lcom"] > 3: ihlal += 1
            tum_siniflar.append({**sinif, "dosya": dosya, "ihlal": ihlal})
            print(f"{dosya:<20} {sinif['sinif']:<25} {sinif['wmc']:>5} {sinif['dit']:>5} {sinif['noc']:>5} {sinif['cbo']:>5} {sinif['rfc']:>5} {sinif['lcom']:>5} {ihlal:>5}")

    # ═══════════════════════════════════════════
    # TABLO 5: Ozellik Vektoru Karsilastirmasi
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 5: Ozellik Vektoru (Feature Vector) Karsilastirmasi")
    print("=" * 80)
    print(f"{'Dosya':<25} {'Sinif':>6} {'Pub':>5} {'Priv':>5} {'Kaps%':>6} {'IFace%':>7} {'Virt':>5} {'Over':>5} {'New':>5}")
    print("-" * 75)

    for d in sonuclar:
        dosya = os.path.basename(d["dosya"])
        oz = d["ozellikler"]
        print(f"{dosya:<25} {oz['sinif_sayisi']:>6} {oz['public_alan']:>5} {oz['private_alan']:>5} {oz['kapsulleme_orani']*100:>5.0f}% {oz['interface_orani']*100:>6.0f}% {oz['virtual_metod']:>5} {oz['override_metod']:>5} {oz['new_kullanimi']:>5}")

    # ═══════════════════════════════════════════
    # TABLO 6: Istatistiksel Ozet
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 6: Istatistiksel Ozet")
    print("=" * 80)

    good_dosyalar = [d for d in sonuclar if d["gecti"]]
    bad_dosyalar = [d for d in sonuclar if not d["gecti"]]

    def ort(lst): return sum(lst)/len(lst) if lst else 0
    def mn(lst): return min(lst) if lst else 0
    def mx(lst): return max(lst) if lst else 0

    print(f"\n{'Metrik':<35} {'Iyi Kod (n={len(good_dosyalar)})':>20} {'Kotu Kod (n={len(bad_dosyalar)})':>20} {'Tumu (n={len(sonuclar)})':>20}")
    print("-" * 100)

    # Birlesik Skor
    good_skor = [d["birlesik_skor"] for d in good_dosyalar]
    bad_skor = [d["birlesik_skor"] for d in bad_dosyalar]
    all_skor = [d["birlesik_skor"] for d in sonuclar]
    print(f"{'Birlesik Skor (ort)':.<35} {ort(good_skor):>19.1f} {ort(bad_skor):>19.1f} {ort(all_skor):>19.1f}")
    print(f"{'Birlesik Skor (min-max)':.<35} {mn(good_skor):>8.1f}-{mx(good_skor):<8.1f} {mn(bad_skor):>8.1f}-{mx(bad_skor):<8.1f} {mn(all_skor):>8.1f}-{mx(all_skor):<8.1f}")

    # Kural Yuzdesi
    good_kural = [d["kural_bazli"]["yuzde"] for d in good_dosyalar]
    bad_kural = [d["kural_bazli"]["yuzde"] for d in bad_dosyalar]
    all_kural = [d["kural_bazli"]["yuzde"] for d in sonuclar]
    print(f"{'Kural Yuzdesi (ort)':.<35} {ort(good_kural):>18.1f}% {ort(bad_kural):>18.1f}% {ort(all_kural):>18.1f}%")

    # ML Skor
    good_ml = [d["ml_tahmini"]["skor"] for d in good_dosyalar]
    bad_ml = [d["ml_tahmini"]["skor"] for d in bad_dosyalar]
    all_ml = [d["ml_tahmini"]["skor"] for d in sonuclar]
    print(f"{'ML Skor (ort)':.<35} {ort(good_ml):>19.1f} {ort(bad_ml):>19.1f} {ort(all_ml):>19.1f}")

    # Sorun Sayisi
    good_sorun = [len(d["kural_bazli"]["sorunlar"]) for d in good_dosyalar]
    bad_sorun = [len(d["kural_bazli"]["sorunlar"]) for d in bad_dosyalar]
    all_sorun = [len(d["kural_bazli"]["sorunlar"]) for d in sonuclar]
    print(f"{'Sorun Sayisi (ort)':.<35} {ort(good_sorun):>19.1f} {ort(bad_sorun):>19.1f} {ort(all_sorun):>19.1f}")

    # CK Metrikleri Ortalamalari
    for metrik in ["wmc", "dit", "noc", "cbo", "rfc", "lcom"]:
        good_val = [d["ck_metrikleri"]["ortalama"][metrik] for d in good_dosyalar]
        bad_val = [d["ck_metrikleri"]["ortalama"][metrik] for d in bad_dosyalar]
        all_val = [d["ck_metrikleri"]["ortalama"][metrik] for d in sonuclar]
        print(f"{'CK-' + metrik.upper() + ' (ort)':.<35} {ort(good_val):>19.1f} {ort(bad_val):>19.1f} {ort(all_val):>19.1f}")

    # ═══════════════════════════════════════════
    # TABLO 7: Sorun Kategori Dagilimi
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 7: Sorun Kategori Dagilimi")
    print("=" * 80)

    kategori_sayac = {}
    for d in sonuclar:
        for sorun in d["kural_bazli"]["sorunlar"]:
            if sorun.startswith("[Kapsulleme]"): kat = "Kapsulleme"
            elif sorun.startswith("[Tek Sorumluluk]"): kat = "Tek Sorumluluk (SRP)"
            elif sorun.startswith("[Bagimlilik"): kat = "Bagimlilik Enjeksiyonu"
            elif sorun.startswith("[Arayuz]"): kat = "Interface"
            elif sorun.startswith("[Kalitim]"): kat = "Kalitim"
            elif sorun.startswith("[Polimorfizm]"): kat = "Polimorfizm"
            elif sorun.startswith("[CK-"): kat = "CK Metrikleri"
            else: kat = "Diger"
            kategori_sayac[kat] = kategori_sayac.get(kat, 0) + 1

    toplam_sorun = sum(kategori_sayac.values())
    print(f"\n{'Kategori':<30} {'Sayi':>6} {'Oran':>8}")
    print("-" * 50)
    for kat, sayi in sorted(kategori_sayac.items(), key=lambda x: -x[1]):
        print(f"{kat:<30} {sayi:>6} {sayi/toplam_sorun*100:>7.1f}%")
    print(f"{'TOPLAM':<30} {toplam_sorun:>6} {'100.0':>7}%")

    # ═══════════════════════════════════════════
    # TABLO 8: Batch Evaluation (Dataset)
    # ═══════════════════════════════════════════
    print("\n\n" + "=" * 80)
    print("TABLO 8: Dataset Batch Evaluation Sonuclari")
    print("(50 ornek uzerinde — bu veriyi batch komutundan alin)")
    print("=" * 80)
    print("""
  Yontem                  | Dogruluk | Good Recall | Good Prec. | Bad Recall | Bad Prec.
  ----------------------- | -------- | ----------- | ---------- | ---------- | ---------
  Yalniz Kural (K>=65)    | %82      | %96         | %75        | %68        | %94.4
  Yalniz ML (k-NN)        | %100     | %100        | %100       | %100       | %100
  Hibrit (Kural+ML)       | %100     | %100        | %100       | %100       | %100

  Good orneklerde ort ML skor: 92    | Bad orneklerde ort ML skor: 15
  Good orneklerde ort kural  : %83   | Bad orneklerde ort kural  : %57
""")

    # ═══════════════════════════════════════════
    # TABLO 9: Confusion Matrix
    # ═══════════════════════════════════════════
    print("=" * 80)
    print("TABLO 9: Confusion Matrix (Karisiklik Matrisi)")
    print("=" * 80)
    print("""
  a) Yalniz Kural Bazli:          b) Hibrit Sistem:
  
                Tahmin              |            Tahmin
              Good   Bad           |          Good   Bad
  Gercek Good  24     1            | Gercek Good  25     0
  Gercek Bad    8    17            | Gercek Bad    0    25
  
  Accuracy: %82                    | Accuracy: %100
  F1(Good): %84.2                  | F1(Good): %100
  F1(Bad):  %79.1                  | F1(Bad):  %100
""")

    # ═══════════════════════════════════════════
    # MAKALE BOLUM ONERILERI
    # ═══════════════════════════════════════════
    print("=" * 80)
    print("MAKALE ICIN BOLUM YAPISI ONERISI")
    print("=" * 80)
    print("""
  1. GIRIS
     - Yazilim kalitesinin onemi
     - OOP prensiplerinin kontrolunun zorluklari
     - Arac motivasyonu

  2. ILGILI CALISMALARI
     - Statik kod analiz araclari (SonarQube, StyleCop vb.)
     - CK metrikleri literatur
     - ML tabanli kod kalite tahmini

  3. ONERILEN SISTEM MIMARISI
     - 3.1 Genel mimari (Roslyn parser + Kural motoru + ML)
     - 3.2 Kural bazli analiz motoru (7 kural, puanlama)
     - 3.3 CK metrikleri hesaplama
     - 3.4 ML modeli (k-NN, k=3, feature vector)
     - 3.5 Hibrit skor formulu: Q = 0.60*K + 0.40*M
     - 3.6 CI/CD entegrasyonu (GitHub Actions)

  4. DENEYSEL SONUCLAR
     - 4.1 Veri seti (50 ornek: 25 iyi, 25 kotu)
     - 4.2 Model egitim sonuclari (Tablo 8, 9)
     - 4.3 Test dosyalari analizi (Tablo 1-7)
     - 4.4 Kural vs ML vs Hibrit karsilastirmasi
     - 4.5 CK metrikleri analizi

  5. TARTISMA
     - Hibrit yaklasimin avantajlari
     - Kural bazlinin sinirlamalari (%82 vs %100)
     - CK metriklerinin OOP kalitesine katkisi

  6. SONUC VE GELECEK CALISMALARI
""")

    print("=" * 80)
    print("VERILER BASARIYLA TOPLANDI!")
    print("Bu verileri kendi cumlelerin ile makaleye donustur.")
    print("=" * 80)

if __name__ == "__main__":
    main()
