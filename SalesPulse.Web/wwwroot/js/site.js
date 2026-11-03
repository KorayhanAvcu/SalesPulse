// =========================================================
// SalesPulse - Bütün sayfaların ortak kullandığı yardımcılar
// =========================================================

const SP = (() => {
    const tr = "tr-TR";

    // ---- Sayı / para / tarih biçimleri (Türkçe) ----
    const money = (v) => new Intl.NumberFormat(tr, { style: "currency", currency: "TRY", maximumFractionDigits: 0 }).format(v ?? 0);
    const money2 = (v) => new Intl.NumberFormat(tr, { style: "currency", currency: "TRY", minimumFractionDigits: 2 }).format(v ?? 0);
    const num = (v) => new Intl.NumberFormat(tr).format(v ?? 0);
    const compactMoney = (v) => new Intl.NumberFormat(tr, { notation: "compact", maximumFractionDigits: 1 }).format(v ?? 0) + " ₺";
    const pct = (v) => v === null || v === undefined ? "—" : new Intl.NumberFormat(tr, { maximumFractionDigits: 1 }).format(v) + "%";
    const date = (v) => v ? new Date(v).toLocaleDateString(tr) : "—";
    const dateTime = (v) => v ? new Date(v).toLocaleString(tr, { dateStyle: "short", timeStyle: "short" }) : "—";
    const monthLabel = (v) => new Date(v).toLocaleDateString(tr, { month: "short", year: "2-digit" });

    // <input type="date"> için "yyyy-MM-dd"
    const isoDate = (d) => {
        const x = new Date(d);
        return `${x.getFullYear()}-${String(x.getMonth() + 1).padStart(2, "0")}-${String(x.getDate()).padStart(2, "0")}`;
    };

    // HTML'e yazarken < > & gibi karakterleri zararsız hale getir
    const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) =>
        ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

    // ---- Sunucudan JSON al ----
    async function getJson(url) {
        const res = await fetch(url, { headers: { "Accept": "application/json" } });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        return res.json();
    }

    // ---- Grafik renkleri (pastel tema) ----
    const colors = {
        blue: "#5b8def", lavender: "#9b87f5", mint: "#3fbf9b",
        orange: "#f29a52", pink: "#e7799f", gray: "#8a93a6", teal: "#4bb4d6"
    };
    const series = [colors.blue, colors.lavender, colors.mint, colors.orange, colors.pink, colors.teal, colors.gray];

    // Sipariş durumu -> renk tonu (StatusId: 1 Ön Sipariş ... 6 İade)
    const statusTone = { 1: "lavender", 2: "orange", 3: "blue", 4: "mint", 5: "pink", 6: "gray" };
    const statusBadge = (id, name) => `<span class="sp-badge tone-${statusTone[id] || "gray"}">${esc(name)}</span>`;

    // Chart.js genel ayarları
    function setupCharts() {
        if (!window.Chart) return;
        Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
        Chart.defaults.color = "#6b7489";
        Chart.defaults.borderColor = "#eef0f5";
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
        Chart.defaults.plugins.legend.labels.boxWidth = 8;
        Chart.defaults.maintainAspectRatio = false;
    }

    // Bir grafiği yeniden çizmeden önce eskisini temizle
    const charts = {};
    function drawChart(id, config) {
        if (charts[id]) charts[id].destroy();
        charts[id] = new Chart(document.getElementById(id), config);
        return charts[id];
    }

    // Tarih filtresi bileşeni (Dashboard ve Sales sayfaları kullanır)
    // onApply(start, end) -> veriyi yeniden yükle
    function bindDateFilter(root, onApply) {
        const startEl = root.querySelector("[name=startDate]");
        const endEl = root.querySelector("[name=endDate]");
        const dataEnd = new Date(root.dataset.dataEnd);

        root.querySelectorAll("[data-preset]").forEach((btn) => {
            btn.addEventListener("click", () => {
                const end = new Date(dataEnd);
                const start = new Date(dataEnd);
                const p = btn.dataset.preset;
                if (p === "30d") start.setDate(start.getDate() - 29);
                if (p === "3m") { start.setMonth(start.getMonth() - 3); start.setDate(start.getDate() + 1); }
                if (p === "12m") { start.setMonth(start.getMonth() - 12); start.setDate(start.getDate() + 1); }
                if (p === "ytd") { start.setMonth(0); start.setDate(1); }
                startEl.value = isoDate(start);
                endEl.value = isoDate(end);
                root.querySelectorAll("[data-preset]").forEach((b) => b.classList.toggle("active", b === btn));
                onApply(startEl.value, endEl.value);
            });
        });

        root.addEventListener("submit", (e) => {
            e.preventDefault();
            root.querySelectorAll("[data-preset]").forEach((b) => b.classList.remove("active"));
            onApply(startEl.value, endEl.value);
        });
    }

    return { money, money2, num, compactMoney, pct, date, dateTime, monthLabel, isoDate, esc, getJson,
             colors, series, statusBadge, setupCharts, drawChart, bindDateFilter };
})();

// ---- Layout: mobilde menüyü aç/kapat, üst arama kutusu ----
document.addEventListener("DOMContentLoaded", () => {
    SP.setupCharts();

    document.querySelector(".sp-menu-toggle")?.addEventListener("click", () =>
        document.body.classList.toggle("sp-menu-open"));

    // Üst bardaki arama: sipariş no veya müşteri adı -> Order Explorer'da ara
    document.getElementById("globalSearch")?.addEventListener("submit", (e) => {
        e.preventDefault();
        const q = e.target.querySelector("input").value.trim();
        window.location.href = "/Orders" + (q ? "?search=" + encodeURIComponent(q) : "");
    });
});
