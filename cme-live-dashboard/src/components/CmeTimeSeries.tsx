import React from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import type { CmeResult, CalibrationProgress, WindowClass, ActiveAction } from '../types';

const WINDOW_CLASS_COLORS: Record<WindowClass, string> = {
  clean: '#4CAF50',
  artifact: '#FFC107',
  rejected: '#F44336',
  calibrating: '#64B5F6',
};

interface Props {
  history: CmeResult[];
  calibration?: CalibrationProgress | null;
  currentAction?: ActiveAction | null;
}

export const CmeTimeSeries: React.FC<Props> = ({ history, calibration, currentAction }) => {
  const data = history.map((r, i) => ({
    idx: i,
    time: new Date(r.timestamp).toLocaleTimeString(),
    cme: r.cmeVn,
    pFlow: r.pFlow,
    isFlow: r.isFlow,
    windowClass: r.windowClass ?? 'clean',
    actionName: r.actionName ?? '',
  }));

  const cmeSession = (history.length > 0 ? history[history.length - 1].cmeSessionVn : 0) ?? 0;
  const isCalibrating = calibration && !calibration.isComplete;

  const windowCounts = history.reduce((acc, r) => {
    const cls = r.windowClass ?? 'clean';
    acc[cls] = (acc[cls] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  // Detect action boundaries for reference lines
  const actionBoundaries: { idx: number; name: string }[] = [];
  for (let i = 1; i < data.length; i++) {
    if (data[i].actionName !== data[i - 1].actionName && data[i].actionName) {
      actionBoundaries.push({ idx: i, name: data[i].actionName });
    }
  }

  return (
    <div style={{ background: '#1a1a2e', borderRadius: 12, padding: 16, border: '1px solid #333', position: 'relative' }}>
      {isCalibrating && (
        <div style={{
          position: 'absolute', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(26, 26, 46, 0.85)', borderRadius: 12,
          display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
          zIndex: 10,
        }}>
          <div style={{ color: '#64B5F6', fontSize: 16, fontWeight: 600, marginBottom: 12 }}>
            Calibrating{calibration.actionName ? ` [${calibration.actionName}]` : ''}...
          </div>
          <div style={{
            width: '60%', height: 8, background: '#333', borderRadius: 4, overflow: 'hidden',
          }}>
            <div style={{
              width: `${Math.round((calibration.windowsCollected / calibration.windowsNeeded) * 100)}%`,
              height: '100%', background: '#64B5F6', borderRadius: 4,
              transition: 'width 0.3s ease',
            }} />
          </div>
          <div style={{ color: '#888', fontSize: 12, marginTop: 6 }}>
            {calibration.windowsCollected} / {calibration.windowsNeeded} windows
          </div>
        </div>
      )}

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8, flexWrap: 'wrap', gap: 4 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: '#aaa', fontSize: 13 }}>CME(t) Time Series</span>
          {currentAction && (
            <span style={{
              background: '#2e3a1e', color: '#81C784', fontSize: 11,
              padding: '2px 8px', borderRadius: 10, fontWeight: 500,
            }}>
              {currentAction.name}
            </span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {Object.entries(windowCounts).map(([cls, count]) => (
            <span key={cls} style={{ fontSize: 11, color: WINDOW_CLASS_COLORS[cls as WindowClass] || '#888' }}>
              {cls}: {count}
            </span>
          ))}
          <span style={{ color: '#64B5F6', fontSize: 13 }}>
            CME<sub>session</sub> = <strong>{cmeSession.toFixed(1)}</strong>
          </span>
        </div>
      </div>
      <ResponsiveContainer width="100%" height={200}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" stroke="#333" />
          <XAxis dataKey="time" tick={{ fill: '#888', fontSize: 10 }} interval="preserveStartEnd" />
          <YAxis tick={{ fill: '#888', fontSize: 10 }} />
          <Tooltip
            contentStyle={{ background: '#222', border: '1px solid #555', borderRadius: 8, fontSize: 12 }}
            labelStyle={{ color: '#aaa' }}
            formatter={(value: number, _name: string, props: { payload?: { windowClass?: string; actionName?: string } }) => {
              const cls = props.payload?.windowClass ?? 'clean';
              const action = props.payload?.actionName;
              return [
                <span style={{ color: WINDOW_CLASS_COLORS[cls as WindowClass] || '#888' }}>
                  {value.toFixed(2)} Vn [{cls}]{action ? ` · ${action}` : ''}
                </span>,
                'CME',
              ];
            }}
          />
          {actionBoundaries.map(b => (
            <ReferenceLine
              key={b.idx}
              x={data[b.idx]?.time}
              stroke="#81C784"
              strokeDasharray="4 4"
              strokeOpacity={0.5}
              label={{ value: b.name, fill: '#81C784', fontSize: 9, position: 'insideTopRight' }}
            />
          ))}
          <Line
            type="monotone"
            dataKey="cme"
            stroke="#64B5F6"
            strokeWidth={2}
            dot={(props: { cx: number; cy: number; index: number; payload: { windowClass: string } }) => {
              const cls = props.payload.windowClass as WindowClass;
              const color = WINDOW_CLASS_COLORS[cls] || '#64B5F6';
              if (data.length > 60) return <></>;
              return (
                <circle
                  key={props.index}
                  cx={props.cx}
                  cy={props.cy}
                  r={3}
                  fill={color}
                  stroke={color}
                  strokeWidth={1}
                />
              );
            }}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};
