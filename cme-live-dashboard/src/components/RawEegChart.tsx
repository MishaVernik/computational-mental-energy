import React from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import type { EegWindowData } from '../types';

interface Props {
  history: EegWindowData[];
}

const BAND_COLORS: Record<string, string> = {
  delta: '#F44336',
  theta: '#FF9800',
  alpha: '#4CAF50',
  beta: '#2196F3',
  gamma: '#9C27B0',
};

const ELECTRODES = ['TP9', 'AF7', 'AF8', 'TP10'];

export const RawEegChart: React.FC<Props> = ({ history }) => {
  // Build data for each electrode
  const charts = ELECTRODES.map(electrode => {
    const data = history.map((w, i) => {
      const ch = w.channels?.[electrode];
      return {
        idx: i,
        delta: ch?.delta ?? 0,
        theta: ch?.theta ?? 0,
        alpha: ch?.alpha ?? 0,
        beta: ch?.beta ?? 0,
        gamma: ch?.gamma ?? 0,
      };
    });
    return { electrode, data };
  });

  return (
    <div style={{ background: '#1a1a2e', borderRadius: 12, padding: 16, border: '1px solid #333' }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 8 }}>EEG Band Powers by Electrode</div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
        {charts.map(({ electrode, data }) => (
          <div key={electrode}>
            <div style={{ color: '#ccc', fontSize: 11, marginBottom: 4, textAlign: 'center' }}>{electrode}</div>
            <ResponsiveContainer width="100%" height={120}>
              <LineChart data={data}>
                <CartesianGrid strokeDasharray="2 2" stroke="#2a2a3e" />
                <XAxis hide />
                <YAxis tick={{ fill: '#666', fontSize: 9 }} width={35} />
                <Tooltip
                  contentStyle={{ background: '#222', border: '1px solid #555', borderRadius: 6, fontSize: 11 }}
                />
                {Object.entries(BAND_COLORS).map(([band, color]) => (
                  <Line key={band} type="monotone" dataKey={band} stroke={color}
                        strokeWidth={1.5} dot={false} isAnimationActive={false} />
                ))}
              </LineChart>
            </ResponsiveContainer>
          </div>
        ))}
      </div>
      <div style={{ display: 'flex', justifyContent: 'center', gap: 16, marginTop: 8 }}>
        {Object.entries(BAND_COLORS).map(([band, color]) => (
          <div key={band} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 11, color: '#aaa' }}>
            <div style={{ width: 12, height: 3, background: color, borderRadius: 2 }} />
            {band}
          </div>
        ))}
      </div>
    </div>
  );
};
