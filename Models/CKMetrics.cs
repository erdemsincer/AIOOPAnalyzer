namespace AIOOPAnalyzer.Models
{
    /// <summary>
    /// Chidamber & Kemerer (CK) Nesne Yönelimli Metrikler.
    /// Her sınıf için ayrı ayrı veya tüm kod için ortalama hesaplanabilir.
    /// </summary>
    public class CKMetrics
    {
        /// <summary>Sınıf ismi (sınıf bazlı hesaplamada)</summary>
        public string ClassName { get; set; } = "";

        // ── 1. WMC — Weighted Methods per Class ──
        /// <summary>
        /// Sınıf Başına Ağırlıklı Method: Sınıftaki metotların cyclomatic complexity toplamı.
        /// Yüksek WMC = karmaşık sınıf = kötü.
        /// İdeal: WMC ≤ 10
        /// </summary>
        public int WMC { get; set; }

        // ── 2. DIT — Depth of Inheritance Tree ──
        /// <summary>
        /// Kalıtım Ağacının Derinliği: Sınıfın kalıtım hiyerarşisindeki derinliği.
        /// 0 = base class yok, 1 = bir parent, 2 = grandparent var, vb.
        /// Yüksek DIT = fazla bağımlılık = zor bakım.
        /// İdeal: DIT ≤ 3
        /// </summary>
        public int DIT { get; set; }

        // ── 3. NOC — Number of Children ──
        /// <summary>
        /// Alt Sınıf Sayısı: Bu sınıftan doğrudan türeyen sınıf sayısı.
        /// Yüksek NOC = sınıfın çok yaygın kullanıldığını gösterir (iyi veya kötü olabilir).
        /// Çok yüksek NOC = soyut tasarım eksik olabilir.
        /// İdeal: NOC ≤ 5
        /// </summary>
        public int NOC { get; set; }

        // ── 4. CBO — Coupling Between Object Classes ──
        /// <summary>
        /// Nesneler Arası Eşleme: Sınıfın bağımlı olduğu diğer sınıf sayısı.
        /// Yüksek CBO = sıkı bağlılık = kötü (değişiklik zor).
        /// İdeal: CBO ≤ 5
        /// </summary>
        public int CBO { get; set; }

        // ── 5. RFC — Response for a Class ──
        /// <summary>
        /// Sınıf Yanıt Sayısı: Sınıftaki metod sayısı + bu metotların çağırdığı benzersiz metod sayısı.
        /// Yüksek RFC = sınıf çok fazla iş yapıyor.
        /// İdeal: RFC ≤ 20
        /// </summary>
        public int RFC { get; set; }

        // ── 6. LCOM — Lack of Cohesion of Methods ──
        /// <summary>
        /// Uyumsuzluk: Sınıf içindeki metotların ortak alan kullanım oranı.
        /// Yüksek LCOM = metotlar farklı alanlarla çalışıyor = düşük uyum = kötü.
        /// 0 = tüm metotlar aynı alanları kullanıyor (iyi).
        /// LCOM = max(0, P - Q) formülü. P: ortak alan paylaşmayan çiftler, Q: paylaşanlar.
        /// İdeal: LCOM = 0
        /// </summary>
        public int LCOM { get; set; }

        /// <summary>CK metriklerini özet string olarak döndürür</summary>
        public override string ToString()
        {
            return $"[{ClassName}] WMC={WMC}, DIT={DIT}, NOC={NOC}, CBO={CBO}, RFC={RFC}, LCOM={LCOM}";
        }
    }
}
