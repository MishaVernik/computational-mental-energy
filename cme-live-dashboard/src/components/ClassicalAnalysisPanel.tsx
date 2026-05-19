import React, { useEffect, useState, useMemo } from 'react';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell, LineChart, Line, CartesianGrid, Legend } from 'recharts';
import { api, type SessionDto, type LabelStatsDto } from '../api';

const BAND_NAMES = [
  'Δ TP9', 'θ TP9', 'α TP9', 'β TP9', 'γ TP9',
  'Δ AF7', 'θ AF7', 'α AF7', 'β AF7', 'γ AF7',
  'Δ AF8', 'θ AF8', 'α AF8', 'β AF8', 'γ AF8',
  'Δ TP10', 'θ TP10', 'α TP10', 'β TP10', 'γ TP10',
];

interface LabeledWindow {
  id: string;
  windowId: string;
  timestamp: string;
  flowLabel: boolean | null;
  flowProbability: number | null;
  features?: number[];
  taskDifficulty?: number;
  quality?: number;
}

const LabeledWindowsTable: React.FC<{ sessionId: string | null }> = ({ sessionId }) => {
  const [windows, setWindows] = useState<LabeledWindow[]>([]);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    if (!expanded) return;
    const url = sessionId
      ? `/api/dataset/windows?sessionId=${sessionId}&labeled=true&limit=500`
      : '/api/dataset/windows?labeled=true&limit=500';
    api.get<LabeledWindow[]>(url).then(setWindows).catch(() => setWindows([]));
  }, [sessionId, expanded]);

  return (
    <div style={{ border: '1px solid #333', borderRadius: 8, overflow: 'hidden' }}>
      <button
        onClick={() => setExpanded(!expanded)}
        style={{
          width: '100%', padding: '10px 14px', background: '#12122a', color: '#aaa', border: 'none',
          cursor: 'pointer', fontSize: 12, textAlign: 'left', display: 'flex', justifyContent: 'space-between',
        }}
      >
        <span>Where are the labels? View labeled windows (stored in cme.EegWindowFeatures)</span>
        <span>{expanded ? '▼' : '▶'}</span>
      </button>
      {expanded && (
        <div style={{ maxHeight: 240, overflow: 'auto', fontSize: 11 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#1a1a2e' }}>
                <th style={{ padding: '6px 8px', textAlign: 'left', color: '#888' }}>Window</th>
                <th style={{ padding: '6px 8px', textAlign: 'left', color: '#888' }}>Time</th>
                <th style={{ padding: '6px 8px', textAlign: 'left', color: '#888' }}>Flow</th>
                <th style={{ padding: '6px 8px', textAlign: 'left', color: '#888' }}>Prob</th>
              </tr>
            </thead>
            <tbody>
              {windows.map((w) => (
                <tr key={w.id} style={{ borderTop: '1px solid #333' }}>
                  <td style={{ padding: '4px 8px', color: '#ccc' }}>{w.windowId}</td>
                  <td style={{ padding: '4px 8px', color: '#ccc' }}>{new Date(w.timestamp).toLocaleTimeString()}</td>
                  <td style={{ padding: '4px 8px', color: w.flowLabel === true ? '#4CAF50' : w.flowLabel === false ? '#F44336' : '#666' }}>
                    {w.flowLabel === true ? 'Flow' : w.flowLabel === false ? 'Not Flow' : '–'}
                  </td>
                  <td style={{ padding: '4px 8px', color: '#888' }}>{w.flowProbability != null ? (w.flowProbability * 100).toFixed(1) + '%' : '–'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {windows.length === 0 && <div style={{ padding: 12, color: '#666' }}>Loading…</div>}
        </div>
      )}
    </div>
  );
};

const DeepDiveChart: React.FC<{ sessionId: string | null }> = ({ sessionId }) => {
  const [windows, setWindows] = useState<LabeledWindow[]>([]);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    if (!expanded) return;
    const url = sessionId
      ? `/api/dataset/windows?sessionId=${sessionId}&labeled=true&limit=500`
      : '/api/dataset/windows?labeled=true&limit=500';
    api.get<LabeledWindow[]>(url).then(setWindows).catch(() => setWindows([]));
  }, [sessionId, expanded]);

  const { bandComparison, probOverTime } = useMemo(() => {
    const flow = windows.filter(w => w.flowLabel === true && w.features?.length === 20);
    const notFlow = windows.filter(w => w.flowLabel === false && w.features?.length === 20);

    const avg = (arr: LabeledWindow[], idx: number) => {
      if (arr.length === 0) return 0;
      return arr.reduce((s, w) => s + (w.features![idx] ?? 0), 0) / arr.length;
    };

    const bandComparison = BAND_NAMES.map((name, i) => ({
      band: name,
      flow: avg(flow, i),
      notFlow: avg(notFlow, i),
    }));

    const probOverTime = windows
      .filter(w => w.flowProbability != null)
      .map(w => ({
        time: new Date(w.timestamp).toLocaleTimeString(),
        ts: new Date(w.timestamp).getTime(),
        prob: (w.flowProbability ?? 0) * 100,
        flow: w.flowLabel === true ? 'Flow' : w.flowLabel === false ? 'Not' : '–',
      }))
      .sort((a, b) => a.ts - b.ts);

    return { bandComparison, probOverTime };
  }, [windows]);

  return (
    <div style={{ border: '1px solid #333', borderRadius: 8, overflow: 'hidden' }}>
      <button
        onClick={() => setExpanded(!expanded)}
        style={{
          width: '100%', padding: '10px 14px', background: '#12122a', color: '#aaa', border: 'none',
          cursor: 'pointer', fontSize: 12, textAlign: 'left', display: 'flex', justifyContent: 'space-between',
        }}
      >
        <span>Deep dive: band powers for Flow vs Not Flow</span>
        <span>{expanded ? '▼' : '▶'}</span>
      </button>
      {expanded && (
        <div style={{ padding: 12, display: 'flex', flexDirection: 'column', gap: 16 }}>
          {bandComparison.length > 0 ? (
            <>
              <div>
                <div style={{ fontSize: 11, color: '#888', marginBottom: 6 }}>Avg band power: Flow (green) vs Not Flow (red)</div>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart data={bandComparison} layout="vertical" margin={{ left: 0, right: 20 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#333" />
                    <XAxis type="number" stroke="#666" fontSize={10} />
                    <YAxis type="category" dataKey="band" stroke="#666" fontSize={10} width={52} />
                    <Tooltip contentStyle={{ background: '#1a1a2e', border: '1px solid #333' }} />
                    <Legend />
                    <Bar dataKey="flow" name="Flow" fill="#4CAF50" radius={[0, 2, 2, 0]} />
                    <Bar dataKey="notFlow" name="Not Flow" fill="#F44336" radius={[0, 2, 2, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
              {probOverTime.length > 0 && (
                <div>
                  <div style={{ fontSize: 11, color: '#888', marginBottom: 6 }}>Flow probability over time</div>
                  <ResponsiveContainer width="100%" height={140}>
                    <LineChart data={probOverTime} margin={{ left: 0, right: 10 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#333" />
                      <XAxis dataKey="time" stroke="#666" fontSize={9} />
                      <YAxis stroke="#666" fontSize={10} domain={[0, 100]} tickFormatter={v => v + '%'} />
                      <Tooltip contentStyle={{ background: '#1a1a2e', border: '1px solid #333' }} formatter={(v: number) => [v.toFixed(1) + '%', 'Prob']} />
                      <Line type="monotone" dataKey="prob" stroke="#64B5F6" dot={false} strokeWidth={2} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              )}
            </>
          ) : (
            <div style={{ padding: 12, color: '#666', fontSize: 12 }}>Loading…</div>
          )}
        </div>
      )}
    </div>
  );
};

interface Props {
  onStopSession?: (sessionId: string | null) => void;
}

export const ClassicalAnalysisPanel: React.FC<Props> = ({ onStopSession }) => {
  const [sessions, setSessions] = useState<SessionDto[]>([]);
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);
  const [labelStats, setLabelStats] = useState<LabelStatsDto | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [result, setResult] = useState<{ analyzed: number; labeled: number } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchSessions = async () => {
    try {
      const list = await api.get<SessionDto[]>('/api/sessions');
      setSessions(Array.isArray(list) ? list : []);
      if (list?.length && !selectedSessionId) setSelectedSessionId(list[0]?.id ?? null);
    } catch {
      setSessions([]);
    }
  };

  const fetchLabelStats = async () => {
    try {
      const url = selectedSessionId
        ? `/api/dataset/label-stats?sessionId=${selectedSessionId}`
        : '/api/dataset/label-stats';
      const stats = await api.get<LabelStatsDto>(url);
      setLabelStats(stats);
    } catch {
      setLabelStats(null);
    }
  };

  useEffect(() => { fetchSessions(); }, []);
  useEffect(() => { fetchLabelStats(); }, [selectedSessionId]);

  const runAnalysis = async () => {
    setAnalyzing(true);
    setResult(null);
    setError(null);
    try {
      const path = selectedSessionId
        ? `/api/dataset/analyze-classical?sessionId=${selectedSessionId}`
        : '/api/dataset/analyze-classical';
      const res = await api.post(path) as { analyzed: number; labeled: number };
      setResult(res);
      if (res.analyzed > 0 && res.labeled === 0) {
        setError('Classifier returned 0 labels. Is flow-classifier running on port 8002?');
      }
      await fetchLabelStats();
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Analysis failed';
      const hint = /fetch|network|failed/i.test(msg) ? ' Is CmeSim.Api running on port 5000?' : '';
      setError(msg + hint);
      console.error(e);
    } finally {
      setAnalyzing(false);
    }
  };

  const chartData = labelStats
    ? [
        { name: 'Flow', count: labelStats.flowCount, color: '#4CAF50' },
        { name: 'Not Flow', count: labelStats.notFlowCount, color: '#F44336' },
        { name: 'Unlabeled', count: labelStats.unlabeledCount, color: '#666' },
      ].filter(d => d.count > 0)
    : [];

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 16,
      border: '1px solid #333', display: 'flex', flexDirection: 'column', gap: 16,
    }}>
      <div style={{ color: '#aaa', fontSize: 13 }}>Classical Analysis</div>

      <div>
        <div style={{ fontSize: 11, color: '#888', marginBottom: 6 }}>Session</div>
        <select
          value={selectedSessionId ?? ''}
          onChange={e => setSelectedSessionId(e.target.value || null)}
          style={{
            width: '100%', padding: '8px 12px', borderRadius: 6, background: '#12122a',
            color: '#eee', border: '1px solid #333', fontSize: 12,
          }}
        >
          <option value="">All sessions</option>
          {sessions.map(s => (
            <option key={s.id} value={s.id}>
              {s.userId} – {s.windowCount} windows ({new Date(s.startedAt).toLocaleString()})
            </option>
          ))}
        </select>
      </div>

      {analyzing && (
        <div style={{
          display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px',
          background: 'rgba(37,99,235,0.2)', borderRadius: 8, border: '1px solid rgba(37,99,235,0.4)',
        }}>
          <div style={{
            width: 18, height: 18, border: '2px solid #64B5F6', borderTopColor: 'transparent',
            borderRadius: '50%', animation: 'spin 0.8s linear infinite',
          }} />
          <span style={{ color: '#64B5F6', fontSize: 13 }}>Running analysis… calling flow-classifier for each window</span>
        </div>
      )}

      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        <button
          onClick={runAnalysis}
          disabled={analyzing}
          style={{
            padding: '10px 16px', borderRadius: 6, background: analyzing ? '#333' : '#2563eb',
            color: analyzing ? '#888' : '#fff', border: 'none', cursor: analyzing ? 'not-allowed' : 'pointer', fontSize: 13,
            display: 'flex', alignItems: 'center', gap: 8,
          }}
        >
          {analyzing && (
            <span style={{ width: 14, height: 14, border: '2px solid #888', borderTopColor: 'transparent', borderRadius: '50%', animation: 'spin 0.8s linear infinite' }} />
          )}
          {analyzing ? 'Analyzing…' : 'Run Classical NN Analysis'}
        </button>
        {selectedSessionId && onStopSession && (
          <button
            onClick={() => onStopSession(selectedSessionId)}
            style={{
              padding: '10px 16px', borderRadius: 6, background: '#dc2626', color: '#fff',
              border: 'none', cursor: 'pointer', fontSize: 13,
            }}
          >
            Stop Session
          </button>
        )}
      </div>

      {error && (
        <div style={{ fontSize: 12, color: '#f87171', padding: '8px 12px', background: 'rgba(220,38,38,0.15)', borderRadius: 6 }}>
          {error}
        </div>
      )}

      {result && !error && (
        <div style={{
          fontSize: 12, padding: '8px 12px', borderRadius: 6,
          ...(result.analyzed > 0
            ? { color: '#81C784', background: 'rgba(76,175,80,0.15)' }
            : { color: '#888', background: 'rgba(128,128,128,0.1)' }
          ),
        }}>
          {result.analyzed > 0
            ? `Done: analyzed ${result.analyzed} windows, labeled ${result.labeled}`
            : `No unlabeled windows. All ${labelStats?.total ?? 0} windows already have labels.`
          }
        </div>
      )}

      <div>
        <div style={{ fontSize: 11, color: '#888', marginBottom: 8 }}>Label distribution</div>
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height={120}>
            <BarChart data={chartData} layout="vertical" margin={{ left: 0, right: 20 }}>
              <XAxis type="number" stroke="#666" fontSize={10} />
              <YAxis type="category" dataKey="name" stroke="#666" fontSize={10} width={70} />
              <Tooltip contentStyle={{ background: '#1a1a2e', border: '1px solid #333' }} />
              <Bar dataKey="count" radius={4}>
                {chartData.map((entry, i) => (
                  <Cell key={i} fill={entry.color} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        ) : (
          <div style={{ fontSize: 12, color: '#666' }}>No data</div>
        )}
      </div>

      {labelStats && (labelStats.flowCount + labelStats.notFlowCount) > 0 && (
        <>
          <LabeledWindowsTable sessionId={selectedSessionId} />
          <DeepDiveChart sessionId={selectedSessionId} />
        </>
      )}

      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
};
