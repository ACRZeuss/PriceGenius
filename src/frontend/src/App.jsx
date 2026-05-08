import { Routes, Route } from 'react-router-dom';
import { useSignalR } from './hooks/useSignalR';
import Layout from './components/Layout';
import Dashboard from './components/Dashboard';
import ProductsPage from './pages/ProductsPage';
import LogsPage from './pages/LogsPage';

export default function App() {
  const { connected, logs, priceUpdates, decisions, marketAlerts, clearLogs } = useSignalR();

  return (
    <Routes>
      <Route element={<Layout connected={connected} />}>
        <Route path="/" element={
          <Dashboard realtimeDecisions={decisions} realtimePriceUpdates={priceUpdates} />
        } />
        <Route path="/products" element={
          <ProductsPage realtimePriceUpdates={priceUpdates} />
        } />
        <Route path="/logs" element={
          <LogsPage logs={logs} clearLogs={clearLogs} connected={connected} />
        } />
      </Route>
    </Routes>
  );
}
