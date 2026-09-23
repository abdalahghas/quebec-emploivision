// Québec EmploiVision — client du dashboard.
// Lit les données exclusivement via l'API ASP.NET (/api/...), jamais de valeurs codées en dur.

const state = { region: "", industry: "", year: "", charts: {} };

const fmtInt = v => v == null ? "—" :
    Math.round(v).toLocaleString("fr-CA");
const fmtMoney = v => v == null ? "—" :
    v.toLocaleString("fr-CA", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " $";
const fmtPct = v => v == null ? "—" :
    (v >= 0 ? "+" : "") + v.toLocaleString("fr-CA", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + " %";

async function api(path) {
    const response = await fetch(path);
    if (!response.ok) throw new Error("API " + response.status);
    return response.json();
}

function errorBanner(view, message) {
    const old = view.querySelector(".error-banner");
    if (old) old.remove();
    const div = document.createElement("div");
    div.className = "error-banner";
    div.textContent = message;
    view.prepend(div);
}

function chartColors() {
    const css = getComputedStyle(document.body);
    return {
        primary: css.getPropertyValue("--primary").trim() || "#0f4c81",
        muted: css.getPropertyValue("--muted").trim() || "#64748b",
        border: css.getPropertyValue("--border").trim() || "#dde3ea",
        accent: css.getPropertyValue("--text").trim() || "#16212e"
    };
}

function lineChart(canvasId, labels, datasets, dashedSecond) {
    const colors = chartColors();
    if (state.charts[canvasId]) state.charts[canvasId].destroy();
    state.charts[canvasId] = new Chart(document.getElementById(canvasId), {
        type: "line",
        data: {
            labels,
            datasets: datasets.map((d, i) => ({
                label: d.label,
                data: d.data,
                borderColor: i === 0 ? colors.primary : colors.accent,
                backgroundColor: "transparent",
                borderDash: dashedSecond && i > 0 ? [6, 5] : [],
                borderWidth: 2,
                pointRadius: 2,
                tension: .3
            }))
        },
        options: {
            responsive: true,
            plugins: { legend: { labels: { color: colors.muted } } },
            scales: {
                x: { ticks: { color: colors.muted }, grid: { color: "transparent" } },
                y: { ticks: { color: colors.muted }, grid: { color: colors.border } }
            }
        }
    });
}

// ---------- Navigation ----------

document.querySelectorAll(".nav-item").forEach(button => {
    button.addEventListener("click", () => {
        document.querySelectorAll(".nav-item").forEach(b => b.classList.remove("active"));
        button.classList.add("active");
        document.querySelectorAll(".view").forEach(v => v.classList.remove("active"));
        document.getElementById("view-" + button.dataset.view).classList.add("active");
        document.getElementById("sidebar").classList.remove("open");
        if (button.dataset.view === "dashboard") loadDashboard();
        if (button.dataset.view === "professions") loadProfessions();
        if (button.dataset.view === "industries") loadIndustries();
        if (button.dataset.view === "regions") loadRegionCard();
        if (button.dataset.view === "comparison") loadComparison();
        if (button.dataset.view === "forecast") loadForecast();
        if (button.dataset.view === "quality") loadQuality();
        if (button.dataset.view === "imports") loadImportHistory();
        if (button.dataset.view === "pipeline") loadPipeline();
    });
});
document.getElementById("menuToggle").addEventListener("click", () =>
    document.getElementById("sidebar").classList.toggle("open"));

// ---------- Thème ----------

const savedTheme = localStorage.getItem("ev-theme") ||
    (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
applyTheme(savedTheme);
document.getElementById("themeToggle").addEventListener("click", () => {
    const current = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
    applyTheme(current);
    localStorage.setItem("ev-theme", current);
    refreshCharts();
});
function applyTheme(theme) {
    document.documentElement.dataset.theme = theme;
    document.getElementById("themeToggle").textContent = theme === "dark" ? "☀" : "☾";
}
function refreshCharts() {
    const active = document.querySelector(".view.active");
    if (active && active.id === "view-dashboard") loadDashboard();
    if (active && active.id === "view-comparison") loadComparison();
    if (active && active.id === "view-forecast") loadForecast();
}

// ---------- Filtres partagés ----------

async function loadFilterOptions() {
    const [regions, industries, years] = await Promise.all([
        api("/api/regions"), api("/api/industries"), api("/api/dashboard").catch(() => null)
    ]);
    fillSelect("filterRegion", regions, "Toutes les régions");
    fillSelect("filterIndustry", industries, "Toutes les industries");
    fillSelect("filterYear", years ? years : [], "Dernière année", true);
    fillSelect("compareLeft", regions, "—", false);
    fillSelect("compareRight", regions, "—", false);
    fillSelect("regionSelect", regions, "Choisir une région", false);
    fillSelect("forecastRegion", regions, "—", false);
}
function fillSelect(id, values, placeholder, reverse) {
    const select = document.getElementById(id);
    select.innerHTML = "";
    const first = document.createElement("option");
    first.value = ""; first.textContent = placeholder;
    select.appendChild(first);
    let list = [...values];
    if (reverse) list = list.slice().reverse();
    for (const value of list) {
        const option = document.createElement("option");
        option.value = value; option.textContent = value;
        select.appendChild(option);
    }
}
["filterRegion", "filterIndustry", "filterYear"].forEach(id =>
    document.getElementById(id).addEventListener("change", e => {
        state[id.replace("filter", "").toLowerCase()] = e.target.value;
        loadDashboard();
    }));

// ---------- Vue globale ----------

async function loadDashboard() {
    const view = document.getElementById("view-dashboard");
    const query = new URLSearchParams();
    if (state.region) query.set("region", state.region);
    if (state.industry) query.set("industry", state.industry);
    if (state.year) query.set("year", state.year);
    try {
        const data = await api("/api/dashboard?" + query.toString());
        renderKpi(data.kpi);
        lineChart("chartEmployment", data.employmentHistory.map(p => p.label),
            [{ label: "Emplois", data: data.employmentHistory.map(p => p.value) }]);
        lineChart("chartVacancies", data.vacancyHistory.map(p => p.label),
            [{ label: "Postes vacants", data: data.vacancyHistory.map(p => p.value) }]);
        lineChart("chartWages", data.wageHistory.map(p => p.label),
            [{ label: "Salaire horaire moyen", data: data.wageHistory.map(p => p.value) }]);
        lineChart("chartEmploymentFull", data.employmentHistory.map(p => p.label),
            [{ label: "Emplois", data: data.employmentHistory.map(p => p.value) }]);
        const old = view.querySelector(".error-banner"); if (old) old.remove();
    } catch {
        errorBanner(view, "Impossible de récupérer les données. Vérifiez la connexion SQL Server puis réessayez.");
    }
}

function renderKpi(kpi) {
    const grid = document.getElementById("kpiGrid");
    const variation = kpi.employmentVariationPct;
    grid.innerHTML = `
        <div class="kpi"><span class="kpi-label">Emplois</span>
            <span class="kpi-value">${fmtInt(kpi.employment)}</span>
            <span class="kpi-delta muted">${kpi.period}</span></div>
        <div class="kpi"><span class="kpi-label">Postes vacants</span>
            <span class="kpi-value">${fmtInt(kpi.vacancies)}</span>
            <span class="kpi-delta muted">si disponible pour la source</span></div>
        <div class="kpi"><span class="kpi-label">Salaire horaire moyen</span>
            <span class="kpi-value">${fmtMoney(kpi.averageWage)}</span>
            <span class="kpi-delta muted">moyenne pondérée</span></div>
        <div class="kpi"><span class="kpi-label">Variation annuelle</span>
            <span class="kpi-value">${fmtPct(variation)}</span>
            <span class="kpi-delta ${variation == null ? "muted" : variation >= 0 ? "up" : "down"}">
                par rapport à l'année précédente</span></div>`;
}

// ---------- Professions / industries ----------

let professionDebounce;
document.getElementById("professionSearch").addEventListener("input", e => {
    clearTimeout(professionDebounce);
    professionDebounce = setTimeout(() => loadProfessions(e.target.value), 250);
});

async function loadProfessions(search) {
    const query = new URLSearchParams();
    if (search) query.set("search", search);
    try {
        const rows = await api("/api/professions?" + query.toString());
        const body = document.querySelector("#professionTable tbody");
        body.innerHTML = rows.length === 0
            ? '<tr><td colspan="5">Aucune observation disponible pour cette recherche.</td></tr>'
            : rows.map(r => `<tr>
                <td>${r.code}</td><td>${r.name}</td>
                <td>${fmtInt(r.employment)}</td><td>${fmtMoney(r.averageWage)}</td><td>${fmtInt(r.vacancies)}</td>
              </tr>`).join("");
    } catch {
        errorBanner(document.getElementById("view-professions"),
            "Impossible de récupérer les professions.");
    }
}

async function loadIndustries() {
    try {
        const rows = await api("/api/industries");
        const body = document.querySelector("#industryTable tbody");
        body.innerHTML = rows.map(r => `<tr>
            <td>${r.code}</td><td>${r.name}</td>
            <td>${fmtInt(r.employment)}</td><td>${fmtMoney(r.averageWage)}</td><td>${fmtInt(r.vacancies)}</td>
          </tr>`).join("");
    } catch {
        errorBanner(document.getElementById("view-industries"),
            "Impossible de récupérer les industries.");
    }
}

// ---------- Régions ----------

document.getElementById("regionSelect").addEventListener("change", e => {
    if (e.target.value) loadRegionCard(e.target.value);
});

async function loadRegionCard(name) {
    const region = name || document.getElementById("regionSelect").value;
    if (!region) return;
    try {
        const card = await api("/api/regions/" + encodeURIComponent(region));
        document.getElementById("regionKpi").innerHTML = `
            <div class="kpi"><span class="kpi-label">Région</span>
                <span class="kpi-value" style="font-size:19px">${card.name}</span></div>
            <div class="kpi"><span class="kpi-label">Emplois</span>
                <span class="kpi-value">${fmtInt(card.employment)}</span></div>
            <div class="kpi"><span class="kpi-label">Salaire moyen</span>
                <span class="kpi-value">${fmtMoney(card.averageWage)}</span></div>
            <div class="kpi"><span class="kpi-label">Postes vacants</span>
                <span class="kpi-value">${fmtInt(card.vacancies)}</span></div>`;
        document.getElementById("regionChartTitle").textContent =
            "Historique de l'emploi — " + card.name;
        lineChart("chartRegion", card.history.map(p => p.label),
            [{ label: "Emplois", data: card.history.map(p => p.value) }]);
    } catch {
        errorBanner(document.getElementById("view-regions"),
            "Impossible de récupérer cette région.");
    }
}

// ---------- Comparateur ----------

["compareLeft", "compareRight"].forEach(id =>
    document.getElementById(id).addEventListener("change", loadComparison));

async function loadComparison() {
    const left = document.getElementById("compareLeft").value;
    const right = document.getElementById("compareRight").value;
    if (!left || !right) return;
    try {
        const data = await api(`/api/comparison?left=${encodeURIComponent(left)}&right=${encodeURIComponent(right)}`);
        document.getElementById("compareLeftName").textContent = data.leftRegion;
        document.getElementById("compareRightName").textContent = data.rightRegion;
        document.querySelector("#comparisonTable tbody").innerHTML = data.metrics.map(m => `<tr>
            <td>${m.metric}</td>
            <td>${m.metric.includes("Salaire") ? fmtMoney(m.leftValue) : fmtInt(m.leftValue)}</td>
            <td>${m.metric.includes("Salaire") ? fmtMoney(m.rightValue) : fmtInt(m.rightValue)}</td>
          </tr>`).join("");
        lineChart("chartComparison", data.leftHistory.map(p => p.label), [
            { label: data.leftRegion, data: data.leftHistory.map(p => p.value) },
            { label: data.rightRegion, data: data.rightHistory.map(p => p.value) }
        ], true);
    } catch {
        errorBanner(document.getElementById("view-comparison"),
            "Impossible de comparer ces régions.");
    }
}

// ---------- Prévisions ----------

["forecastRegion", "forecastHorizon"].forEach(id =>
    document.getElementById(id).addEventListener("change", loadForecast));

async function loadForecast() {
    const region = document.getElementById("forecastRegion").value;
    const horizon = document.getElementById("forecastHorizon").value;
    if (!region) return;
    try {
        const data = await api(`/api/forecast?region=${encodeURIComponent(region)}&horizon=${horizon}`);
        document.getElementById("modelGrid").innerHTML = `
            <div class="model-cell"><div class="k">Modèle</div><div class="v">${data.model}</div></div>
            <div class="model-cell"><div class="k">Observations</div><div class="v">${data.trainingObservations}</div></div>
            <div class="model-cell"><div class="k">Horizon</div><div class="v">${data.forecastHorizon}</div></div>
            <div class="model-cell"><div class="k">R²</div><div class="v">${data.rSquared}</div></div>
            <div class="model-cell"><div class="k">Pente / période</div><div class="v">${fmtInt(data.slopePerPeriod)}</div></div>`;
        document.getElementById("methodologyNote").textContent = data.methodology;
        const colors = chartColors();
        const labels = [...data.history.map(p => p.label), ...data.forecast.map(p => p.label)];
        const historyData = data.history.map(p => p.value);
        const forecastData = [...historyData.map(() => null),
            historyData[historyData.length - 1], ...data.forecast.map(p => p.value)];
        if (state.charts.chartForecast) state.charts.chartForecast.destroy();
        state.charts.chartForecast = new Chart(document.getElementById("chartForecast"), {
            type: "line",
            data: {
                labels,
                datasets: [
                    { label: "Historique", data: historyData, borderColor: colors.primary, borderWidth: 2, pointRadius: 2, tension: .3 },
                    { label: "Prévision", data: forecastData, borderColor: colors.accent, borderDash: [6, 5], borderWidth: 2, pointRadius: 2, tension: .3 }
                ]
            },
            options: {
                responsive: true,
                plugins: { legend: { labels: { color: colors.muted } } },
                scales: {
                    x: { ticks: { color: colors.muted }, grid: { color: "transparent" } },
                    y: { ticks: { color: colors.muted }, grid: { color: colors.border } }
                }
            }
        });
    } catch {
        errorBanner(document.getElementById("view-forecast"),
            "Pas assez d'observations historiques pour entraîner le modèle.");
    }
}

// ---------- Qualité ----------

async function loadQuality() {
    try {
        const data = await api("/api/data-quality");
        const score = data.score;
        document.getElementById("scoreValue").textContent = score.overall + " / 100";
        document.getElementById("scoreRing").style.background =
            `conic-gradient(var(--primary) ${score.overall * 3.6}deg, var(--border) 0deg)`;
        document.getElementById("scoreBars").innerHTML = [
            ["Complétude", score.completeness], ["Validité", score.validity],
            ["Unicité", score.uniqueness], ["Cohérence", score.consistency]
        ].map(([name, value]) => `<div class="score-bar-row">
                <span>${name}</span>
                <div class="score-track"><div class="score-fill" style="width:${value}%"></div></div>
                <span>${value} %</span></div>`).join("");
        const anomalies = data.anomalies || [];
        document.getElementById("anomalyList").innerHTML = anomalies.length === 0
            ? '<p class="note">Aucune anomalie détectée sur les dernières variations observées.</p>'
            : anomalies.map(a => `<div class="anomaly-card">
                <b>⚠ Observation inhabituelle</b> — ${a.region}, ${a.indicator} :
                variation de ${fmtPct(a.observedVariationPct)} alors que l'historique
                se situe habituellement entre ${fmtPct(a.historicalLowPct)} et ${fmtPct(a.historicalHighPct)}.
                <em>${a.status}.</em></div>`).join("");
    } catch {
        errorBanner(document.getElementById("view-quality"),
            "Impossible de calculer le score de qualité.");
    }
}

// ---------- Imports ----------

document.getElementById("importForm").addEventListener("submit", async event => {
    event.preventDefault();
    const button = event.target.querySelector("button");
    button.disabled = true; button.textContent = "Traitement…";
    const formData = new FormData();
    formData.append("dataset", document.getElementById("importDataset").value);
    formData.append("file", document.getElementById("importFile").files[0]);
    const resultBox = document.getElementById("importResult");
    resultBox.hidden = false;
    resultBox.className = "import-result";
    try {
        const response = await fetch("/api/import", { method: "POST", body: formData });
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || data.title || "Import interrompu.");
        resultBox.innerHTML = `<h3>Import #${data.importRunId} — ${data.status}</h3>
            <p>${data.rowsProcessed} lignes traitées, ${data.rowsAccepted} acceptées,
               ${data.rowsRejected} rejetées, ${data.rowsWarning} avertissements,
               en ${data.totalDurationSeconds} s.</p>
            ${data.errors.length ? "<ul>" + data.errors.slice(0, 10).map(e =>
                `<li>Ligne ${e.rowNumber}, colonne ${e.columnName} : attendu ${e.expected}, reçu « ${e.received} »</li>`).join("") + "</ul>" : ""}`;
        if (data.rowsRejected > 0) resultBox.className = "import-result error";
    } catch (error) {
        resultBox.className = "import-result error";
        resultBox.innerHTML = `<h3>Échec de l'import</h3><p>${error.message}</p>`;
    } finally {
        button.disabled = false; button.textContent = "Importer";
        loadImportHistory();
    }
});

async function loadImportHistory() {
    try {
        const rows = await api("/api/imports");
        document.querySelector("#importHistory tbody").innerHTML =
            rows.length === 0 ? '<tr><td colspan="7">Aucun import enregistré pour l\'instant.</td></tr>'
            : rows.map(r => `<tr>
                <td>${new Date(r.startedAt).toLocaleString("fr-CA")}</td>
                <td>${r.sourceFileName}</td>
                <td><span class="status-pill ${r.status === "SUCCESS" ? "good" : r.status === "WARNING" ? "warn" : "bad"}">${r.status}</span></td>
                <td>${r.rowsProcessed}</td><td>${r.rowsAccepted}</td><td>${r.rowsRejected}</td>
                <td><a href="#" data-import="${r.importRunId}">détails</a></td>
              </tr>`).join("");
        document.querySelectorAll("[data-import]").forEach(link =>
            link.addEventListener("click", async e => {
                e.preventDefault();
                showImportDetail(e.target.dataset.import);
            }));
    } catch {
        errorBanner(document.getElementById("view-imports"),
            "Impossible de récupérer l'historique des imports.");
    }
}

async function showImportDetail(id) {
    try {
        const detail = await api("/api/imports/" + id);
        const box = document.getElementById("importResult");
        box.hidden = false; box.className = "import-result";
        box.innerHTML = `<h3>Import #${detail.importRunId} — ${detail.status}</h3>
            <p>Début : ${new Date(detail.startedAt).toLocaleTimeString("fr-CA")} ·
               Fin : ${new Date(detail.finishedAt).toLocaleTimeString("fr-CA")}</p>
            <p>${detail.rowsProcessed} traitées · ${detail.rowsAccepted} acceptées ·
               ${detail.rowsRejected} rejetées · ${detail.rowsWarning} avertissements</p>
            ${detail.errors.length ? "<ul>" + detail.errors.map(e =>
                `<li>Ligne ${e.rowNumber}, colonne ${e.columnName} : ${e.message}</li>`).join("") + "</ul>"
                : "<p>Aucune erreur ligne par ligne.</p>"}`;
    } catch {
        errorBanner(document.getElementById("view-imports"), "Import introuvable.");
    }
}

// ---------- Pipeline ----------

async function loadPipeline() {
    try {
        const status = await api("/api/pipeline");
        const pill = document.getElementById("pipelineHealth");
        pill.textContent = status.health;
        pill.className = "status-pill " +
            (status.health === "HEALTHY" ? "good" : status.health === "DEGRADED" ? "bad" : "warn");
        document.querySelector("#pipelineTable tbody").innerHTML =
            status.sources.length === 0
                ? '<tr><td colspan="3">Aucune source encore importée.</td></tr>'
                : status.sources.map(s => `<tr>
                    <td>${s.source}</td>
                    <td>${new Date(s.lastImport).toLocaleString("fr-CA")}</td>
                    <td>${s.ok ? "✓" : "✗"}</td></tr>`).join("");
        const last = status.lastPipeline;
        document.getElementById("lastPipeline").innerHTML = last
            ? ["Extract", "Validate", "Transform", "Load"].map(step => `<div class="step-card ok">
                  <div class="step-mark">✓</div>
                  <div class="step-name">${step}</div>
                  <div class="step-meta">${last.datasetName}</div></div>`).join("") +
              `<p class="note">${last.rowsProcessed} lignes traitées, ${last.rowsAccepted} acceptées,
                 ${last.rowsRejected} rejetées, ${last.rowsWarning} avertissements.</p>`
            : '<p class="note">Aucun pipeline exécuté pour l\'instant.</p>';
    } catch {
        errorBanner(document.getElementById("view-pipeline"),
            "Impossible de récupérer l'état du pipeline.");
    }
}

// ---------- Recherche globale → professions ----------

let globalDebounce;
document.getElementById("globalSearch").addEventListener("input", e => {
    clearTimeout(globalDebounce);
    globalDebounce = setTimeout(() => {
        const value = e.target.value.trim();
        if (value.length < 2) return;
        document.querySelector('[data-view="professions"]').click();
        document.getElementById("professionSearch").value = value;
        loadProfessions(value);
    }, 300);
});

// ---------- Démarrage ----------

loadFilterOptions().then(loadDashboard).catch(() => loadDashboard());
