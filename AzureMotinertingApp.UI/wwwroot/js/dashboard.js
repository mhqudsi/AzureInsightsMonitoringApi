let allEndpoints = [];
let isLoading = false;
let lastAutoRefreshTimer = null;
let endpointDataTable = null;
let currentDuration = '1h';

function getEl(id) {
    return document.getElementById(id);
}

function setAlert(message) {
    const alert = getEl('pageAlert');
    if (!alert) return;

    if (!message) {
        alert.classList.add('d-none');
        alert.textContent = '';
        return;
    }

    alert.textContent = message;
    alert.classList.remove('d-none');
}

function formatNumber(value) {
    const n = Number(value ?? 0);
    return Number.isFinite(n) ? n.toLocaleString() : '—';
}

function formatMs(value) {
    const n = Number(value);
    return Number.isFinite(n) ? n.toFixed(2) : '—';
}

function setLastUpdated(date) {
    const el = getEl('lastUpdated');
    if (!el) return;
    el.textContent = date ? date.toLocaleString() : '—';
}

function getDurationUnit() {
    const unit = (getEl('durationUnit')?.value ?? 'h').toLowerCase();
    return ['m', 'h', 'd'].includes(unit) ? unit : 'h';
}

function getDurationValue() {
    const raw = Number(getEl('durationValue')?.value ?? 1);
    if (!Number.isFinite(raw)) return 1;
    return Math.max(1, Math.floor(raw));
}

function getSelectedDuration() {
    return `${getDurationValue()}${getDurationUnit()}`;
}

function setDurationDisplay(duration) {
    currentDuration = duration;
    const label = getEl('selectedDurationLabel');
    const windowText = getEl('windowText');
    if (label) label.textContent = duration;
    if (windowText) windowText.textContent = `last ${duration}`;
}

function setLoading(loading) {
    isLoading = loading;

    const refreshBtn = getEl('refreshBtn');
    const spinner = getEl('refreshSpinner');

    if (refreshBtn && 'disabled' in refreshBtn) {
        refreshBtn.disabled = loading;
    }

    // Guard against non-DOM values to avoid runtime crashes.
    if (spinner && spinner.classList && typeof spinner.classList.toggle === 'function') {
        spinner.classList.toggle('d-none', !loading);
    }
}

function getSearchQuery() {
    const input = getEl('endpointSearch');
    return (input?.value ?? '').trim().toLowerCase();
}

function applyFilter(endpoints) {
    const q = getSearchQuery();
    if (!q) return endpoints;
    return endpoints.filter(e => (e?.endpointName ?? '').toString().toLowerCase().includes(q));
}

function updateResultsHint(filteredCount, totalCount) {
    const el = getEl('resultsHint');
    if (!el) return;

    if (totalCount === 0) {
        el.textContent = 'No data';
        return;
    }

    if (filteredCount === totalCount) {
        el.textContent = `Showing ${formatNumber(totalCount)} endpoints`;
        return;
    }

    el.textContent = `Showing ${formatNumber(filteredCount)} of ${formatNumber(totalCount)} endpoints`;
}

function renderSummary(summary) {
    const host = getEl('summaryCards');
    if (!host) return;

    const total = Number(summary?.totalRequests ?? 0);
    const success = Number(summary?.successRequests ?? 0);
    const failed = Number(summary?.failedRequests ?? 0);
    const avg = Number(summary?.averageResponseMs ?? 0);
    const successRate = total > 0 ? (success / total) * 100 : 0;

    host.innerHTML = `
        <div class="row g-3">
            <div class="col-12 col-sm-6 col-lg-3">
                <div class="metric-card border rounded-3 p-3">
                    <div class="metric-label">Total requests</div>
                    <div class="metric-value">${formatNumber(total)}</div>
                </div>
            </div>
            <div class="col-12 col-sm-6 col-lg-3">
                <div class="metric-card border rounded-3 p-3">
                    <div class="metric-label">Success</div>
                    <div class="metric-value text-success">${formatNumber(success)}</div>
                    <div class="metric-sub">${successRate.toFixed(2)}% success rate</div>
                </div>
            </div>
            <div class="col-12 col-sm-6 col-lg-3">
                <div class="metric-card border rounded-3 p-3">
                    <div class="metric-label">Failures</div>
                    <div class="metric-value text-danger">${formatNumber(failed)}</div>
                </div>
            </div>
            <div class="col-12 col-sm-6 col-lg-3">
                <div class="metric-card border rounded-3 p-3">
                    <div class="metric-label">Avg response</div>
                    <div class="metric-value">${formatMs(avg)}<span class="metric-unit"> ms</span></div>
                </div>
            </div>
        </div>
    `;
}

