import React from 'react';
import type { CmeResult } from '../types';

function fmtVn(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M';
  if (abs >= 1_000) return (v / 1_000).toFixed(1) + 'K';
  if (abs >= 1) return v.toFixed(1);
  return v.toFixed(3);
}

interface Props {
  history: CmeResult[];
}

export const SessionMetrics: React.FC<Props> = ({ history }) => {
  const total = history.length;
  const flowWindows = history.filter(r => r.isFlow).length;
  const flowShare = total > 0 ? (flowWindows / total * 100) : 0;
  const cmeSession = total > 0 ? (history[history.length - 1].cmeSessionVn ?? 0) : 0;
  const avgCme = total > 0 ? history.reduce((s, r) => s + (r.cmeVn ?? 0), 0) / total : 0;
  const maxCme = total > 0 ? Math.max(...history.map(r => r.cmeVn ?? 0)) : 0;
  const avgLatency = total > 0 ? Math.round(history.reduce((s, r) => s + r.totalLatencyMs, 0) / total) : 0;
  const lastResult = history[history.length - 1];

  let longestStreak = 0, currentStreak = 0;
  for (const r of history) {
    if (r.isFlow) { currentStreak++; longestStreak = Math.max(longestStreak, currentStreak); }
    else { currentStreak = 0; }
  }

  const metrics = [
    { label: 'CME Session', value: fmtVn(cmeSession), color: '#64B5F6' },
    { label: 'Avg CME/window', value: fmtVn(avgCme), color: '#81C784' },
    { label: 'Max CME', value: fmtVn(maxCme), color: '#FFB74D' },
    { label: 'Flow Share', value: `${flowShare.toFixed(0)}%`, color: flowShare >= 50 ? '#4CAF50' : '#FF9800' },
    { label: 'Flow Windows', value: `${flowWindows}/${total}`, color: '#aaa' },
    { label: 'Longest Streak', value: `${longestStreak}`, color: '#CE93D8' },
    { label: 'Avg Latency', value: `${avgLatency}ms`, color: '#aaa' },
    { label: 'Shots', value: `${lastResult?.shotsUsed ?? '-'}`, color: '#aaa' },
  ];

  return (
    <div style={{ background: '#1a1a2e', borderRadius: 12, padding: 16, border: '1px solid #333' }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 12 }}>Session Metrics</div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
        {metrics.map(m => (
          <div key={m.label} style={{
            background: '#12122a', borderRadius: 8, padding: '6px 10px',
            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          }}>
            <span style={{ color: '#888', fontSize: 10 }}>{m.label}</span>
            <span style={{ color: m.color, fontSize: 13, fontWeight: 'bold' }}>{m.value}</span>
          </div>
        ))}
      </div>
    </div>
  );
};
