import React from 'react';

interface Props {
  status: 'disconnected' | 'connecting' | 'connected';
  sessionId: string | null;
  sessionStartTime: number | null;
  totalWindows: number;
  onStartSession: () => void;
  onStopSession: (sessionId: string | null) => void;
}

export const ConnectionStatus: React.FC<Props> = ({
  status,
  sessionId,
  sessionStartTime,
  totalWindows,
  onStartSession,
  onStopSession,
}) => {
  const isRecording = !!sessionId;
  const elapsed = sessionStartTime ? Math.floor((Date.now() - sessionStartTime) / 1000) : 0;
  const mins = Math.floor(elapsed / 60);
  const secs = elapsed % 60;
  const canControl = status === 'connected';

  const statusColor = status === 'connected' ? '#4CAF50' : status === 'connecting' ? '#FFC107' : '#F44336';
  const statusText = status === 'connected' ? 'Connected' : status === 'connecting' ? 'Connecting...' : 'Disconnected';

  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 20, padding: '10px 20px',
      background: isRecording ? 'rgba(220,38,38,0.12)' : '#1a1a2e',
      borderBottom: isRecording ? '2px solid #dc2626' : '1px solid #333',
      color: '#eee', fontSize: 14,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <div style={{ width: 10, height: 10, borderRadius: '50%', background: statusColor, boxShadow: `0 0 8px ${statusColor}` }} />
        <span>{statusText}</span>
      </div>
      <div style={{ color: '#444' }}>|</div>

      {/* Recording state - prominent */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 12,
        padding: '6px 14px', borderRadius: 8,
        background: isRecording ? 'rgba(220,38,38,0.25)' : 'rgba(100,100,100,0.2)',
        border: `1px solid ${isRecording ? '#dc2626' : '#444'}`,
      }}>
        <span style={{
          fontSize: 13, fontWeight: 'bold',
          color: isRecording ? '#f87171' : '#888',
        }}>
          {isRecording ? '● RECORDING' : '○ Stopped'}
        </span>
        {canControl && (
          isRecording ? (
            <button
              onClick={() => onStopSession(sessionId)}
              style={{
                padding: '4px 12px', borderRadius: 4, background: '#dc2626', color: '#fff',
                border: 'none', cursor: 'pointer', fontSize: 12, fontWeight: 'bold',
              }}
            >
              Stop Session
            </button>
          ) : (
            <button
              onClick={onStartSession}
              style={{
                padding: '4px 12px', borderRadius: 4, background: '#2563eb', color: '#fff',
                border: 'none', cursor: 'pointer', fontSize: 12, fontWeight: 'bold',
              }}
            >
              Start Session
            </button>
          )
        )}
      </div>

      <div style={{ color: '#444' }}>|</div>
      <div>Muse Athena</div>
      <div style={{ color: '#444' }}>|</div>
      <div>Windows: <strong>{totalWindows}</strong></div>
      {isRecording && sessionStartTime && (
        <>
          <div style={{ color: '#444' }}>|</div>
          <div>Session: <strong>{mins}:{secs.toString().padStart(2, '0')}</strong></div>
        </>
      )}
      <div style={{ flex: 1 }} />
      <div style={{ fontSize: 12, color: '#888' }}>CME Live Dashboard v1.0</div>
    </div>
  );
};
