import React, { useState, useEffect, useCallback } from 'react';

const API_BASE = window.location.hostname === 'localhost'
  ? 'http://localhost:5000'
  : `http://${window.location.hostname}:5000`;

interface Segment {
  id: string;
  sessionId: string;
  startTime: string;
  endTime: string;
  actionName: string;
  actionSlug: string;
  description?: string;
  difficulty: number;
  windowCount: number;
  cmeTotalVn: number;
  avgPFlow: number;
  createdAt: string;
}

interface ActivityCompareRow {
  slug: string;
  displayName: string;
  icon?: string;
  totalCmeVn: number;
  totalMinutes: number;
  sessionCount: number;
  avgPFlow: number;
  peakPFlow: number;
  lastUsedAt: string;
  windowCount: number;
}

type SortKey = 'cme' | 'avgPFlow' | 'minutes' | 'peak';
type ViewMode = 'current' | 'compare';

function fmtVn(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M';
  if (abs >= 1_000) return (v / 1_000).toFixed(1) + 'K';
  if (abs >= 1) return v.toFixed(1);
  return v.toFixed(3);
}

function fmtDuration(start: string, end: string): string {
  const ms = new Date(end).getTime() - new Date(start).getTime();
  const min = Math.round(ms / 60000);
  if (min < 1) return '<1m';
  if (min >= 60) return `${Math.floor(min / 60)}h ${min % 60}m`;
  return `${min}m`;
}

