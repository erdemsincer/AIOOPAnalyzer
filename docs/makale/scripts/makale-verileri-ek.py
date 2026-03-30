#!/usr/bin/env python3
"""
AI OOP Analyzer - Ek Deneysel Veriler
Per-sample LOO-CV detaylari ve korelasyon analizi
"""
import json, math, os
from collections import defaultdict

PROJE = "/Users/erdemsincer/Project/AIOOPAnalyzer"

def load_model():
    with open(os.path.join(PROJE, "Models/model.json")) as f:
        return json.load(f)

def euclidean_dist(a, b, weights):
    s = 0
    for i in range(min(len(a), len(b))):
        w = abs(weights[i]) if i < len(weights) else 1.0
        s += w * (a[i] - b[i])**2
    return math.sqrt(s)

def knn_predict(samples, test_features, weights, k=3):
    dists = []
    for s in samples:
        d = euclidean_dist(test_features, s["Features"], weights)
        dists.append((d, s))
    dists.sort(key=lambda x: x[0])
    top_k = dists[:k]
    total_w = 0
    label_sum = 0
    score_sum = 0
    neighbors = []
    for dist, smp in top_k:
        w = 1.0 / (dist + 0.0001)
        total_w += w
        label_sum += w * smp["Label"]
        score_sum += w * smp["QualityScore"]
        neighbors.append(smp["Id"])
    prob = label_sum / total_w
    pred_label = 1 if prob >= 0.5 else 0
    pred_score = score_sum / total_w
    confidence = prob if pred_label == 1 else 1 - prob
    return pred_label, pred_score, confidence, neighbors

output = []
def p(text=""):
    print(text)
    output.append(text)

def avg(lst):
    return sum(lst)/len(lst) if lst else 0

def pearson_corr(x, y):
    n = len(x)
    if n < 2: return 0
    mx, my = avg(x), avg(y)
    num = sum((xi-mx)*(yi-my) for xi,yi in zip(x,y))
    den_x = math.sqrt(sum((xi-mx)**2 for xi in x))
    den_y = math.sqrt(sum((yi-my)**2 for yi in y))
    if den_x * den_y == 0: return 0
    return num / (den_x * den_y)

