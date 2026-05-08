import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export function useSignalR() {
  const [connected, setConnected] = useState(false);
  const [logs, setLogs] = useState([]);
  const [priceUpdates, setPriceUpdates] = useState([]);
  const [decisions, setDecisions] = useState([]);
  const [marketAlerts, setMarketAlerts] = useState([]);
  const connectionRef = useRef(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/pricehub')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('LogMessage', (log) => {
      setLogs(prev => [log, ...prev].slice(0, 200));
    });

    connection.on('PriceUpdated', (update) => {
      setPriceUpdates(prev => [update, ...prev].slice(0, 50));
    });

    connection.on('NewAgentDecision', (decision) => {
      setDecisions(prev => [decision, ...prev].slice(0, 50));
    });

    connection.on('MarketAlert', (alert) => {
      setMarketAlerts(prev => [alert, ...prev].slice(0, 20));
    });

    connection.onreconnecting(() => {
      setConnected(false);
    });

    connection.onreconnected(() => {
      setConnected(true);
    });

    connection.onclose(() => {
      setConnected(false);
    });

    connection.start()
      .then(() => {
        setConnected(true);
        console.log('✅ SignalR connected');
      })
      .catch(err => {
        console.warn('⚠️ SignalR connection failed:', err);
        setConnected(false);
      });

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, []);

  const clearLogs = useCallback(() => {
    setLogs([]);
  }, []);

  return {
    connected,
    logs,
    priceUpdates,
    decisions,
    marketAlerts,
    clearLogs,
  };
}
