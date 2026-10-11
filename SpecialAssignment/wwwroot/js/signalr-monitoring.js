/* signalr-monitoring.js — shared SignalR bootstrap for dashboard clients */
(function () {
  'use strict';
  async function ensureSignalR(onEvent) {
    if (typeof signalR === 'undefined') {
      console.warn('SignalR client not loaded.');
      return null;
    }
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/monitoring')
      .withAutomaticReconnect()
      .build();

    for (const [event, handler] of Object.entries(onEvent || {}))
      connection.on(event, handler);

    try {
      await connection.start();
      await connection.invoke('JoinDashboard');
    } catch (err) { console.error('SignalR failed:', err); }
    return connection;
  }
  window.EWMonitoring = { ensureSignalR };
})();