def main():
    model = load_model()
    samples = model["Samples"]
    weights = model["FeatureWeights"]
    feature_names = model["FeatureNames"]

    p("=" * 120)
    p("  AI OOP ANALYZER - EK DENEYSEL VERILER")
    p("=" * 120)

    # ═══════════════════════════════════════════════════════════════════
    # EK-1: PER-SAMPLE LOO-CV DETAYI
    # ═══════════════════════════════════════════════════════════════════
    p("\n" + "=" * 120)
    p("  EK-1: PER-SAMPLE LOO-CV DETAYI (k=3)")
    p("=" * 120)
    p(f"\n  Her ornegin LOO-CV'de tahmin sonucu, skoru ve en yakin 3 komsulari")
    p(f"")
    p(f"  {'No':>3} {'ID':<12} {'Gercek':>6} {'Tahmin':>6} {'Hedef Skor':>11} {'Tahmin Skor':>12} {'Hata':>7} {'Guven':>7} {'Komsu-1':<12} {'Komsu-2':<12} {'Komsu-3':<12}")
    p(f"  {'-'*3} {'-'*12} {'-'*6} {'-'*6} {'-'*11} {'-'*12} {'-'*7} {'-'*7} {'-'*12} {'-'*12} {'-'*12}")

    all_errors = []
    for i, s in enumerate(samples):
        train = [x for j, x in enumerate(samples) if j != i]
        pred_label, pred_score, conf, neighbors = knn_predict(train, s["Features"], weights, 3)
        actual_label_str = "Good" if s["Label"] == 1 else "Bad"
        pred_label_str = "Good" if pred_label == 1 else "Bad"
        error = abs(pred_score - s["QualityScore"])
        correct = "OK" if pred_label == s["Label"] else "XX"
        all_errors.append(error)
        n1 = neighbors[0] if len(neighbors) > 0 else "-"
        n2 = neighbors[1] if len(neighbors) > 1 else "-"
        n3 = neighbors[2] if len(neighbors) > 2 else "-"
        p(f"  {i+1:>3} {s['Id']:<12} {actual_label_str:>6} {pred_label_str:>6} {s['QualityScore']:>11.1f} {pred_score:>12.2f} {error:>7.2f} {conf*100:>6.1f}% {n1:<12} {n2:<12} {n3:<12}")

    p(f"\n  Toplam: {sum(1 for e in all_errors if e < 5)} ornek < 5 puan hata")
    p(f"  Ort. tahmin hatasi: {avg(all_errors):.2f} puan")
    p(f"  Maks. tahmin hatasi: {max(all_errors):.2f} puan")
    p(f"  Min. tahmin hatasi: {min(all_errors):.2f} puan")
    p(f"  Tahmin hatasi std sapma: {math.sqrt(sum((e-avg(all_errors))**2 for e in all_errors)/len(all_errors)):.2f}")

    # ═══════════════════════════════════════════════════════════════════
    # EK-2: OZELLIK KORELASYONLARI
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-2: OZELLIK-KALITE SKOR KORELASYONU (Pearson)")
    p("=" * 120)
    p(f"\n  Her ozellikteki degerin hedef kalite skoru ile korelasyonu")
    p(f"  r > 0.5 : Guclu pozitif iliskili (iyi koda isaret eder)")
    p(f"  r < -0.5: Guclu negatif iliskili (kotu koda isaret eder)")
    p(f"")
    p(f"  {'Ozellik':<35} {'r (Pearson)':>12} {'|r|':>6} {'Guc':>10} {'Yorum':<20}")
    p(f"  {'-'*35} {'-'*12} {'-'*6} {'-'*10} {'-'*20}")

    quality_scores = [s["QualityScore"] for s in samples]

    feature_corrs = []
    for fi in range(len(feature_names)):
        feature_vals = [s["Features"][fi] for s in samples]
        r = pearson_corr(feature_vals, quality_scores)
        feature_corrs.append((feature_names[fi], r))

    feature_corrs.sort(key=lambda x: abs(x[1]), reverse=True)
    for name, r in feature_corrs:
        abs_r = abs(r)
        if abs_r >= 0.7: guc = "Cok guclu"
        elif abs_r >= 0.5: guc = "Guclu"
        elif abs_r >= 0.3: guc = "Orta"
        elif abs_r >= 0.1: guc = "Zayif"
        else: guc = "Cok zayif"
        yorum = "Iyi kod+" if r > 0 else "Kotu kod+"
        bar = "#" * int(abs_r * 20)
        p(f"  {name:<35} {r:>+12.4f} {abs_r:>6.4f} {guc:<10} {yorum} {bar}")

    # ═══════════════════════════════════════════════════════════════════
    # EK-3: OZELLIKLER ARASI KORELASYON MATRISI (onemli olanlar)
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-3: OZELLIKLER ARASI KORELASYON (en yuksek 20 cift)")
    p("=" * 120)
    p(f"\n  Ozellikler arasindaki Pearson korelasyonu (coklu dogrusal bagimliligi gosterir)")
    p(f"")

    pairs = []
    for i in range(len(feature_names)):
        for j in range(i+1, len(feature_names)):
            vals_i = [s["Features"][i] for s in samples]
            vals_j = [s["Features"][j] for s in samples]
            r = pearson_corr(vals_i, vals_j)
            pairs.append((feature_names[i], feature_names[j], r))

    pairs.sort(key=lambda x: abs(x[2]), reverse=True)
    p(f"  {'Ozellik-1':<30} {'Ozellik-2':<30} {'r':>8}")
    p(f"  {'-'*30} {'-'*30} {'-'*8}")
    for f1, f2, r in pairs[:25]:
        p(f"  {f1:<30} {f2:<30} {r:>+8.4f}")

    # ═══════════════════════════════════════════════════════════════════
    # EK-4: SINIF BAZLI DERIN ANALIZ
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-4: GOOD vs BAD FEATURE DISTRIBUTION (Histogram)")
    p("=" * 120)
    p(f"\n  Her ozellik icin Good ve Bad orneklerin dagilimi")

    good_samples = [s for s in samples if s["Label"] == 1]
    bad_samples = [s for s in samples if s["Label"] == 0]

    for fi in range(len(feature_names)):
        name = feature_names[fi]
        mean = model["FeatureMeans"][fi]
        std = model["FeatureStdDevs"][fi]

        g_vals = [s["Features"][fi] * std + mean for s in good_samples]
        b_vals = [s["Features"][fi] * std + mean for s in bad_samples]
        g_avg = avg(g_vals)
        b_avg = avg(b_vals)
        g_min = min(g_vals)
        g_max = max(g_vals)
        b_min = min(b_vals)
        b_max = max(b_vals)

        # t-test (basit)
        g_std = math.sqrt(sum((x-g_avg)**2 for x in g_vals)/(len(g_vals)-1)) if len(g_vals) > 1 else 0
        b_std = math.sqrt(sum((x-b_avg)**2 for x in b_vals)/(len(b_vals)-1)) if len(b_vals) > 1 else 0
        se = math.sqrt(g_std**2/len(g_vals) + b_std**2/len(b_vals)) if (g_std + b_std) > 0 else 0.001
        t_val = abs(g_avg - b_avg) / se
        # Basit significance estimate (df~48)
        sig = "***" if t_val > 3.5 else "**" if t_val > 2.5 else "*" if t_val > 1.5 else "ns"

        p(f"\n  {name}:")
        p(f"    Good: ort={g_avg:.3f}, min={g_min:.1f}, max={g_max:.1f}, std={g_std:.3f}")
        p(f"    Bad : ort={b_avg:.3f}, min={b_min:.1f}, max={b_max:.1f}, std={b_std:.3f}")
        p(f"    t-test: t={t_val:.3f}, anlamlilik={sig}")

    # ═══════════════════════════════════════════════════════════════════
    # EK-5: AGIRLIKSIZ vs AGIRLIKLI k-NN KARSILASTIRMASI
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-5: AGIRLIKSIZ vs AGIRLIKLI k-NN KARSILASTIRMASI")
    p("=" * 120)

    p(f"\n  Agirliksiz: tum ozellikler esit agirlik (1.0)")
    p(f"  Agirlikli : feature importance agirlikli")
    p(f"")
    p(f"  {'k':>3} {'Agirlikli Acc':>15} {'Agirlikli Hata':>15} {'Agirlksz Acc':>15} {'Agirlksz Hata':>15} {'Fark':>8}")
    p(f"  {'-'*3} {'-'*15} {'-'*15} {'-'*15} {'-'*15} {'-'*8}")

    uniform_weights = [1.0] * len(feature_names)

    for k in [1, 3, 5, 7, 9]:
        # Agirlikli
        w_correct = 0
        w_error_sum = 0
        # Agirliksiz
        u_correct = 0
        u_error_sum = 0

        for i, s in enumerate(samples):
            train = [x for j, x in enumerate(samples) if j != i]

            # Weighted
            pl_w, ps_w, _, _ = knn_predict(train, s["Features"], weights, k)
            if pl_w == s["Label"]: w_correct += 1
            w_error_sum += abs(ps_w - s["QualityScore"])

            # Uniform
            dists = []
            for t in train:
                d = euclidean_dist(s["Features"], t["Features"], uniform_weights)
                dists.append((d, t))
            dists.sort(key=lambda x: x[0])
            top = dists[:k]
            tw = sum(1/(d+0.0001) for d,_ in top)
            pl_u = 1 if sum((1/(d+0.0001))*t["Label"]/tw for d,t in top) >= 0.5 else 0
            ps_u = sum((1/(d+0.0001))*t["QualityScore"]/tw for d,t in top)
            if pl_u == s["Label"]: u_correct += 1
            u_error_sum += abs(ps_u - s["QualityScore"])

        n = len(samples)
        w_acc = w_correct / n
        u_acc = u_correct / n
        w_err = w_error_sum / n
        u_err = u_error_sum / n
        fark = (w_acc - u_acc) * 100
        p(f"  {k:>3} {w_acc*100:>14.1f}% {w_err:>15.2f} {u_acc*100:>14.1f}% {u_err:>15.2f} {fark:>+7.1f}%")

    # ═══════════════════════════════════════════════════════════════════
    # EK-6: MESAFE ANALIZI
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-6: SINIF-ICI ve SINIFLAR-ARASI MESAFE ANALIZI")
    p("=" * 120)

    # Good-Good, Bad-Bad, Good-Bad mesafe ortalamasi
    gg_dists, bb_dists, gb_dists = [], [], []
    for i in range(len(samples)):
        for j in range(i+1, len(samples)):
            d = euclidean_dist(samples[i]["Features"], samples[j]["Features"], weights)
            if samples[i]["Label"] == 1 and samples[j]["Label"] == 1:
                gg_dists.append(d)
            elif samples[i]["Label"] == 0 and samples[j]["Label"] == 0:
                bb_dists.append(d)
            else:
                gb_dists.append(d)

    p(f"\n  Oklid mesafesi istatistikleri (agirlikli):")
    p(f"  {'Cift Turu':<25} {'Ort Mesafe':>12} {'Min':>8} {'Max':>8} {'Std':>8} {'Cift Sayisi':>12}")
    p(f"  {'-'*25} {'-'*12} {'-'*8} {'-'*8} {'-'*8} {'-'*12}")

    def stats(lst):
        m = avg(lst)
        mn = min(lst)
        mx = max(lst)
        sd = math.sqrt(sum((x-m)**2 for x in lst)/len(lst)) if lst else 0
        return m, mn, mx, sd

    for name, lst in [("Good-Good", gg_dists), ("Bad-Bad", bb_dists), ("Good-Bad", gb_dists)]:
        m, mn, mx, sd = stats(lst)
        p(f"  {name:<25} {m:>12.4f} {mn:>8.4f} {mx:>8.4f} {sd:>8.4f} {len(lst):>12}")

    ratio = avg(gb_dists) / avg(gg_dists) if avg(gg_dists) > 0 else 0
    p(f"\n  Siniflar-arasi / Sinif-ici mesafe orani: {ratio:.2f}")
    p(f"  (1'den buyuk = siniflar iyi ayrilmis)")

    # ═══════════════════════════════════════════════════════════════════
    # EK-7: SKOR DAGILIM ISTATISTIKLERI
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-7: TAHMIN SKOR DAGILIMI (LOO-CV, k=3)")
    p("=" * 120)

    good_preds = []
    bad_preds = []
    for i, s in enumerate(samples):
        train = [x for j,x in enumerate(samples) if j != i]
        _, ps, _, _ = knn_predict(train, s["Features"], weights, 3)
        if s["Label"] == 1:
            good_preds.append(ps)
        else:
            bad_preds.append(ps)

    p(f"\n  Good orneklerin tahmin skorlari:")
    p(f"    Ortalama: {avg(good_preds):.2f}")
    p(f"    Min: {min(good_preds):.2f}")
    p(f"    Max: {max(good_preds):.2f}")
    p(f"    Std: {math.sqrt(sum((x-avg(good_preds))**2 for x in good_preds)/len(good_preds)):.2f}")
    p(f"    Degerler: {', '.join(f'{v:.1f}' for v in sorted(good_preds))}")

    p(f"\n  Bad orneklerin tahmin skorlari:")
    p(f"    Ortalama: {avg(bad_preds):.2f}")
    p(f"    Min: {min(bad_preds):.2f}")
    p(f"    Max: {max(bad_preds):.2f}")
    p(f"    Std: {math.sqrt(sum((x-avg(bad_preds))**2 for x in bad_preds)/len(bad_preds)):.2f}")
    p(f"    Degerler: {', '.join(f'{v:.1f}' for v in sorted(bad_preds))}")

    p(f"\n  Siniflar arasi skor farki: {avg(good_preds) - avg(bad_preds):.2f} puan")
    p(f"  En dusuk Good tahmin: {min(good_preds):.2f}")
    p(f"  En yuksek Bad tahmin: {max(bad_preds):.2f}")
    p(f"  Ayrim marji: {min(good_preds) - max(bad_preds):.2f} puan")

    # ═══════════════════════════════════════════════════════════════════
    # EK-8: FEATURE IMPORTANCE RANKING KARSILASTIRMASI
    # ═══════════════════════════════════════════════════════════════════
    p("\n\n" + "=" * 120)
    p("  EK-8: FEATURE IMPORTANCE YONTEM KARSILASTIRMASI")
    p("=" * 120)
    p(f"\n  Yontem-1: Feature Weight (Good-Bad ortalama farki)")
    p(f"  Yontem-2: Pearson Korelasyonu (feature-score)")
    p(f"  Yontem-3: t-test istatistigi (Good vs Bad)")
    p(f"")
    p(f"  {'Ozellik':<35} {'Agirlik Sira':>13} {'Korelasyon Sira':>16} {'t-test Sira':>12}")
    p(f"  {'-'*35} {'-'*13} {'-'*16} {'-'*12}")

    # Yontem 1: Weight ranking
    weight_rank = sorted(range(len(feature_names)), key=lambda i: abs(weights[i]), reverse=True)
    weight_rank_map = {idx: rank+1 for rank, idx in enumerate(weight_rank)}

    # Yontem 2: Correlation ranking
    corr_vals = []
    for fi in range(len(feature_names)):
        vals = [s["Features"][fi] for s in samples]
        scores = [s["QualityScore"] for s in samples]
        r = pearson_corr(vals, scores)
        corr_vals.append(abs(r))
    corr_rank = sorted(range(len(feature_names)), key=lambda i: corr_vals[i], reverse=True)
    corr_rank_map = {idx: rank+1 for rank, idx in enumerate(corr_rank)}

    # Yontem 3: t-test ranking
    t_vals = []
    for fi in range(len(feature_names)):
        g_vals = [s["Features"][fi] for s in good_samples]
        b_vals = [s["Features"][fi] for s in bad_samples]
        g_avg_v = avg(g_vals)
        b_avg_v = avg(b_vals)
        g_std_v = math.sqrt(sum((x-g_avg_v)**2 for x in g_vals)/(len(g_vals)-1)) if len(g_vals) > 1 else 0
        b_std_v = math.sqrt(sum((x-b_avg_v)**2 for x in b_vals)/(len(b_vals)-1)) if len(b_vals) > 1 else 0
        se = math.sqrt(g_std_v**2/len(g_vals) + b_std_v**2/len(b_vals)) if (g_std_v + b_std_v) > 0 else 0.001
        t = abs(g_avg_v - b_avg_v) / se
        t_vals.append(t)
    t_rank = sorted(range(len(feature_names)), key=lambda i: t_vals[i], reverse=True)
    t_rank_map = {idx: rank+1 for rank, idx in enumerate(t_rank)}

    for fi in range(len(feature_names)):
        p(f"  {feature_names[fi]:<35} {weight_rank_map[fi]:>13} {corr_rank_map[fi]:>16} {t_rank_map[fi]:>12}")

    # Kendall tau benzeri uyum skoru
    p(f"\n  Siralama uyumu (Spearman rank korelasyonu):")
    def spearman(r1, r2):
        n = len(r1)
        d2 = sum((r1[i] - r2[i])**2 for i in range(n))
        return 1 - 6*d2/(n*(n**2-1))
    wr = [weight_rank_map[i] for i in range(len(feature_names))]
    cr = [corr_rank_map[i] for i in range(len(feature_names))]
    tr = [t_rank_map[i] for i in range(len(feature_names))]
    p(f"    Weight vs Korelasyon: rho = {spearman(wr, cr):.4f}")
    p(f"    Weight vs t-test   : rho = {spearman(wr, tr):.4f}")
    p(f"    Korelasyon vs t-test: rho = {spearman(cr, tr):.4f}")

    p("\n\n" + "=" * 120)
    p("  EK VERILER TAMAMLANDI!")
    p("=" * 120)

    # Dosyaya kaydet
    with open(os.path.join(PROJE, "makale-ek-veriler.txt"), "w") as f:
        f.write("\n".join(output))
    print(f"\n[KAYDEDILDI] makale-ek-veriler.txt ({len(output)} satir)")

if __name__ == "__main__":
    main()
