import React, { useState, useEffect, useCallback } from 'react';
import type { EnergyForecast as EnergyForecastType } from '../types';

import { getApiBase } from '../runtimeApi';

const API_BASE = getApiBase();

function fmtVn(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M';
  if (abs >= 1_000)     return (v / 1_000).toFixed(1) + 'K';
  if (abs >= 1)         return v.toFixed(1);
  return v.toFixed(3);
}

function fmtRate(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(2) + 'M';
  if (abs >= 1_000)     return (v / 1_000).toFixed(1) + 'K';
  return v.toFixed(1);
}

function fmtDur(min: number): string {
  if (min < 1) return '<1m';
  if (min >= 60) return `${Math.floor(min / 60)}h ${Math.round(min % 60)}m`;
  return `${Math.round(min)}m`;
}

interface Props {
  sessionId: string | null;
  totalWindows: number;
}

export const EnergyForecast: React.FC<Props> = ({ sessionId, totalWindows }) => {
  const [forecast, setForecast] = useState<EnergyForecastType | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchForecast = useCallback(async () => {
    try {
      const params = sessionId ? `?sessionId=${sessionId}` : '';
      const res = await fetch(`${API_BASE}/api/dataset/energy-forecast${params}`);
      if (res.ok) {
        setForecast(await res.json());
        setError(null);
      } else {
        setError(`HTTP ${res.status}`);
      }
    } catch {
      setError('API unavailable');
    }
  }, [sessionId]);

  useEffect(() => {
    fetchForecast();
    const id = setInterval(fetchForecast, 30_000);
    return () => clearInterval(id);
  }, [fetchForecast]);

  useEffect(() => {
    if (totalWindows > 0) {
      const t = setTimeout(fetchForecast, 2000);
      return () => clearTimeout(t);
    }
  }, [totalWindows, fetchForecast]);

  const cardStyle: React.CSSProperties = {
    background: '#1a1a2e', borderRadius: 12, padding: 16,
    border: '1px solid #333', fontSize: 12,
  };

  if (!forecast) {
    return (
      <div style={cardStyle}>
        <div style={{ color: '#aaa', fontSize: 13, marginBottom: 8 }}>Session Energy</div>
        <div style={{ color: '#666' }}>{error ?? 'Loading...'}</div>
      </div>
    );
  }

  const sessionDur = forecast.sessionMinutes ?? 0;
  const avgPerWindow = forecast.totalWindows > 0
    ? forecast.energySpentToday / forecast.totalWindows : 0;

  return (
    <div style={cardStyle}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 10 }}>Session Energy</div>

      {/* Session spent */}
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 6, marginBottom: 4 }}>
        <span style={{ color: '#64B5F6', fontSize: 20, fontWeight: 700 }}>
          {fmtVn(forecast.energySpentToday)}
        </span>
        <span style={{ color: '#888', fontSize: 11 }}>Vn in {fmtDur(sessionDur)}</span>
      </div>

      {/* 16h projection – the key insight */}
      <div style={{
        background: '#12122a', borderRadius: 8, padding: '8px 10px', marginBottom: 10,
      }}>
        <div style={{ color: '#666', fontSize: 10, marginBottom: 2 }}>16h day projection at current rate</div>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 6 }}>
          <span style={{ color: '#FFC107', fontSize: 18, fontWeight: 700 }}>
            {fmtVn(forecast.projectedTotal)}
          </span>
          <span style={{ color: '#888', fontSize: 10 }}>
            Vn ({fmtRate(forecast.currentRatePerMin)} Vn/min × 960 min)
          </span>
        </div>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 6, marginBottom: 10 }}>
        {[
          { label: 'Rate', value: `${fmtRate(forecast.currentRatePerMin)}/min` },
          { label: 'Avg/window', value: `${fmtVn(avgPerWindow)}` },
          { label: 'Windows', value: forecast.totalWindows.toLocaleString() },
        ].map(m => (
          <div key={m.label} style={{ textAlign: 'center' }}>
            <div style={{ color: '#555', fontSize: 9 }}>{m.label}</div>
            <div style={{ color: '#ccc', fontSize: 11, fontWeight: 600 }}>{m.value}</div>
          </div>
        ))}
      </div>

      {/* Per-action breakdown */}
      {forecast.perAction.length > 0 && (
        <>
          <div style={{ color: '#888', fontSize: 11, marginBottom: 4, borderTop: '1px solid #333', paddingTop: 8 }}>
            By Activity (this session)
          </div>
          <div style={{ maxHeight: 120, overflowY: 'auto' }}>
            {forecast.perAction.map((a, i) => (
              <div key={i} style={{
                display: 'flex', justifyContent: 'space-between', padding: '3px 0',
                borderBottom: '1px solid #1e1e3e',
              }}>
                <span style={{ color: '#bbb' }}>{a.actionName}</span>
                <span>
                  <span style={{ color: '#64B5F6', fontWeight: 600 }}>{fmtVn(a.spent)}</span>
                  <span style={{ color: '#666', marginLeft: 6 }}>{fmtDur(a.minutes)}</span>
                </span>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
};
