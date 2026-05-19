import React from 'react';

export type InferenceMode = 'classical' | 'quantum' | 'hybrid';

interface Props {
  value: InferenceMode;
  onChange: (mode: InferenceMode) => void;
}

export const InferenceModeToggle: React.FC<Props> = ({ value, onChange }) => {
  const options: { mode: InferenceMode; label: string }[] = [
    { mode: 'classical', label: 'Classical' },
    { mode: 'quantum', label: 'Quantum' },
    { mode: 'hybrid', label: 'Hybrid' },
  ];

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 16,
      border: '1px solid #333',
    }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 12 }}>Inference Mode</div>
      <div style={{ display: 'flex', gap: 8 }}>
        {options.map(({ mode, label }) => (
          <button
            key={mode}
            onClick={() => onChange(mode)}
            style={{
              flex: 1, padding: '8px 12px', borderRadius: 6, fontSize: 12,
              background: value === mode ? '#2563eb' : '#333',
              color: value === mode ? '#fff' : '#aaa',
              border: 'none', cursor: 'pointer',
            }}
          >
            {label}
          </button>
        ))}
      </div>
    </div>
  );
};
