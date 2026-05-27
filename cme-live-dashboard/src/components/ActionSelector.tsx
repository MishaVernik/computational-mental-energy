import React, { useState, useEffect, useMemo, useCallback } from 'react';
import type { ActionTreeNode, ActiveAction } from '../types';

import { getApiBase } from '../runtimeApi';

const API_BASE = getApiBase();

interface Props {
  currentAction: ActiveAction | null;
  onStartAction: (actionDefId: string, description?: string) => void;
  onStopAction: () => void;
  sessionId: string | null;
  onSegmentSaved?: () => void;
}

export const ActionSelector: React.FC<Props> = ({ currentAction, onStartAction, onStopAction, sessionId, onSegmentSaved }) => {
  const [tree, setTree] = useState<ActionTreeNode[]>([]);
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [elapsed, setElapsed] = useState(0);
  const [mode, setMode] = useState<'live' | 'annotate'>('live');

  const [selectedAction, setSelectedAction] = useState<ActionTreeNode | null>(null);
  const [description, setDescription] = useState('');
  const [durationMin, setDurationMin] = useState(5);
  const [saving, setSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<string | null>(null);

  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [newParentId, setNewParentId] = useState<string | null>(null);
  const [newDifficulty, setNewDifficulty] = useState(0.5);

  const fetchTree = useCallback(async () => {
    try {
      setLoading(true);
      const res = await fetch(`${API_BASE}/api/actions/tree`);
      if (res.ok) setTree(await res.json());
    } catch (e) {
      console.error('Failed to fetch action tree:', e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchTree(); }, [fetchTree]);

  useEffect(() => {
    if (!currentAction) { setElapsed(0); return; }
    const start = new Date(currentAction.startedAt).getTime();
    const tick = () => setElapsed(Math.floor((Date.now() - start) / 1000));
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [currentAction]);

  const formatElapsed = (s: number) => {
    const m = Math.floor(s / 60);
    const sec = s % 60;
    return `${m}:${sec.toString().padStart(2, '0')}`;
  };

  const toggleExpand = (id: string) => {
    setExpandedIds(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const flatLeaves = useMemo(() => {
    const result: { node: ActionTreeNode; categoryName: string }[] = [];
    const walk = (nodes: ActionTreeNode[], parent: string) => {
      for (const n of nodes) {
        if (n.children && n.children.length > 0) {
          walk(n.children, n.name);
        } else {
          result.push({ node: n, categoryName: parent });
        }
      }
    };
    walk(tree, '');
    return result;
  }, [tree]);

  const filtered = useMemo(() => {
    if (!search.trim()) return null;
    const q = search.trim().toLowerCase();
    return flatLeaves.filter(
      ({ node, categoryName }) =>
        node.name.toLowerCase().includes(q) ||
        node.slug.toLowerCase().includes(q) ||
        categoryName.toLowerCase().includes(q)
    );
  }, [search, flatLeaves]);

  const handleSelectAction = (node: ActionTreeNode) => {
    if (mode === 'live' && sessionId) {
      onStartAction(node.id, description || undefined);
      setDescription('');
      setSearch('');
    } else {
      setSelectedAction(node);
      setSearch('');
    }
  };

  const handleAnnotate = async () => {
    if (!selectedAction) return;
    setSaving(true);
    try {
      const res = await fetch(`${API_BASE}/api/dataset/segments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          actionDefinitionId: selectedAction.id,
          description: description || null,
          sessionId: sessionId || null,
          durationMinutes: durationMin,
        }),
      });
      if (res.ok) {
        setLastSaved(`Saved: ${selectedAction.name} (${durationMin}m)`);
        setSelectedAction(null);
        setDescription('');
        setDurationMin(5);
        onSegmentSaved?.();
        setTimeout(() => setLastSaved(null), 4000);
      }
    } catch (e) {
      console.error('Failed to annotate segment:', e);
    } finally {
      setSaving(false);
    }
  };

  const handleCreate = async () => {
    if (!newName.trim()) return;
    try {
      const res = await fetch(`${API_BASE}/api/actions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: newName.trim(),
          parentId: newParentId,
          defaultDifficulty: newDifficulty,
        }),
      });
      if (res.ok) {
        setShowCreate(false);
        setNewName('');
        setNewDifficulty(0.5);
        await fetchTree();
      }
    } catch (e) {
      console.error('Failed to create action:', e);
    }
  };

  const cardStyle: React.CSSProperties = {
    background: '#1a1a2e', borderRadius: 12, padding: 16,
    border: '1px solid #333', fontSize: 12,
  };

  const renderActiveAction = () => currentAction && (
    <div style={{
      background: '#1e3a2e', borderRadius: 8, padding: 10, marginBottom: 10,
      border: '1px solid #4CAF50',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ color: '#4CAF50', fontWeight: 600, fontSize: 13 }}>
            {currentAction.name}
          </div>
          <div style={{ color: '#888', fontSize: 11, marginTop: 2 }}>
            {formatElapsed(elapsed)} · difficulty {(currentAction.difficulty * 100).toFixed(0)}%
          </div>
        </div>
        <button onClick={onStopAction} style={{
          padding: '6px 14px', background: '#F44336', color: '#fff',
          border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 11, fontWeight: 600,
        }}>
          Stop
        </button>
      </div>
    </div>
  );

  const renderAnnotationForm = () => selectedAction && mode === 'annotate' && (
    <div style={{
      background: '#1e2a3e', borderRadius: 8, padding: 10, marginBottom: 10,
      border: '1px solid #64B5F6',
    }}>
      <div style={{ color: '#64B5F6', fontWeight: 600, fontSize: 13, marginBottom: 6 }}>
        {selectedAction.name}
      </div>
      <textarea
        placeholder="What happened? e.g. 'Read 50 pages of Design Patterns book'"
        value={description}
        onChange={e => setDescription(e.target.value)}
        rows={2}
        style={{
          width: '100%', padding: '6px 8px', background: '#111', border: '1px solid #444',
          borderRadius: 4, color: '#eee', fontSize: 12, resize: 'vertical', boxSizing: 'border-box',
        }}
      />
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <span style={{ color: '#888' }}>Duration:</span>
        <input
          type="number" min={1} max={480} value={durationMin}
          onChange={e => setDurationMin(parseInt(e.target.value) || 5)}
          style={{
            width: 50, padding: '3px 6px', background: '#111', border: '1px solid #444',
            borderRadius: 4, color: '#eee', fontSize: 12, textAlign: 'center',
          }}
        />
        <span style={{ color: '#888' }}>min ago</span>
      </div>
      <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
        <button onClick={handleAnnotate} disabled={saving} style={{
          padding: '5px 12px', background: '#64B5F6', color: '#fff',
          border: 'none', borderRadius: 4, cursor: 'pointer', fontSize: 11, opacity: saving ? 0.5 : 1,
        }}>
          {saving ? 'Saving...' : 'Save Segment'}
        </button>
        <button onClick={() => setSelectedAction(null)} style={{
          padding: '5px 12px', background: '#333', color: '#aaa',
          border: 'none', borderRadius: 4, cursor: 'pointer', fontSize: 11,
        }}>
          Cancel
        </button>
      </div>
    </div>
  );

  const renderPicker = () => (
    <>
      {mode === 'live' && sessionId && !currentAction && (
        <input
          type="text"
          placeholder="What are you doing? (optional note)"
          value={description}
          onChange={e => setDescription(e.target.value)}
          style={{
            width: '100%', padding: '6px 10px', background: '#111', border: '1px solid #444',
            borderRadius: 6, color: '#eee', fontSize: 12, marginBottom: 4, boxSizing: 'border-box',
          }}
        />
      )}

      <input
        type="text"
        placeholder="Search activities..."
        value={search}
        onChange={e => setSearch(e.target.value)}
        style={{
          width: '100%', padding: '6px 10px', background: '#111', border: '1px solid #444',
          borderRadius: 6, color: '#eee', fontSize: 12, marginBottom: 8, boxSizing: 'border-box',
        }}
      />

      {filtered ? (
        <div style={{ maxHeight: 180, overflowY: 'auto' }}>
          {filtered.length === 0 && <div style={{ color: '#666', padding: 4 }}>No matches</div>}
          {filtered.map(({ node, categoryName }) => (
            <button key={node.id} onClick={() => handleSelectAction(node)} style={{
              display: 'block', width: '100%', textAlign: 'left',
              padding: '6px 8px', background: 'none', border: 'none',
              color: '#ccc', cursor: 'pointer', fontSize: 12, borderRadius: 4,
            }}
              onMouseEnter={e => (e.currentTarget.style.background = '#2a2a4e')}
              onMouseLeave={e => (e.currentTarget.style.background = 'none')}
            >
              <span style={{ color: '#888' }}>{categoryName} &rsaquo; </span>
              {node.name}
              <span style={{ color: '#666', marginLeft: 8 }}>
                {(node.defaultDifficulty * 100).toFixed(0)}%
              </span>
            </button>
          ))}
        </div>
      ) : (
        <div style={{ maxHeight: 200, overflowY: 'auto' }}>
          {loading && <div style={{ color: '#666' }}>Loading...</div>}
          {tree.map(category => (
            <div key={category.id}>
              <button onClick={() => toggleExpand(category.id)} style={{
                display: 'flex', alignItems: 'center', gap: 4, width: '100%',
                padding: '5px 4px', background: 'none', border: 'none',
                color: '#aaa', cursor: 'pointer', fontSize: 12, fontWeight: 600,
              }}>
                <span style={{ fontSize: 10, width: 14 }}>
                  {expandedIds.has(category.id) ? '▾' : '▸'}
                </span>
                {category.name}
                <span style={{ color: '#555', fontWeight: 400, marginLeft: 4 }}>
                  ({category.children?.length ?? 0})
                </span>
              </button>
              {expandedIds.has(category.id) && category.children?.map(action => (
                <button key={action.id} onClick={() => handleSelectAction(action)} style={{
                  display: 'block', width: '100%', textAlign: 'left',
                  padding: '4px 8px 4px 24px', background: 'none', border: 'none',
                  color: '#ccc', cursor: 'pointer', fontSize: 12, borderRadius: 4,
                }}
                  onMouseEnter={e => (e.currentTarget.style.background = '#2a2a4e')}
                  onMouseLeave={e => (e.currentTarget.style.background = 'none')}
                >
                  {action.name}
                  <span style={{ color: '#666', marginLeft: 8 }}>
                    {(action.defaultDifficulty * 100).toFixed(0)}%
                  </span>
                </button>
              ))}
            </div>
          ))}
        </div>
      )}
    </>
  );

  return (
    <div style={cardStyle}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <span style={{ color: '#aaa', fontSize: 13 }}>Activity Tracking</span>
        <div style={{ display: 'flex', gap: 2 }}>
          {(['live', 'annotate'] as const).map(m => (
            <button key={m} onClick={() => setMode(m)} style={{
              padding: '3px 8px', fontSize: 10, border: 'none', borderRadius: 3, cursor: 'pointer',
              background: mode === m ? (m === 'live' ? '#4CAF50' : '#64B5F6') : '#333',
              color: mode === m ? '#fff' : '#888',
            }}>
              {m === 'live' ? 'Live' : 'Annotate'}
            </button>
          ))}
          {currentAction && (
            <span style={{ color: '#4CAF50', fontSize: 11, marginLeft: 4 }}>
              {formatElapsed(elapsed)}
            </span>
          )}
        </div>
      </div>

      {renderActiveAction()}
      {renderAnnotationForm()}

      {mode === 'live' && !currentAction && renderPicker()}
      {mode === 'annotate' && !selectedAction && renderPicker()}

      {!currentAction && !selectedAction && (
        <div style={{ marginTop: 8, borderTop: '1px solid #333', paddingTop: 8 }}>
          {showCreate ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <input type="text" placeholder="Action name..." value={newName}
                onChange={e => setNewName(e.target.value)}
                style={{ padding: '5px 8px', background: '#111', border: '1px solid #444', borderRadius: 4, color: '#eee', fontSize: 12 }}
              />
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ color: '#888' }}>Difficulty:</span>
                <input type="range" min="0" max="1" step="0.05" value={newDifficulty}
                  onChange={e => setNewDifficulty(parseFloat(e.target.value))} style={{ flex: 1 }}
                />
                <span style={{ color: '#aaa', minWidth: 32 }}>{(newDifficulty * 100).toFixed(0)}%</span>
              </div>
              <select value={newParentId ?? ''} onChange={e => setNewParentId(e.target.value || null)}
                style={{ padding: '5px 8px', background: '#111', border: '1px solid #444', borderRadius: 4, color: '#eee', fontSize: 12 }}
              >
                <option value="">No category</option>
                {tree.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              <div style={{ display: 'flex', gap: 6 }}>
                <button onClick={handleCreate} style={{ padding: '5px 12px', background: '#4CAF50', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer', fontSize: 11 }}>
                  Create
                </button>
                <button onClick={() => setShowCreate(false)} style={{ padding: '5px 12px', background: '#333', color: '#aaa', border: 'none', borderRadius: 4, cursor: 'pointer', fontSize: 11 }}>
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <button onClick={() => setShowCreate(true)} style={{
              padding: '5px 10px', background: 'none', border: '1px dashed #555',
              borderRadius: 4, color: '#888', cursor: 'pointer', fontSize: 11, width: '100%',
            }}>
              + Add Custom Activity
            </button>
          )}
        </div>
      )}

      {lastSaved && (
        <div style={{
          marginTop: 8, padding: '6px 10px', background: '#1e3a2e',
          borderRadius: 6, color: '#4CAF50', fontSize: 11, border: '1px solid #2e5a3e',
        }}>
          {lastSaved}
        </div>
      )}
    </div>
  );
};