function fmtTime(iso: string): string {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

interface Props {
  sessionId: string | null;
  refreshKey?: number;
}

export const ActionSegments: React.FC<Props> = ({ sessionId, refreshKey }) => {
  const [segments, setSegments] = useState<Segment[]>([]);
  const [compareRows, setCompareRows] = useState<ActivityCompareRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [mode, setMode] = useState<ViewMode>('current');
  const [sortBy, setSortBy] = useState<SortKey>('cme');

  const fetchSegments = useCallback(async () => {
    try {
      setLoading(true);
      const url = sessionId
        ? `${API_BASE}/api/dataset/segments?sessionId=${sessionId}&limit=50`
        : `${API_BASE}/api/dataset/segments?limit=50`;
      const res = await fetch(url);
      if (res.ok) setSegments(await res.json());
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [sessionId]);

  const fetchCompare = useCallback(async () => {
    try {
      setLoading(true);
      const res = await fetch(`${API_BASE}/api/activities/compare?days=30`);
      if (res.ok) setCompareRows(await res.json());
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (mode === 'current') fetchSegments();
    else fetchCompare();
  }, [mode, fetchSegments, fetchCompare, refreshKey]);

  // Aggregate by action name (current-session view)
  const byAction = segments.reduce((acc, s) => {
    const key = s.actionName;
    if (!acc[key]) acc[key] = { name: key, totalVn: 0, totalMin: 0, count: 0, avgPFlow: 0, windows: 0 };
    const min = (new Date(s.endTime).getTime() - new Date(s.startTime).getTime()) / 60000;
    acc[key].totalVn += s.cmeTotalVn;
    acc[key].totalMin += min;
    acc[key].count += 1;
    acc[key].avgPFlow += s.avgPFlow;
    acc[key].windows += s.windowCount;
    return acc;
  }, {} as Record<string, { name: string; totalVn: number; totalMin: number; count: number; avgPFlow: number; windows: number }>);

  const actionSummaries = Object.values(byAction)
    .map(a => ({ ...a, avgPFlow: a.count > 0 ? a.avgPFlow / a.count : 0 }))
    .sort((a, b) => b.totalVn - a.totalVn);

  const maxVn = actionSummaries.length > 0 ? Math.max(...actionSummaries.map(a => a.totalVn)) : 1;

  const sortedCompare = [...compareRows].sort((a, b) => {
    switch (sortBy) {
      case 'avgPFlow': return b.avgPFlow - a.avgPFlow;
      case 'minutes':  return b.totalMinutes - a.totalMinutes;
      case 'peak':     return b.peakPFlow - a.peakPFlow;
      default:         return b.totalCmeVn - a.totalCmeVn;
    }
  });

  const compareMaxVn = sortedCompare.length > 0 ? Math.max(...sortedCompare.map(r => r.totalCmeVn)) : 1;

  const cardStyle: React.CSSProperties = {
    background: '#1a1a2e', borderRadius: 12, padding: 16,
    border: '1px solid #333', fontSize: 12,
  };

  const tabStyle = (active: boolean): React.CSSProperties => ({
    padding: '4px 10px', borderRadius: 6, cursor: 'pointer', fontSize: 11,
    background: active ? '#2a2a4e' : 'transparent',
    color: active ? '#fff' : '#888',
    border: active ? '1px solid #4a4a8e' : '1px solid transparent',
  });

  const sortBtn = (key: SortKey, label: string): React.CSSProperties => ({
    padding: '2px 8px', borderRadius: 4, cursor: 'pointer', fontSize: 10,
    background: sortBy === key ? '#2a2a4e' : '#1a1a2e',
    color: sortBy === key ? '#fff' : '#777',
    border: '1px solid #333',
  });

  return (
    <div style={cardStyle}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
        <div style={{ color: '#aaa', fontSize: 13 }}>Action Analysis</div>
        <div style={{ display: 'flex', gap: 6 }}>
          <div role="button" style={tabStyle(mode === 'current')} onClick={() => setMode('current')}>Current session</div>
          <div role="button" style={tabStyle(mode === 'compare')} onClick={() => setMode('compare')}>Compare (30d)</div>
        </div>
      </div>

      {mode === 'current' && (
        <>
          {segments.length === 0 && !loading && (
            <div style={{ color: '#555', fontStyle: 'italic' }}>
              No segments annotated yet. Use "Activity Tracking" -&gt; "Annotate" to tag what you were doing.
            </div>
          )}

          {actionSummaries.length > 0 && (
            <div style={{ marginBottom: 12 }}>
              <div style={{ color: '#888', fontSize: 10, marginBottom: 6 }}>CME by Activity</div>
              {actionSummaries.map(a => (
                <div key={a.name} style={{ marginBottom: 6 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 2 }}>
                    <span style={{ color: '#ccc' }}>{a.name}</span>
                    <span style={{ color: '#64B5F6', fontWeight: 600 }}>{fmtVn(a.totalVn)} Vn</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <div style={{ flex: 1, height: 4, background: '#222', borderRadius: 2, overflow: 'hidden' }}>
                      <div style={{
                        width: `${(a.totalVn / maxVn * 100).toFixed(1)}%`,
                        height: '100%',
                        background: a.avgPFlow >= 0.5 ? '#4CAF50' : '#64B5F6',
                        borderRadius: 2,
                      }} />
                    </div>
                    <span style={{ color: '#888', fontSize: 10, minWidth: 70, textAlign: 'right' }}>
                      {a.totalMin.toFixed(0)}m · p={a.avgPFlow.toFixed(2)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}

          {segments.length > 0 && (
            <div style={{ borderTop: '1px solid #333', paddingTop: 8 }}>
              <div style={{ color: '#888', fontSize: 10, marginBottom: 6 }}>Segment Timeline</div>
              <div style={{ maxHeight: 200, overflowY: 'auto' }}>
                {segments.map(s => (
                  <div key={s.id} style={{ padding: '6px 0', borderBottom: '1px solid #1e1e3e' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                          <span style={{ color: '#ccc', fontWeight: 500 }}>{s.actionName}</span>
                          <span style={{ color: '#555', fontSize: 10 }}>
                            {fmtTime(s.startTime)} - {fmtTime(s.endTime)}
                          </span>
                        </div>
                        {s.description && (
                          <div style={{ color: '#777', fontSize: 11, marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {s.description}
                          </div>
                        )}
                      </div>
                      <div style={{ textAlign: 'right', marginLeft: 8, flexShrink: 0 }}>
                        <div style={{ color: '#64B5F6', fontWeight: 600 }}>{fmtVn(s.cmeTotalVn)}</div>
                        <div style={{ color: '#888', fontSize: 10 }}>
                          {fmtDuration(s.startTime, s.endTime)} · {s.windowCount}w
                        </div>
                        <div style={{
                          fontSize: 10, marginTop: 1,
                          color: s.avgPFlow >= 0.85 ? '#4CAF50' : s.avgPFlow >= 0.5 ? '#FFC107' : '#888',
                        }}>
                          flow {(s.avgPFlow * 100).toFixed(0)}%
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}

      {mode === 'compare' && (
        <>
          <div style={{ display: 'flex', gap: 4, marginBottom: 10, flexWrap: 'wrap' }}>
            <span style={{ color: '#777', fontSize: 10, alignSelf: 'center' }}>sort by:</span>
            <div role="button" style={sortBtn('cme', 'CME')} onClick={() => setSortBy('cme')}>CME</div>
            <div role="button" style={sortBtn('minutes', 'minutes')} onClick={() => setSortBy('minutes')}>minutes</div>
            <div role="button" style={sortBtn('avgPFlow', 'avg pFlow')} onClick={() => setSortBy('avgPFlow')}>avg pFlow</div>
            <div role="button" style={sortBtn('peak', 'peak pFlow')} onClick={() => setSortBy('peak')}>peak pFlow</div>
          </div>

          {sortedCompare.length === 0 && !loading && (
            <div style={{ color: '#555', fontStyle: 'italic' }}>No activity data in the last 30 days.</div>
          )}

          <div style={{ maxHeight: 320, overflowY: 'auto' }}>
            {sortedCompare.map(r => (
              <div key={r.slug} style={{ padding: '8px 0', borderBottom: '1px solid #1e1e3e' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                  <span style={{ color: '#ccc', fontWeight: 500 }}>{r.displayName}</span>
                  <span style={{ color: '#64B5F6', fontWeight: 600 }}>{fmtVn(r.totalCmeVn)} Vn</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                  <div style={{ flex: 1, height: 4, background: '#222', borderRadius: 2, overflow: 'hidden' }}>
                    <div style={{
                      width: `${(r.totalCmeVn / compareMaxVn * 100).toFixed(1)}%`,
                      height: '100%',
                      background: r.avgPFlow >= 0.5 ? '#4CAF50' : '#64B5F6',
                      borderRadius: 2,
                    }} />
                  </div>
                  <span style={{ color: '#888', fontSize: 10, minWidth: 90, textAlign: 'right' }}>
                    {r.totalMinutes.toFixed(0)}m · {r.sessionCount} sess
                  </span>
                </div>
                <div style={{ display: 'flex', gap: 12, color: '#777', fontSize: 10 }}>
                  <span>avg p={r.avgPFlow.toFixed(2)}</span>
                  <span style={{ color: r.peakPFlow >= 0.85 ? '#4CAF50' : r.peakPFlow >= 0.5 ? '#FFC107' : '#777' }}>
                    peak p={r.peakPFlow.toFixed(2)}
                  </span>
                  <span style={{ marginLeft: 'auto' }}>last: {new Date(r.lastUsedAt).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      {loading && <div style={{ color: '#666', marginTop: 4 }}>Loading...</div>}
    </div>
  );
};
