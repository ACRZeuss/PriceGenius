import { useRef, useEffect, useState } from 'react';

export default function LogsPage({ logs, clearLogs, connected }) {
  const logBodyRef = useRef(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const [filter, setFilter] = useState('all');

  useEffect(() => {
    if (autoScroll && logBodyRef.current) {
      logBodyRef.current.scrollTop = 0;
    }
  }, [logs, autoScroll]);

  const filteredLogs = filter === 'all' ? logs : logs.filter(l => l.level === filter);

  function formatTime(ts) {
    const d = new Date(ts);
    return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  return (
    <div className="animate-in">
      <div className="page-header">
        <h1>Canlı Loglar</h1>
        <p>Sistem olaylarını gerçek zamanlı izleyin</p>
      </div>

      <div className="log-panel">
        <div className="log-header">
          <div className="log-header-title">
            <span className={`status-dot ${connected ? 'online' : 'offline'}`}></span>
            {connected ? 'Bağlı — Canlı Akış' : 'Bağlantı Bekleniyor...'}
          </div>
          <div style={{ display: 'flex', gap: '8px' }}>
            {['all', 'info', 'success', 'warning', 'error'].map(f => (
              <button key={f} className={`btn btn-ghost ${filter === f ? 'active' : ''}`}
                style={filter === f ? { borderColor: 'var(--accent-indigo)', color: 'var(--accent-indigo-light)' } : {}}
                onClick={() => setFilter(f)}>
                {f === 'all' ? 'Tümü' : f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
            <button className="btn btn-ghost" onClick={() => setAutoScroll(!autoScroll)}>
              {autoScroll ? '⏸ Duraklat' : '▶ Devam'}
            </button>
            <button className="btn btn-ghost" onClick={clearLogs}>🗑 Temizle</button>
          </div>
        </div>
        <div className="log-body" ref={logBodyRef}>
          {filteredLogs.length === 0 ? (
            <div style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '40px' }}>
              {connected ? '📋 Henüz log mesajı yok. Sistem olayları burada görünecek...' : '⏳ Bağlantı bekleniyor...'}
            </div>
          ) : (
            filteredLogs.map((log, i) => (
              <div key={i} className={`log-entry ${log.level}`}>
                <span className="log-time">{formatTime(log.timestamp)}</span>
                <span className="log-level">{log.level}</span>
                <span className="log-source">[{log.source}]</span>
                <span className="log-message">{log.message}</span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
