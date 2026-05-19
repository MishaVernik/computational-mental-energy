import React from 'react';
import type { EegWindowData, BandPowers } from '../types';

interface Props {
  latest: EegWindowData | null;
}

const ELECTRODES = ['TP9', 'AF7', 'AF8', 'TP10'];
const BANDS: (keyof BandPowers)[] = ['delta', 'theta', 'alpha', 'beta', 'gamma'];
const BAND_LABELS = ['δ 1-4Hz', 'θ 4-8Hz', 'α 8-13Hz', 'β 13-30Hz', 'γ 30-45Hz'];

function valueToColor(value: number, maxVal: number): string {
  const t = Math.min(value / (maxVal || 1), 1);
  const r = Math.round(30 + 200 * t);
  const g = Math.round(30 + 100 * (1 - t));
  const b = Math.round(80 + 150 * (1 - t * 0.5));
  return `rgb(${r},${g},${b})`;
}

export const SpectralHeatmap: React.FC<Props> = ({ latest }) => {
  const channels = latest?.channels ?? {};

  // Find max for color scaling
  let maxVal = 0.1;
  for (const ch of Object.values(channels)) {
    for (const band of BANDS) {
      maxVal = Math.max(maxVal, ch[band] ?? 0);
    }
  }

  return (
    <div style={{ background: '#1a1a2e', borderRadius: 12, padding: 16, border: '1px solid #333' }}>
      <div style={{ color: '#aaa', fontSize: 13, marginBottom: 8 }}>Spectral Heatmap (Current Window)</div>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th style={{ color: '#888', fontSize: 11, padding: 6, textAlign: 'left' }}>Electrode</th>
            {BAND_LABELS.map(b => (
              <th key={b} style={{ color: '#888', fontSize: 10, padding: 6, textAlign: 'center' }}>{b}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {ELECTRODES.map(elec => {
            const ch = channels[elec];
            return (
              <tr key={elec}>
                <td style={{ color: '#ccc', fontSize: 12, padding: 6, fontWeight: 'bold' }}>{elec}</td>
                {BANDS.map(band => {
                  const val = ch?.[band] ?? 0;
                  return (
                    <td key={band} style={{
                      padding: 4, textAlign: 'center',
                    }}>
                      <div style={{
                        background: valueToColor(val, maxVal),
                        borderRadius: 6, padding: '6px 4px',
                        color: '#fff', fontSize: 11, fontFamily: 'monospace',
                        transition: 'background 0.3s ease',
                      }}>
                        {val.toFixed(3)}
                      </div>
                    </td>
                  );
                })}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