function renderTable(endpoints) {
    const tableEl = document.getElementById('endpointTable');
    if (!tableEl) return;

    const empty = getEl('emptyState');

    if (window.jQuery && window.jQuery.fn && window.jQuery.fn.dataTable) {
        const dt = ensureDataTable(tableEl);
        dt.clear();

        endpoints.forEach(e => {
            const endpointName = (e?.endpointName ?? '—').toString();
            const endpointLink = `
                <a class="fw-semibold text-decoration-none" href="/Home/Insight?endpoint=${encodeURIComponent(endpointName)}">
                    ${escapeHtml(endpointName)}
                </a>
            `;

            const fail = Number(e?.failedRequests ?? 0);
            dt.row.add([
                endpointLink,
                `<div class="text-end">${escapeHtml(formatNumber(e?.totalRequests))}</div>`,
                `<div class="text-end text-success">${escapeHtml(formatNumber(e?.successRequests))}</div>`,
                `<div class="text-end ${fail > 0 ? 'text-danger' : ''}">${escapeHtml(formatNumber(e?.failedRequests))}</div>`,
                `<div class="text-end">${escapeHtml(formatMs(e?.averageDurationMs))}</div>`,
                `<div class="text-end">${escapeHtml(formatMs(e?.maxDurationMs))}</div>`,
                `<div class="text-nowrap">${escapeHtml(formatDate(e?.lastCalled))}</div>`
            ]);
        });

        dt.draw();

        const isEmpty = endpoints.length === 0;
        if (empty) empty.classList.toggle('d-none', !isEmpty);
        return;
    }

    const tbody = tableEl.querySelector('tbody');
    if (!tbody) return;

    tbody.innerHTML = '';
    endpoints.forEach(e => {
        const fail = Number(e?.failedRequests ?? 0);
        const rowClass = fail > 0 ? 'table-warning' : '';

        const endpointName = (e?.endpointName ?? '—').toString();

        const tr = document.createElement('tr');
        if (rowClass) tr.classList.add(...rowClass.split(' '));

        tr.innerHTML = `
            <td class="endpoint-cell">
                <a class="fw-semibold text-decoration-none" href="/Home/Insight?endpoint=${encodeURIComponent(endpointName)}">
                    ${escapeHtml(endpointName)}
                </a>
            </td>
            <td class="text-end">${formatNumber(e?.totalRequests)}</td>
            <td class="text-end text-success">${formatNumber(e?.successRequests)}</td>
            <td class="text-end ${fail > 0 ? 'text-danger' : ''}">${formatNumber(e?.failedRequests)}</td>
            <td class="text-end">${formatMs(e?.averageDurationMs)}</td>
            <td class="text-end">${formatMs(e?.maxDurationMs)}</td>
            <td class="text-nowrap">${formatDate(e?.lastCalled)}</td>
        `;
        tbody.appendChild(tr);
    });

    const isEmpty = endpoints.length === 0;
    if (empty) empty.classList.toggle('d-none', !isEmpty);
}

