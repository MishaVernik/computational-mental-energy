import React from 'react';
import type { CmeResult } from '../types';

interface Props {
  latest: CmeResult | null;
}

export const FlowStateGauge: React.FC<Props> = ({ latest }) => {
  const pFlow = latest?.pFlow ?? 0;
  const cme = latest?.cmeVn ?? 0;
  const isFlow = latest?.isFlow ?? false;
  const pct = Math.round(pFlow * 100);

  // SVG arc gauge
  const radius = 70;
  const circumference = Math.PI * radius;  // half circle
  const offset = circumference * (1 - pFlow);

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 20,
      display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8,
      border: isFlow ? '2px solid #4CAF50' : '2px solid #333',
      boxShadow: isFlow ? '0 0 30px rgba(76,175,80,0.3)' : 'none',
      transition: 'all 0.3s ease',
    }}>
      <div style={{ fontSize: 13, color: '#aaa', letterSpacing: 1 }}>FLOW STATE</div>

      <svg width="160" height="100" viewBox="0 0 160 100">
        {/* Background arc */}
        <path d="M 10 90 A 70 70 0 0 1 150 90" fill="none" stroke="#333" strokeWidth="10" strokeLinecap="round" />
        {/* Value arc */}
        <path d="M 10 90 A 70 70 0 0 1 150 90" fill="none"
          stroke={isFlow ? '#4CAF50' : pFlow > 0.5 ? '#FFC107' : '#666'}
          strokeWidth="10" strokeLinecap="round"
          strokeDasharray={`${circumference}`}
          strokeDashoffset={offset}
          style={{ transition: 'stroke-dashoffset 0.5s ease, stroke 0.3s ease' }}
        />
        {/* Threshold marker at 85% */}
        <line x1="27" y1="35" x2="33" y2="42" stroke="#FF5722" strokeWidth="2" opacity="0.7" />
        <text x="80" y="75" textAnchor="middle" fill="#eee" fontSize="28" fontWeight="bold">
          {pct}%
        </text>
        <text x="80" y="95" textAnchor="middle" fill="#888" fontSize="11">
          p_focus(t)
        </text>
      </svg>

      <div style={{
        fontSize: 28, fontWeight: 'bold', letterSpacing: 2,
        color: isFlow ? '#4CAF50' : '#666',
        textShadow: isFlow ? '0 0 10px rgba(76,175,80,0.5)' : 'none',
      }}>
        {isFlow ? 'FLOW' : 'NOT FLOW'}
      </div>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, fontSize: 13, color: '#ccc' }}>
        <div>CME: <strong style={{ color: '#64B5F6', fontSize: 16 }}>{cme.toFixed(1)}</strong></div>
        <div>Threshold: <span style={{ color: '#FF5722' }}>85%</span></div>
        {latest?.classicalPFlow != null && (
          <div>Classical: <strong style={{ color: '#FFC107' }}>{Math.round(latest.classicalPFlow * 100)}%</strong></div>
        )}
      </div>
    </div>
  );
};
