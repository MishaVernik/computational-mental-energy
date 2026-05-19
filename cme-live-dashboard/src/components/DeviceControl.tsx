import React, { useState } from 'react';

interface Props {
  hubStatus: 'disconnected' | 'connecting' | 'connected';
  isReceiving: boolean;
  totalWindows: number;
  onReset: () => void;
  sessionId?: string | null;
  onStopSession?: (sessionId: string | null) => void;
}

export const DeviceControl: React.FC<Props> = ({ hubStatus, isReceiving, totalWindows, onReset, sessionId, onStopSession }) => {
  const [oscPort] = useState(7002);
  const [difficulty, setDifficulty] = useState(0.5);

  const deviceConnected = hubStatus === 'connected' && isReceiving;

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 16,
      border: deviceConnected ? '2px solid #4CAF50' : '2px solid #333',
      transition: 'border-color 0.3s',
    }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 12 }}>Device Control</div>

      {/* Connection Status */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16,
        padding: '10px 14px', borderRadius: 8,
        background: deviceConnected ? 'rgba(76,175,80,0.1)' : 'rgba(244,67,54,0.1)',
      }}>
        <div style={{
          width: 14, height: 14, borderRadius: '50%',
          background: deviceConnected ? '#4CAF50' : isReceiving ? '#FFC107' : '#F44336',
          boxShadow: deviceConnected ? '0 0 10px rgba(76,175,80,0.5)' : 'none',
          animation: isReceiving ? 'pulse 1.5s infinite' : 'none',
        }} />
        <div>
          <div style={{ color: '#eee', fontSize: 14, fontWeight: 'bold' }}>
            {deviceConnected ? 'Muse Athena Connected' :
             hubStatus === 'connected' ? 'Waiting for MindMonitor...' :
             hubStatus === 'connecting' ? 'Connecting to Hub...' :
             'Disconnected'}
          </div>
          <div style={{ color: '#888', fontSize: 11 }}>
            {deviceConnected ? `Streaming via MindMonitor OSC :${oscPort}` :
             'Start MindMonitor OSC streaming to connect'}
          </div>
        </div>
      </div>

      {/* Instructions (when not connected) */}
      {!deviceConnected && (
        <div style={{
          background: '#12122a', borderRadius: 8, padding: 12, marginBottom: 12,
          fontSize: 12, color: '#aaa', lineHeight: 1.8,
        }}>
          <div style={{ color: '#64B5F6', fontWeight: 'bold', marginBottom: 4 }}>Setup Instructions:</div>
          <div>1. Open <strong>MindMonitor</strong> on your phone</div>
          <div>2. Connect to <strong>Muse Athena</strong></div>
          <div>3. Go to Settings → <strong>OSC Stream</strong></div>
          <div>4. Set IP to this computer's address</div>
          <div>5. Set Port to <strong>{oscPort}</strong></div>
          <div>6. Enable streaming</div>
          <div style={{ marginTop: 8, padding: '6px 10px', background: '#1a2a1a', borderRadius: 6, color: '#81C784' }}>
            Then run: <code>python bridge.py --osc</code>
          </div>
        </div>
      )}

      {/* Task Difficulty Slider */}
      <div style={{ marginBottom: 12 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
          <span style={{ color: '#888', fontSize: 11 }}>Task Difficulty</span>
          <span style={{ color: '#64B5F6', fontSize: 12, fontWeight: 'bold' }}>{difficulty.toFixed(2)}</span>
        </div>
        <input type="range" min="0" max="1" step="0.05" value={difficulty}
          onChange={e => setDifficulty(parseFloat(e.target.value))}
          style={{ width: '100%', accentColor: '#64B5F6' }}
        />
        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 10, color: '#666' }}>
          <span>Easy</span><span>Medium</span><span>Hard</span>
        </div>
      </div>

      {/* Session Controls */}
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        <button onClick={onReset} style={{
          flex: 1, minWidth: 100, padding: '8px 12px', borderRadius: 6,
          background: '#333', color: '#ccc', border: 'none', cursor: 'pointer',
          fontSize: 12, transition: 'background 0.2s',
        }}
        onMouseEnter={e => (e.target as HTMLElement).style.background = '#444'}
        onMouseLeave={e => (e.target as HTMLElement).style.background = '#333'}
        >
          Reset Session
        </button>
        {sessionId && onStopSession && (
          <button onClick={() => onStopSession(sessionId)} style={{
            padding: '8px 16px', borderRadius: 6, background: '#dc2626', color: '#fff',
            border: 'none', cursor: 'pointer', fontSize: 12,
          }}>
            Stop Session
          </button>
        )}
      </div>

      {/* Pulse animation */}
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  );
};
