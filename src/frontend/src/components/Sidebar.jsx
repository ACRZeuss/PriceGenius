import { NavLink } from 'react-router-dom';

export default function Sidebar({ connected }) {
  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <div className="logo-icon">⚡</div>
        <span className="logo-text">PriceGenius</span>
      </div>

      <nav className="sidebar-nav">
        <NavLink to="/" end className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <span className="link-icon">📊</span>
          Dashboard
        </NavLink>
        <NavLink to="/products" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <span className="link-icon">📦</span>
          Ürünler
        </NavLink>
        <NavLink to="/logs" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <span className="link-icon">📋</span>
          Canlı Loglar
        </NavLink>
      </nav>

      <div className="sidebar-status">
        <div className="status-row">
          <span className="status-label">API Durumu</span>
          <span>
            <span className={`status-dot ${connected ? 'online' : 'offline'}`}></span>
            {connected ? 'Çevrimiçi' : 'Çevrimdışı'}
          </span>
        </div>
        <div className="status-row">
          <span className="status-label">AI Agent</span>
          <span>
            <span className={`status-dot ${connected ? 'online' : 'offline'}`}></span>
            {connected ? 'Aktif' : 'Pasif'}
          </span>
        </div>
        <div className="status-row">
          <span className="status-label">RabbitMQ</span>
          <span>
            <span className={`status-dot ${connected ? 'online' : 'offline'}`}></span>
            {connected ? 'Bağlı' : 'Bağlantı Yok'}
          </span>
        </div>
      </div>
    </aside>
  );
}
