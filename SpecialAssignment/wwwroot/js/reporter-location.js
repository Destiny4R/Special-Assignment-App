/* reporter-location.js — §6 GPS tracking: watchPosition + thresholds + offline queue */
(function () {
  'use strict';
  const cfg = window.__reporterConfig || {};
  const MIN_METERS = cfg.minMeters ?? 15;
  const MIN_SECONDS = cfg.minSeconds ?? 8;

  const els = {
    idle: document.getElementById('reporterIdle'),
    active: document.getElementById('reporterActive'),
    shareBtn: document.getElementById('btnShare'),
    stopBtn: document.getElementById('btnStop'),
    agreeBtn: document.getElementById('btnAgree'),
    lastUpdate: document.getElementById('lastUpdate'),
    accuracy: document.getElementById('accuracy'),
    reportId: document.getElementById('reportIdLabel'),
    status: document.getElementById('reporterStatus'),
    queue: document.getElementById('queueNote'),
    error: document.getElementById('reporterError'),
  };

  let watchId = null;
  let reportId = localStorage.getItem('ew_reportId') || null;
  let lastSent = null; // {lat, lon, at}
  let queue = [];      // offline points
  let lastTick = Date.now();
  let highAccuracy = true;
  let consecutiveTimeouts = 0;

  function showError(msg) {
    if (!els.error) return;
    els.error.textContent = msg;
    els.error.hidden = !msg;
  }

  function setActive(on) {
    if (els.idle) els.idle.hidden = on;
    if (els.active) els.active.hidden = !on;
  }

  function setShareState(state) {
    // state: 'live' | 'nogps' — pill shows icon + text, never color alone
    const pill = document.getElementById('reporterStatusPill');
    if (els.status) els.status.textContent = state === 'live' ? 'LIVE' : 'NO GPS';
    if (pill) pill.className = state === 'live' ? 'pill pill-live' : 'pill pill-stale';
  }

  function haversine(a, b, c, d) {
    const R = 6371000, t = Math.PI / 180;
    const s1 = Math.sin((c - a) / 2 * t), s2 = Math.sin((d - b) / 2 * t);
    const h = s1 * s1 + Math.cos(a * t) * Math.cos(c * t) * s2 * s2;
    return 2 * R * Math.asin(Math.min(1, Math.sqrt(h)));
  }

  async function api(path, body) {
    const res = await fetch(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || `Request failed (${res.status})`);
    return data;
  }

  async function startSession() {
    const termsVersion = cfg.termsVersion || 'v1.0';
    const data = await api('/api/report/start', { consentAccepted: true, termsVersion });
    reportId = data.reportId;
    localStorage.setItem('ew_reportId', reportId);
    if (els.reportId) els.reportId.textContent = reportId;
    return data;
  }

  async function sendPoint(payload, retry = true) {
    try {
      await api(`/api/report/${encodeURIComponent(reportId)}/location`, payload);
      if (els.queue) els.queue.hidden = true;
      // flush backlog
      while (queue.length) {
        const q = queue.shift();
        try { await api(`/api/report/${encodeURIComponent(reportId)}/location`, q); }
        catch { queue.unshift(q); break; }
      }
      updateQueueNote();
    } catch (err) {
      if (retry && !navigator.onLine) { queue.push(payload); updateQueueNote(); return; }
      if (!navigator.onLine) { queue.push(payload); updateQueueNote(); return; }
      queue.push(payload); updateQueueNote();
    }
  }

  function updateQueueNote() {
    if (!els.queue) return;
    if (queue.length) {
      els.queue.hidden = false;
      els.queue.textContent = `Connection interrupted — ${queue.length} point(s) queued. Retrying automatically.`;
    } else els.queue.hidden = true;
  }

  function handlePosition(pos) {
    const { latitude, longitude, accuracy, altitude, speed, heading } = pos.coords;
    const now = Date.now();

    if (lastSent) {
      const dist = haversine(lastSent.lat, lastSent.lon, latitude, longitude);
      const elapsed = (now - lastSent.at) / 1000;
      if (dist < MIN_METERS && elapsed < MIN_SECONDS) return; // avoid redundant points (§6)
    }
    lastSent = { lat: latitude, lon: longitude, at: now };
    consecutiveTimeouts = 0;
    showError(''); // a fix clears any earlier timeout/queue warning tone
    setShareState('live');

    if (els.lastUpdate) els.lastUpdate.textContent = 'just now';
    if (els.accuracy) els.accuracy.textContent = accuracy != null ? `${Math.round(accuracy)} metres` : '—';
    lastTick = now;

    sendPoint({
      latitude, longitude, accuracy, altitude, speed, heading,
      capturedAt: new Date(pos.timestamp).toISOString(),
    });
  }

  function handleLocationError(err) {
    if (err.code === err.PERMISSION_DENIED) {
      setShareState('nogps');
      showError('Location access is required to share your live location. Please enable location access in your browser/device settings.');
    } else if (err.code === err.POSITION_UNAVAILABLE) {
      setShareState('nogps');
      showError('We cannot determine your current location. Please move to an area with better GPS reception.');
    } else if (err.code === err.TIMEOUT) {
      // Transient: the watch keeps running. A cold GPS fix can take 30–60s,
      // so say so instead of alarming, and fall back to coarse fixes.
      consecutiveTimeouts++;
      if (consecutiveTimeouts >= 2 && highAccuracy) {
        highAccuracy = false;
        restartWatch();
        showError('High-accuracy GPS is taking too long — switched to approximate location. Keep this page open; accuracy improves outdoors.');
      } else {
        showError('Waiting for GPS signal — this can take up to a minute, especially indoors. Keep this page open; your session stays active.');
      }
    } else {
      setShareState('nogps');
      showError('Location error. Retrying…');
    }
  }

  function startWatch() {
    if (watchId !== null) navigator.geolocation.clearWatch(watchId);
    watchId = navigator.geolocation.watchPosition(handlePosition, handleLocationError, {
      enableHighAccuracy: highAccuracy, maximumAge: 10000, timeout: 30000,
    });
  }

  function restartWatch() {
    try { startWatch(); } catch { /* keep the existing watch */ }
  }

  async function beginSharing() {
    showError('');
    if (!('geolocation' in navigator)) {
      showError('Geolocation is not supported by this browser.');
      return;
    }
    try {
      if (!reportId) await startSession();
      else if (els.reportId) els.reportId.textContent = reportId;
      setActive(true);
      if (els.status) els.status.textContent = 'WAIT GPS';
      const pill0 = document.getElementById('reporterStatusPill');
      if (pill0) pill0.className = 'pill pill-stale';
      startWatch();
    } catch (err) { showError(err.message); }
  }

  async function stopSharing() {
    if (watchId !== null) navigator.geolocation.clearWatch(watchId);
    watchId = null;
    try { if (reportId) await api(`/api/report/${encodeURIComponent(reportId)}/stop`); } catch { /* offline — session stays, will go stale */ }
    localStorage.removeItem('ew_reportId');
    reportId = null; lastSent = null;
    setActive(false);
    showError('Location sharing has ended. Your live location is no longer being transmitted.');
  }

  // "x seconds ago" ticker
  setInterval(() => {
    if (!els.lastUpdate || els.active?.hidden) return;
    const s = Math.round((Date.now() - lastTick) / 1000);
    els.lastUpdate.textContent = s < 10 ? 'just now' : `${s} seconds ago`;
  }, 2000);

  window.addEventListener('online', () => { updateQueueNote(); });
  els.agreeBtn?.addEventListener('click', async () => {
    const modal = bootstrap.Modal.getInstance(document.getElementById('consentModal'));
    modal?.hide();
    await beginSharing();
  });
  els.stopBtn?.addEventListener('click', stopSharing);

  // Resume active session after reload
  if (reportId) { setActive(true); if (els.reportId) els.reportId.textContent = reportId; beginSharing(); }
})();
