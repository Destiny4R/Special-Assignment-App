/* live-map.js — single-report Google Maps tracking: marker + trail + accuracy (§9, §11, §14) */
(function () {
  'use strict';
  // NOTE: window.__trackConfig is assigned by an inline script AFTER this file
  // loads, so it must be read lazily inside initTrackMap — never at eval time.
  let cfg = {};
  let reportId = null;
  let map, marker, trail, accuracyCircle;
  let trailPath = [];
  let hasFix = false;

  // Surfaced when the Google Maps key is invalid/restricted — otherwise the map is silently blank.
  window.gm_authFailure = function () {
    const el = document.getElementById('trackMapNotice');
    if (el) {
      el.hidden = false;
      el.textContent = 'Google Maps failed to load (invalid or restricted API key). Check the GoogleMaps:ApiKey setting.';
    }
  };

  function setNotice(msg) {
    const el = document.getElementById('trackMapNotice');
    if (!el) return;
    if (!msg) { el.hidden = true; el.textContent = ''; return; }
    el.hidden = false;
    el.textContent = msg;
  }

  function validLoc(loc) {
    return loc && Number.isFinite(loc.latitude) && Number.isFinite(loc.longitude) &&
      loc.latitude >= -90 && loc.latitude <= 90 && loc.longitude >= -180 && loc.longitude <= 180;
  }

  function updatePanel(loc) {
    const set = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
    set('tLat', Number.isFinite(loc.latitude) ? loc.latitude.toFixed(6) : '—');
    set('tLon', Number.isFinite(loc.longitude) ? loc.longitude.toFixed(6) : '—');
    set('tAcc', loc.accuracy != null ? `${Math.round(loc.accuracy)} m` : '—');
    set('tSpeed', loc.speed != null ? `${(loc.speed * 3.6).toFixed(1)} km/h` : '—');
    set('tTime', loc.capturedAt ? new Date(loc.capturedAt).toLocaleTimeString() : '—');
  }

  function applyLocation(loc) {
    if (!validLoc(loc)) return;
    updatePanel(loc); // panel works even if the map tiles/API key fail
    hasFix = true; setNotice(null); // a fresh fix overrides any stale/waiting notice
    if (!map) return;
    const pos = { lat: loc.latitude, lng: loc.longitude };
    if (!marker) {
      marker = new google.maps.Marker({ position: pos, map, title: reportId });
      map.panTo(pos);
    } else { marker.setPosition(pos); if (cfg.follow !== false) map.panTo(pos); }
    if (loc.accuracy != null && Number.isFinite(loc.accuracy)) {
      if (!accuracyCircle) accuracyCircle = new google.maps.Circle({ map, center: pos, radius: loc.accuracy, fillOpacity: 0.12, strokeOpacity: 0.4 });
      else { accuracyCircle.setCenter(pos); accuracyCircle.setRadius(loc.accuracy); }
    }
    trailPath.push(pos);
    if (!trail) trail = new google.maps.Polyline({ path: trailPath, map, strokeWeight: 3 });
    else trail.setPath(trailPath);
  }

  function setTrackStatus(status) {
    const el = document.getElementById('trackStatus');
    if (!el) return;
    const cls = { LIVE: 'pill pill-live', STALE: 'pill pill-stale', OFFLINE: 'pill pill-offline', ENDED: 'pill pill-ended' }[status] || 'pill pill-offline';
    el.className = cls;
    el.replaceChildren(document.createTextNode(status));
    if (status === 'STALE') setNotice('Signal weak — showing last known position. The marker resumes when fresh coordinates arrive.');
    else if (status === 'OFFLINE') setNotice('Reporter offline — showing last known position.');
    else if (status === 'ENDED') setNotice('This session has ended. Trail below is the recorded history.');
  }

  async function syncSessionStatus() {
    try {
      const res = await fetch('/api/dashboard/reports/active');
      if (!res.ok) return;
      const rows = await res.json();
      const hit = rows.find(r => r.reportId === reportId);
      if (hit) setTrackStatus(hit.status);
      else setTrackStatus('ENDED'); // gone from active list = ended
    } catch { /* pill keeps default */ }
  }

  async function loadInitial() {
    // Reset drawing state: loadInitial runs once on page ready (panel data)
    // and again when the map initializes, so stale polylines must not duplicate.
    trailPath = [];
    if (trail) { trail.setMap(null); trail = null; }
    // 1) Movement trail (history)
    try {
      const res = await fetch(`/api/dashboard/reports/${encodeURIComponent(reportId)}/locations?take=200`);
      if (res.ok) {
        const history = await res.json();
        history.filter(validLoc).forEach(applyLocation);
      }
    } catch { /* fall through to latest */ }
    // 2) Fallback: latest known fix (covers sessions whose points were movement-thresholded)
    if (!hasFix) {
      try {
        const res = await fetch(`/api/dashboard/reports/${encodeURIComponent(reportId)}/location/latest`);
        if (res.ok) applyLocation(await res.json());
      } catch { /* still nothing — waiting notice below */ }
    }
    if (!hasFix) setNotice('Waiting for GPS fix — the marker appears here as soon as the reporter’s first coordinate arrives.');
    // 3) Real session status LAST so STALE/OFFLINE/ENDED context wins over the generic notice.
    await syncSessionStatus();
  }

  let liveStarted = false;
  async function ensureLive() {
    if (liveStarted) return;
    liveStarted = true;
    await window.EWMonitoring?.ensureSignalR({
      LocationUpdated: (u) => { if (u.reportId === reportId) applyLocation(u); },
      ReportEnded: (e) => { if (e.reportId === reportId) setTrackStatus('ENDED'); },
    });
  }

  function readConfig() {
    cfg = window.__trackConfig || {};
    reportId = cfg.reportId;
  }

  window.initTrackMap = async function () {
    readConfig();
    if (!reportId) { setNotice('No report selected.'); return; }
    map = new google.maps.Map(document.getElementById('trackMap'), {
      center: { lat: cfg.defaultLat ?? 9.0765, lng: cfg.defaultLon ?? 7.3986 },
      zoom: 15, fullscreenControl: true, streetViewControl: false,
    });
    setNotice('Loading reporter position…');
    await loadInitial(); // redraws trail now that the map exists
    await ensureLive();

    document.getElementById('btnCenter')?.addEventListener('click', () => { if (marker) map.panTo(marker.getPosition()); });
    document.getElementById('btnStopWatch')?.addEventListener('click', () => { window.location = '/Dashboard'; });
  };

  // Panel + status + live updates start with the page — independent of Google Maps,
  // so telemetry still works if the Maps script/key fails.
  document.addEventListener('DOMContentLoaded', async () => {
    readConfig();
    if (!reportId) return;
    await loadInitial();
    await ensureLive();
  });
})();
