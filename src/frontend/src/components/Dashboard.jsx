import { useState, useEffect } from 'react';
import { fetchDashboardSummary, fetchPriceHistory, fetchDecisions } from '../services/api';

function formatPrice(val) {
  return Number(val).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function timeAgo(dateStr) {
  const now = new Date();
  const date = new Date(dateStr);
  const diffMs = now - date;
  const diffMins = Math.floor(diffMs / 60000);
  if (diffMins < 1) return 'az önce';
  if (diffMins < 60) return `${diffMins} dk önce`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours} saat önce`;
  return `${Math.floor(diffHours / 24)} gün önce`;
}

function getStrategyBadge(strategy) {
  const map = {
    opportunistic: { class: 'success', label: '🎯 Fırsat' },
    undercut: { class: 'warning', label: '⚔️ Alt Fiyat' },
    premium: { class: 'info', label: '💎 Premium' },
    match: { class: 'neutral', label: '🤝 Eşle' },
    hold: { class: 'neutral', label: '⏸️ Bekle' },
  };
  const s = map[strategy] || { class: 'neutral', label: strategy };
  return <span className={`badge ${s.class}`}>{s.label}</span>;
}

function getConfidenceColor(score) {
  if (score >= 75) return 'var(--accent-emerald)';
  if (score >= 50) return 'var(--accent-amber)';
  return 'var(--accent-red)';
}

export default function Dashboard({ realtimeDecisions, realtimePriceUpdates }) {
  const [summary, setSummary] = useState(null);
  const [history, setHistory] = useState([]);
  const [decisions, setDecisions] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 15000);
    return () => clearInterval(interval);
  }, []);

  // Merge realtime decisions
  useEffect(() => {
    if (realtimeDecisions.length > 0) {
      setDecisions(prev => {
        const merged = [...realtimeDecisions, ...prev];
        const unique = merged.filter((d, i, arr) => 
          i === arr.findIndex(x => x.id === d.id)
        );
        return unique.slice(0, 20);
      });
    }
  }, [realtimeDecisions]);

  async function loadData() {
    try {
      const [s, h, d] = await Promise.all([
        fetchDashboardSummary(),
        fetchPriceHistory(10),
        fetchDecisions(10),
      ]);
      setSummary(s);
      setHistory(h);
      setDecisions(d);
    } catch (err) {
      console.warn('Dashboard data fetch failed:', err);
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <div className="animate-in" style={{ textAlign: 'center', padding: '60px', color: 'var(--text-muted)' }}>
        <div style={{ fontSize: '2rem', marginBottom: '12px' }}>⏳</div>
        Veriler yükleniyor...
      </div>
    );
  }

  return (
    <div className="animate-in">
      <div className="page-header">
        <h1>Dashboard</h1>
        <p>Otonom fiyatlandırma sisteminin genel durumu</p>
      </div>

      {/* Stats */}
      <div className="stats-grid">
        <div className="stat-card indigo">
          <div className="stat-icon">📦</div>
          <div className="stat-value">{summary?.totalProducts || 0}</div>
          <div className="stat-label">Toplam Ürün</div>
        </div>
        <div className="stat-card emerald">
          <div className="stat-icon">📈</div>
          <div className="stat-value">%{summary?.averageProfitMargin || 0}</div>
          <div className="stat-label">Ort. Kar Marjı</div>
        </div>
        <div className="stat-card amber">
          <div className="stat-icon">🔄</div>
          <div className="stat-value">{summary?.todayPriceChanges || 0}</div>
          <div className="stat-label">Bugünkü Değişiklik</div>
        </div>
        <div className="stat-card cyan">
          <div className="stat-icon">🤖</div>
          <div className="stat-value" style={{ fontSize: '1.4rem' }}>{summary?.agentStatus || 'Pasif'}</div>
          <div className="stat-label">AI Agent Durumu</div>
        </div>
      </div>

      {/* Two column layout */}
      <div className="grid-3">
        {/* Price History */}
        <div className="card animate-in-delayed">
          <div className="card-header">
            <span className="card-title">📉 Son Fiyat Değişiklikleri</span>
          </div>
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Ürün</th>
                  <th>Eski Fiyat</th>
                  <th>Yeni Fiyat</th>
                  <th>Değişim</th>
                  <th>Strateji</th>
                  <th>Zaman</th>
                </tr>
              </thead>
              <tbody>
                {history.length === 0 ? (
                  <tr><td colSpan="6" style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '30px' }}>Henüz fiyat değişikliği yok</td></tr>
                ) : (
                  history.map((h) => {
                    const change = h.oldPrice > 0 ? ((h.newPrice - h.oldPrice) / h.oldPrice * 100).toFixed(1) : 0;
                    const isUp = h.newPrice > h.oldPrice;
                    return (
                      <tr key={h.id}>
                        <td style={{ fontWeight: 600 }}>{h.productName}</td>
                        <td>{formatPrice(h.oldPrice)} ₺</td>
                        <td style={{ fontWeight: 600 }}>{formatPrice(h.newPrice)} ₺</td>
                        <td>
                          <span className={isUp ? 'price-up' : 'price-down'}>
                            {isUp ? '▲' : '▼'} %{Math.abs(change)}
                          </span>
                        </td>
                        <td>{getStrategyBadge(h.strategy)}</td>
                        <td style={{ color: 'var(--text-muted)', fontSize: '0.82rem' }}>{timeAgo(h.changedAt)}</td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* AI Decisions */}
        <div className="card animate-in-delayed">
          <div className="card-header">
            <span className="card-title">🧠 AI Kararları</span>
          </div>
          <div className="decision-list">
            {decisions.length === 0 ? (
              <div style={{ textAlign: 'center', color: 'var(--text-muted)', padding: '30px' }}>
                Henüz AI kararı yok
              </div>
            ) : (
              decisions.slice(0, 8).map((d) => (
                <div className="decision-card" key={d.id}>
                  <div className="decision-header">
                    <span className="decision-product">{d.productName}</span>
                    {getStrategyBadge(d.strategy)}
                  </div>
                  <div className="decision-prices">
                    <span style={{ color: 'var(--text-muted)' }}>Önerilen:</span>
                    <span style={{ fontWeight: 700 }}>{formatPrice(d.suggestedPrice)} ₺</span>
                    <span style={{ color: 'var(--text-muted)' }}>→</span>
                    <span style={{ fontWeight: 700, color: d.wasOverridden ? 'var(--accent-amber)' : 'var(--accent-emerald)' }}>
                      {formatPrice(d.appliedPrice)} ₺
                    </span>
                    {d.wasOverridden && <span className="badge warning" style={{ fontSize: '0.65rem' }}>Düzeltildi</span>}
                  </div>
                  <div className="decision-reasoning">{d.decision}</div>
                  <div style={{ marginTop: '8px', display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.78rem', color: 'var(--text-muted)' }}>
                    <span>Güven: %{d.confidenceScore}</span>
                    <div className="confidence-bar">
                      <div className="fill" style={{ width: `${d.confidenceScore}%`, background: getConfidenceColor(d.confidenceScore) }}></div>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
