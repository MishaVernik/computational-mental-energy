import React, { useState, useEffect, useCallback } from 'react';

const API_BASE = window.location.hostname === 'localhost'
  ? 'http://localhost:5000'
  : `http://${window.location.hostname}:5000`;

interface SegmentJournal {
  id: string;
  actionName: string;
  actionSlug: string;
  description?: string;
  difficulty: number;
  startTime: string;
  endTime: string;
  durationMin: number;
  windowCount: number;
  cmeTotal: number;
  avgPFlow: number;
  cmePerMin: number;
  pctOfSession: number;
}

interface SessionJournal {
  sessionId: string;
  startedAt: string;
  endedAt?: string;
  durationMin: number;
  totalWindows: number;
  cmeTotal: number;
  avgCmeRate: number;
  avgPFlow: number;
  flowShare: number;
  weightedDifficulty: number;
  sessionBudget: number;
  budgetUsedPct: number;
  segments: SegmentJournal[];
}

interface DaySummary {
  totalCme: number;
  totalMinutes: number;
  totalSessions: number;
  totalSegments: number;
  avgDifficulty: number;
  avgFlowShare: number;
  dayBudget: number;
  budgetUsedPct: number;
  topActivities: { actionName: string; spent: number; minutes: number; avgRatePerMin: number; windows: number }[];
}

interface DayJournal {
  sessions: SessionJournal[];
  daySummary: DaySummary;
}

function fmtVn(v: number): string {
  const abs = Math.abs(v);
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M';
  if (abs >= 1_000) return (v / 1_000).toFixed(1) + 'K';
  if (abs >= 1) return v.toFixed(1);
  return v.toFixed(3);
}

