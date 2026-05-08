import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';

export default function Layout({ connected }) {
  return (
    <div className="app-layout">
      <Sidebar connected={connected} />
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
