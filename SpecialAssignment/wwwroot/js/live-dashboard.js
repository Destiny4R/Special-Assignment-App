/* live-dashboard.js — active report list + Google Maps markers + alerts (§8–§10) */
(function () {
  'use strict';
  // NOTE: window.__dashboardConfig is assigned by an inline script AFTER this
  // file loads — read it lazily inside initOpsMap, never at eval time.
  let map = null;
  const markers = new Map(); // reportId -> google.maps.Marker
  let connection = null;

  let activeFilter = 'ALL';
  let lastRows = [];

  function statusPill(status) {
    const map = { LIVE: 'pill-live', STALE: 'pill-stale', OFFLINE: 'pill-offline', ENDED: 'pill-ended' };
    const cls = map[status] || 'pill-offline';
    const dot = status === 'LIVE' ? '<span class="dot dot-pulse" aria-hidden="true"></span>' : '<span class="dot" aria-hidden="true"></span>';
    return `<span class="pill ${cls}">${dot}${status}</span>`;
  }

  function esc(s) { return String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }

  function setStat(id, v) { const el = document.getElementById(id); if (el) el.textContent = v; }

  async function loadActive() {
    const res = await fetch('/api/dashboard/reports/active');
    if (res.status === 401) { window.location = '/Identity/Account/Login?returnUrl=/Dashboard'; return; }
    const rows = await res.json();
    lastRows = rows;
    renderTable(rows);
    rows.forEach(upsertMarker);
  }

  function renderTable(rows) {
    const tbody = document.getElementById('reportsBody');
    if (!tbody) return;
    const live = rows.filter(r => r.status === 'LIVE').length;
    const stale = rows.filter(r => r.status === 'STALE').length;
    const offline = rows.filter(r => r.status === 'OFFLINE').length;
    const count = document.getElementById('liveCount');
    if (count) count.innerHTML = `<span class="dot dot-pulse" aria-hidden="true"></span>${live}&nbsp;live`;
    setStat('statLive', live);
    setStat('statStale', stale);
    setStat('statOffline', offline);
    const shown = activeFilter === 'ALL' ? rows : rows.filter(r => r.status === activeFilter);
    if (!shown.length) {
      tbody.innerHTML = `<tr><td colspan="4"><div class="empty-state">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 3"/></svg>
        <strong>${rows.length ? `No ${activeFilter.toLowerCase()} reports` : 'No active reporters'}</strong>
        <span>New reports appear here without refresh.</span></div></td></tr>`;
      return;
    }
    tbody.innerHTML = shown.map(r => `
      <tr data-report="${esc(r.reportId)}">
        <td><span class="cmd-id">${esc(r.reportId)}</span></td>
        <td>${statusPill(r.status)} <span class="visually-hidden">${esc(r.displayStatus)}</span></td>
        <td>${esc(r.lastSeenAgo)}</td>
        <td style="text-align:right"><a class="btn btn-sm btn-secondary" href="/Map/Location?reportId=${encodeURIComponent(r.reportId)}">View Location</a></td>
      </tr>`).join('');
  }

  function upsertMarker(r) {
    if (!map || r.latitude == null || r.longitude == null) return;
    const pos = { lat: r.latitude, lng: r.longitude };
    let m = markers.get(r.reportId);
    if (!m) {
      m = new google.maps.Marker({ position: pos, map, title: r.reportId });
      markers.set(r.reportId, m);
    } else m.setPosition(pos);
  }

  function removeMarker(reportId) { markers.get(reportId)?.setMap(null); markers.delete(reportId); }

  window.initOpsMap = function () {
    const cfg = window.__dashboardConfig || {};
    map = new google.maps.Map(document.getElementById('opsMap'), {
      center: { lat: cfg.defaultLat ?? 9.0765, lng: cfg.defaultLon ?? 7.3986 },
      zoom: cfg.defaultZoom ?? 12,
      mapTypeControl: true, fullscreenControl: true, streetViewControl: false,
    });
    loadActive();
  };

  async function loadAlerts() {
    try {
      const res = await fetch('/api/dashboard/alerts');
      if (!res.ok) return;
      const alerts = await res.json();
      const rail = document.getElementById('alertRail');
      if (!rail) return;
      rail.innerHTML = alerts.length ? alerts.map(a => `
        <div class="card card-pad alert-item sev-${esc(a.severity)}">
          <span class="pill ${a.severity === 'CRITICAL' ? 'pill-critical' : a.severity === 'HIGH' ? 'pill-high' : a.severity === 'WARNING' ? 'pill-warn' : 'pill-info'}">${esc(a.severity)}</span>
          <strong>${esc(a.title)}</strong>
          <p class="text-muted mb-1 small">${esc(a.message)}</p>
          <p class="micro-label mb-1">${new Date(a.createdAt).toLocaleString()}</p>
          <button class="btn btn-sm btn-secondary" data-ack="${a.id}">Acknowledge</button>
        </div>`).join('')
        : `<div class="empty-state">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M12 3l10 18H2zM12 10v5m0 3v.5"/></svg>
          <strong>All clear</strong><span>No active alerts right now.</span></div>`;
      setStat('statAlerts', alerts.length);
      const navCount = document.getElementById('navAlertCount');
      if (navCount) { navCount.hidden = !alerts.length; navCount.textContent = alerts.length; }
      rail.querySelectorAll('[data-ack]').forEach(b => b.addEventListener('click', async () => {
        await fetch(`/api/dashboard/alerts/${b.dataset.ack}/acknowledge`, { method: 'POST' });
        loadAlerts();
      }));
    } catch { /* alerts optional */ }
  }

  document.addEventListener('DOMContentLoaded', async () => {
    document.querySelectorAll('.filter-tabs [data-filter]').forEach(btn => btn.addEventListener('click', () => {
      document.querySelectorAll('.filter-tabs [data-filter]').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      activeFilter = btn.dataset.filter;
      renderTable(lastRows);
    }));
    loadActive();
    loadAlerts();
    setInterval(loadAlerts, 30000);
    setInterval(loadActive, 30000); // backup; SignalR is primary (§8)
    connection = await window.EWMonitoring?.ensureSignalR({
      LocationUpdated: (u) => { upsertMarker({ reportId: u.reportId, latitude: u.latitude, longitude: u.longitude }); loadActive(); },
      ReportStarted: () => loadActive(),
      ReportEnded: (e) => { removeMarker(e.reportId); loadActive(); },
    });
  });
})();