function fmtTime(iso: string): string {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function fmtDur(min: number): string {
  if (min < 1) return '<1m';
  if (min >= 60) return `${Math.floor(min / 60)}h ${Math.round(min % 60)}m`;
  return `${Math.round(min)}m`;
}

function diffColor(d: number): string {
  if (d >= 0.7) return '#F44336';
  if (d >= 0.4) return '#FFC107';
  return '#4CAF50';
}

function flowColor(f: number): string {
  if (f >= 0.85) return '#4CAF50';
  if (f >= 0.5) return '#FFC107';
  return '#888';
}

function budgetColor(pct: number): string {
  if (pct >= 90) return '#F44336';
  if (pct >= 60) return '#FFC107';
  return '#4CAF50';
}

interface Props {
  refreshKey?: number;
}

export const DayJournal: React.FC<Props> = ({ refreshKey }) => {
  const [journal, setJournal] = useState<DayJournal | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expandedSessions, setExpandedSessions] = useState<Set<string>>(new Set());

  const fetchJournal = useCallback(async () => {
    try {
      setLoading(true);
      const res = await fetch(`${API_BASE}/api/dataset/day-journal`);
      if (res.ok) {
        const data = await res.json();
        setJournal(data);
        setError(null);
        if (data.sessions.length > 0) {
          setExpandedSessions(new Set([data.sessions[data.sessions.length - 1].sessionId]));
        }
      } else {
        setError(`HTTP ${res.status}`);
      }
    } catch {
      setError('API unavailable');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchJournal(); }, [fetchJournal, refreshKey]);

  const toggleSession = (id: string) => {
    setExpandedSessions(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const card: React.CSSProperties = {
    background: '#1a1a2e', borderRadius: 12, padding: 16,
    border: '1px solid #333', fontSize: 12,
  };

  if (!journal && loading) return <div style={card}><div style={{ color: '#666' }}>Loading journal...</div></div>;
  if (error) return <div style={card}><div style={{ color: '#F44336' }}>{error}</div></div>;
  if (!journal) return <div style={card}><div style={{ color: '#666' }}>No data</div></div>;

  const { daySummary: ds, sessions } = journal;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* ── Day overview ─────────────────────────── */}
      <div style={card}>
        <div style={{ color: '#aaa', fontSize: 14, fontWeight: 600, marginBottom: 12 }}>
          Day Overview
        </div>

        {/* Budget gauge */}
        <div style={{ marginBottom: 12 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
            <span style={{ color: '#ccc' }}>
              <strong style={{ color: '#64B5F6', fontSize: 18 }}>{fmtVn(ds.totalCme)}</strong>
              <span style={{ color: '#888', marginLeft: 4 }}>/ {fmtVn(ds.dayBudget)} Vn budget</span>
            </span>
            <span style={{ color: budgetColor(ds.budgetUsedPct), fontWeight: 700 }}>
              {ds.budgetUsedPct.toFixed(0)}%
            </span>
          </div>
          <div style={{ height: 8, background: '#222', borderRadius: 4, overflow: 'hidden' }}>
            <div style={{
              width: `${Math.min(ds.budgetUsedPct, 100).toFixed(1)}%`, height: '100%',
              background: budgetColor(ds.budgetUsedPct), borderRadius: 4,
              transition: 'width 0.5s ease',
            }} />
          </div>
          <div style={{ color: '#555', fontSize: 10, marginTop: 4 }}>
            Budget scales with activity difficulty: rest ~3K Vn/hr, deep work ~30K Vn/hr
          </div>
        </div>

        {/* Day stats */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8, marginBottom: 12 }}>
          {[
            { label: 'Sessions', value: ds.totalSessions.toString(), color: '#64B5F6' },
            { label: 'Segments', value: ds.totalSegments.toString(), color: '#81C784' },
            { label: 'Duration', value: fmtDur(ds.totalMinutes), color: '#ccc' },
            { label: 'Avg Difficulty', value: `${(ds.avgDifficulty * 100).toFixed(0)}%`, color: diffColor(ds.avgDifficulty) },
          ].map(m => (
            <div key={m.label} style={{ background: '#12122a', borderRadius: 8, padding: '8px 10px', textAlign: 'center' }}>
              <div style={{ color: '#666', fontSize: 10 }}>{m.label}</div>
              <div style={{ color: m.color, fontSize: 15, fontWeight: 700 }}>{m.value}</div>
            </div>
          ))}
        </div>

        {/* Top activities */}
        {ds.topActivities.length > 0 && (
          <div>
            <div style={{ color: '#888', fontSize: 11, marginBottom: 6 }}>Top Activities</div>
            {ds.topActivities.map((a, i) => {
              const maxSpent = ds.topActivities[0].spent || 1;
              return (
                <div key={i} style={{ marginBottom: 4 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11 }}>
                    <span style={{ color: '#ccc' }}>{a.actionName}</span>
                    <span>
                      <span style={{ color: '#64B5F6', fontWeight: 600 }}>{fmtVn(a.spent)}</span>
                      <span style={{ color: '#666', marginLeft: 6 }}>{fmtDur(a.minutes)}</span>
                    </span>
                  </div>
                  <div style={{ height: 3, background: '#222', borderRadius: 2, overflow: 'hidden', marginTop: 2 }}>
                    <div style={{
                      width: `${(a.spent / maxSpent * 100).toFixed(1)}%`, height: '100%',
                      background: '#64B5F6', borderRadius: 2,
                    }} />
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* ── Sessions ─────────────────────────────── */}
      {sessions.map(session => {
        const expanded = expandedSessions.has(session.sessionId);
        const isActive = !session.endedAt;
        return (
          <div key={session.sessionId} style={{
            ...card,
            border: isActive ? '1px solid #4CAF50' : '1px solid #333',
          }}>
            {/* Session header */}
            <button onClick={() => toggleSession(session.sessionId)} style={{
              display: 'flex', justifyContent: 'space-between', alignItems: 'center',
              width: '100%', background: 'none', border: 'none', cursor: 'pointer',
              padding: 0, marginBottom: expanded ? 12 : 0,
            }}>
              <div style={{ textAlign: 'left' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <span style={{ color: '#aaa', fontSize: 13, fontWeight: 600 }}>
                    {fmtTime(session.startedAt)}
                    {session.endedAt ? ` – ${fmtTime(session.endedAt)}` : ''}
                  </span>
                  {isActive && (
                    <span style={{
                      padding: '1px 6px', background: '#1e3a2e', color: '#4CAF50',
                      borderRadius: 3, fontSize: 9, fontWeight: 700,
                    }}>LIVE</span>
                  )}
                  <span style={{ color: '#555', fontSize: 10 }}>
                    {fmtDur(session.durationMin)} · {session.totalWindows}w
                  </span>
                </div>
                <div style={{ display: 'flex', gap: 12, marginTop: 4, fontSize: 11 }}>
                  <span style={{ color: '#64B5F6' }}>{fmtVn(session.cmeTotal)} Vn</span>
                  <span style={{ color: flowColor(session.flowShare) }}>
                    flow {(session.flowShare * 100).toFixed(0)}%
                  </span>
                  <span style={{ color: diffColor(session.weightedDifficulty) }}>
                    diff {(session.weightedDifficulty * 100).toFixed(0)}%
                  </span>
                </div>
              </div>
              <div style={{ textAlign: 'right', flexShrink: 0 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  {/* Mini budget bar */}
                  <div style={{ width: 50, height: 6, background: '#222', borderRadius: 3, overflow: 'hidden' }}>
                    <div style={{
                      width: `${Math.min(session.budgetUsedPct, 100).toFixed(1)}%`, height: '100%',
                      background: budgetColor(session.budgetUsedPct), borderRadius: 3,
                    }} />
                  </div>
                  <span style={{ color: budgetColor(session.budgetUsedPct), fontSize: 11, fontWeight: 600, minWidth: 32, textAlign: 'right' }}>
                    {session.budgetUsedPct.toFixed(0)}%
                  </span>
                  <span style={{ color: '#555', fontSize: 14 }}>{expanded ? '▾' : '▸'}</span>
                </div>
                <div style={{ color: '#666', fontSize: 10, marginTop: 2 }}>
                  budget {fmtVn(session.sessionBudget)}
                </div>
              </div>
            </button>

            {/* Session segments */}
            {expanded && (
              <div>
                {/* Session budget detail */}
                <div style={{
                  display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 6, marginBottom: 12,
                }}>
                  {[
                    { label: 'CME Total', value: fmtVn(session.cmeTotal), color: '#64B5F6' },
                    { label: 'Budget', value: fmtVn(session.sessionBudget), color: '#888' },
                    { label: 'Rate', value: `${fmtVn(session.avgCmeRate)}/min`, color: '#ccc' },
                  ].map(m => (
                    <div key={m.label} style={{ background: '#12122a', borderRadius: 6, padding: '6px 8px', textAlign: 'center' }}>
                      <div style={{ color: '#555', fontSize: 9 }}>{m.label}</div>
                      <div style={{ color: m.color, fontSize: 13, fontWeight: 600 }}>{m.value}</div>
                    </div>
                  ))}
                </div>

                {session.segments.length === 0 ? (
                  <div style={{ color: '#555', fontStyle: 'italic', fontSize: 11 }}>
                    No segments annotated for this session
                  </div>
                ) : (
                  <div>
                    {session.segments.map(seg => (
                      <div key={seg.id} style={{
                        background: '#12122a', borderRadius: 8, padding: 10, marginBottom: 6,
                        borderLeft: `3px solid ${diffColor(seg.difficulty)}`,
                      }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <div style={{ flex: 1, minWidth: 0 }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                              <span style={{ color: '#eee', fontWeight: 600, fontSize: 13 }}>{seg.actionName}</span>
                              <span style={{ color: '#555', fontSize: 10 }}>
                                {fmtTime(seg.startTime)} – {fmtTime(seg.endTime)}
                              </span>
                            </div>
                            {seg.description && (
                              <div style={{ color: '#888', fontSize: 11, marginTop: 3 }}>{seg.description}</div>
                            )}
                            <div style={{ display: 'flex', gap: 12, marginTop: 4, fontSize: 10, color: '#777' }}>
                              <span>Duration: <strong style={{ color: '#ccc' }}>{fmtDur(seg.durationMin)}</strong></span>
                              <span>Windows: <strong style={{ color: '#ccc' }}>{seg.windowCount}</strong></span>
                              <span>Rate: <strong style={{ color: '#ccc' }}>{fmtVn(seg.cmePerMin)}/min</strong></span>
                            </div>
                          </div>
                          <div style={{ textAlign: 'right', marginLeft: 8, flexShrink: 0 }}>
                            <div style={{ color: '#64B5F6', fontSize: 16, fontWeight: 700 }}>
                              {fmtVn(seg.cmeTotal)}
                            </div>
                            <div style={{ fontSize: 10, color: '#888' }}>
                              {seg.pctOfSession.toFixed(0)}% of session
                            </div>
                            <div style={{
                              display: 'flex', alignItems: 'center', gap: 4, justifyContent: 'flex-end', marginTop: 2,
                            }}>
                              <span style={{ fontSize: 9, color: '#666' }}>flow</span>
                              <span style={{ fontSize: 11, fontWeight: 600, color: flowColor(seg.avgPFlow) }}>
                                {(seg.avgPFlow * 100).toFixed(0)}%
                              </span>
                              <span style={{ fontSize: 9, color: '#666', marginLeft: 4 }}>diff</span>
                              <span style={{ fontSize: 11, fontWeight: 600, color: diffColor(seg.difficulty) }}>
                                {(seg.difficulty * 100).toFixed(0)}%
                              </span>
                            </div>
                          </div>
                        </div>

                        {/* Mini energy bar showing % of session */}
                        <div style={{ height: 3, background: '#1a1a2e', borderRadius: 2, overflow: 'hidden', marginTop: 6 }}>
                          <div style={{
                            width: `${Math.min(seg.pctOfSession, 100).toFixed(1)}%`, height: '100%',
                            background: '#64B5F6', borderRadius: 2,
                          }} />
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
};