function escapeHtml(value) {
    const str = (value ?? '').toString();
    return str
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

function formatDate(value) {
    if (!value) return '—';
    const d = new Date(value);
    return Number.isFinite(d.getTime()) ? d.toLocaleString() : '—';
}

function renderFromCache() {
    const filtered = applyFilter(allEndpoints);
    updateResultsHint(filtered.length, allEndpoints.length);
    renderTable(filtered);
}

function ensureDataTable(tableEl) {
    if (endpointDataTable) return endpointDataTable;

    const $table = window.jQuery(tableEl);

    // Add a second header row for per-column filters (only once).
    const $thead = $table.find('thead');
    if ($thead.find('tr').length === 1) {
        const $filterRow = window.jQuery('<tr class="dt-filters"></tr>');
        $thead.find('th').each(function () {
            const title = window.jQuery(this).text().trim();
            const isNumeric = /^(Total|Success|Fail|Avg|Max)/i.test(title);
            const placeholder = title ? `Filter ${title}...` : 'Filter...';

            const $th = window.jQuery('<th></th>');
            const $input = window.jQuery('<input type="text" class="form-control form-control-sm" />');
            $input.attr('placeholder', placeholder);
            if (isNumeric) $input.addClass('text-end');

            $th.append($input);
            $filterRow.append($th);
        });
        $thead.append($filterRow);
    }

    endpointDataTable = $table.DataTable({
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        orderCellsTop: true,
        fixedHeader: false,
        autoWidth: false,
        dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
            "<'row'<'col-sm-12'tr>>" +
            "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
        language: {
            search: "Global filter:",
            searchPlaceholder: "Search all columns..."
        },
        columnDefs: [
            { targets: [1, 2, 3, 4, 5], className: 'text-end' }
        ]
    });

    // Wire up per-column filters
    endpointDataTable.columns().every(function (colIdx) {
        const that = this;
        const input = window.jQuery(endpointDataTable.table().header()).parent().find('tr.dt-filters th').eq(colIdx).find('input');
        if (!input || input.length === 0) return;

        input.on('input', function () {
            const val = window.jQuery(this).val();
            that.search(val ?? '').draw();
        });
    });

    return endpointDataTable;
}

async function fetchJson(url) {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => controller.abort(), 15000);
    let res;
    try {
        res = await fetch(url, {
            headers: { 'Accept': 'application/json' },
            credentials: 'same-origin',
            signal: controller.signal
        });
    } catch (err) {
        if (err?.name === 'AbortError') {
            throw new Error('Request timed out. Please try refresh again.');
        }
        throw err;
    } finally {
        window.clearTimeout(timeoutId);
    }

    if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(`Request failed (${res.status}) ${text}`.trim());
    }
    return await res.json();
}

async function loadDashboard({ showSpinner } = { showSpinner: true }) {
    if (isLoading) return;
    setAlert(null);
    setLoading(!!showSpinner);

    try {
        const duration = getSelectedDuration();
        setDurationDisplay(duration);
        const query = `?duration=${encodeURIComponent(duration)}`;

        const [summary, endpoints] = await Promise.all([
            fetchJson(`/api/insights/summary${query}`),
            fetchJson(`/api/insights/endpoints${query}`)
        ]);

        allEndpoints = Array.isArray(endpoints) ? endpoints : [];
        renderSummary(summary);
        renderFromCache();
        setLastUpdated(new Date());
    } catch (err) {
        setAlert(err?.message ?? 'Failed to load dashboard data.');
    } finally {
        setLoading(false);
    }
}

function debounce(fn, delayMs) {
    let t = null;
    return (...args) => {
        if (t) window.clearTimeout(t);
        t = window.setTimeout(() => fn(...args), delayMs);
    };
}

function wireInteractions() {
    const refreshBtn = getEl('refreshBtn');
    refreshBtn?.addEventListener('click', () => loadDashboard({ showSpinner: true }));

    const durationValue = getEl('durationValue');
    const durationUnit = getEl('durationUnit');
    const onDurationChange = () => {
        setDurationDisplay(getSelectedDuration());
        loadDashboard({ showSpinner: true });
    };
    durationValue?.addEventListener('change', onDurationChange);
    durationUnit?.addEventListener('change', onDurationChange);

    const search = getEl('endpointSearch');
    const clear = getEl('clearSearch');

    const onSearch = debounce(() => renderFromCache(), 150);
    search?.addEventListener('input', onSearch);

    clear?.addEventListener('click', () => {
        if (search) search.value = '';
        renderFromCache();
        search?.focus();
    });
}

function startAutoRefresh() {
    // Auto refresh only on dashboards that actually render data widgets.
    if (!document.getElementById('endpointTable')) return;
    if (lastAutoRefreshTimer) window.clearInterval(lastAutoRefreshTimer);
    lastAutoRefreshTimer = window.setInterval(() => loadDashboard({ showSpinner: false }), 60000);
}

wireInteractions();
setDurationDisplay(getSelectedDuration());
loadDashboard({ showSpinner: true });
//startAutoRefresh();