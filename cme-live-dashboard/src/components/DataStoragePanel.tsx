import React, { useEffect, useState } from 'react';
import { api } from '../api';

interface Props {
  sessionId: string | null;
  totalWindows: number;
  onStartSession: () => void;
  onStopSession: (sessionId: string | null) => void;
}

export const DataStoragePanel: React.FC<Props> = ({ sessionId, totalWindows, onStartSession, onStopSession }) => {
  const [storedCount, setStoredCount] = useState<number | null>(null);

  useEffect(() => {
    if (!sessionId) return;
    const fetchCount = async () => {
      try {
        const windows = await api.get<{ length?: number }[]>(`/api/dataset/windows?sessionId=${sessionId}&limit=10000`);
        setStoredCount(Array.isArray(windows) ? windows.length : 0);
      } catch {
        setStoredCount(null);
      }
    };
    fetchCount();
    const interval = setInterval(fetchCount, 5000);
    return () => clearInterval(interval);
  }, [sessionId]);

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 16,
      border: '1px solid #333',
    }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 12 }}>Data Storage</div>
      <div style={{ marginBottom: 12 }}>
        <div style={{ fontSize: 24, fontWeight: 'bold', color: '#64B5F6' }}>
          {storedCount !== null ? storedCount : '–'} windows stored
        </div>
        <div style={{ fontSize: 11, color: '#666', marginTop: 4 }}>
          In-memory: {totalWindows} | DB: {storedCount ?? '…'}
        </div>
      </div>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        <button onClick={onStartSession} style={{
          padding: '8px 16px', borderRadius: 6, background: '#2563eb', color: '#fff',
          border: 'none', cursor: 'pointer', fontSize: 12,
        }}>
          Start New Session
        </button>
        {sessionId && (
          <button onClick={() => onStopSession(sessionId)} style={{
            padding: '8px 16px', borderRadius: 6, background: '#dc2626', color: '#fff',
            border: 'none', cursor: 'pointer', fontSize: 12,
          }}>
            Stop Session
          </button>
        )}
      </div>
    </div>
  );
};
