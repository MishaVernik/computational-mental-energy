import React, { useState, useEffect, useCallback, useRef } from 'react';
import type { ActiveAction } from '../types';

import { getApiBase } from '../runtimeApi';

const API_BASE = getApiBase();

const audioCtxRef: { current: AudioContext | null } = { current: null };
function getAudioCtx() {
  if (!audioCtxRef.current) audioCtxRef.current = new AudioContext();
  return audioCtxRef.current;
}
function playTone(freq: number, durationMs: number, volume = 0.3) {
  const ctx = getAudioCtx();
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = 'sine';
  osc.frequency.value = freq;
  gain.gain.value = volume;
  gain.gain.setValueAtTime(volume, ctx.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + durationMs / 1000);
  osc.connect(gain).connect(ctx.destination);
  osc.start();
  osc.stop(ctx.currentTime + durationMs / 1000);
}
function playStartSound() {
  playTone(880, 200, 0.25);
}
function playEndSound() {
  playTone(660, 150, 0.3);
  setTimeout(() => playTone(880, 300, 0.3), 180);
}
function playWarningBeep() {
  playTone(440, 100, 0.15);
}

interface ProtocolStep {
  id: string;
  name: string;
  slug: string;
  actionDefId: string;
  complexity: number;
  durationSec: number;
  extrapolation: number;
  dailyMinutes: number;
  instruction: string;
}

const PROTOCOL_STEPS: Omit<ProtocolStep, 'actionDefId'>[] = [
  { id: '1', name: 'Resting (Eyes Closed)', slug: 'resting-eyes-closed', complexity: 0.05, durationSec: 180, extrapolation: 20, dailyMinutes: 60, instruction: 'Sit still, close your eyes, breathe normally. Try not to think about anything.' },
  { id: '2', name: 'Browsing', slug: 'browsing', complexity: 0.20, durationSec: 180, extrapolation: 10, dailyMinutes: 30, instruction: 'Open a browser and casually browse news, social media, or any website.' },
  { id: '3', name: 'Email', slug: 'email', complexity: 0.30, durationSec: 180, extrapolation: 20, dailyMinutes: 60, instruction: 'Read and compose emails, or simulate writing messages.' },
  { id: '4', name: 'Reading (General)', slug: 'reading-general', complexity: 0.35, durationSec: 180, extrapolation: 20, dailyMinutes: 60, instruction: 'Read a novel, article, or any non-technical text on screen or paper.' },
  { id: '5', name: 'Reading (Technical)', slug: 'reading-technical', complexity: 0.60, durationSec: 180, extrapolation: 20, dailyMinutes: 60, instruction: 'Read a technical paper, documentation, or textbook. Focus on understanding.' },
  { id: '6', name: 'Coding', slug: 'coding', complexity: 0.70, durationSec: 180, extrapolation: 40, dailyMinutes: 120, instruction: 'Write code or work on a programming task you are comfortable with.' },
  { id: '7', name: 'Debugging', slug: 'debugging', complexity: 0.80, durationSec: 180, extrapolation: 40, dailyMinutes: 120, instruction: 'Debug an issue, read logs, trace through code to find a problem.' },
  { id: '8', name: 'Math / Problem Solving', slug: 'math', complexity: 0.90, durationSec: 180, extrapolation: 20, dailyMinutes: 60, instruction: 'Solve math problems, puzzles, or algorithmic challenges that require concentration.' },
];

interface StepResult {
  windowCount: number;
  meanPFlow: number;
  meanCmeRate: number;
  totalCmeVn: number;
  durationActual: number;
}

interface Props {
  sessionId: string | null;
  currentAction: ActiveAction | null;
  onStartSession: () => void;
  onStopSession: (sid?: string | null) => void;
  onStartAction: (actionDefId: string, description?: string) => void;
  onStopAction: () => void;
}

export const MeasurementProtocol: React.FC<Props> = ({
  sessionId, currentAction,
  onStartSession, onStopSession, onStartAction, onStopAction,
}) => {
  const [steps, setSteps] = useState<ProtocolStep[]>([]);
  const [currentStepIdx, setCurrentStepIdx] = useState(-1);
  const [completedSteps, setCompletedSteps] = useState<Map<number, StepResult>>(new Map());
  const [stepElapsed, setStepElapsed] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const [autoAdvance, setAutoAdvance] = useState(true);
  const [pauseCountdown, setPauseCountdown] = useState(-1);
  const pauseTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const res = await fetch(`${API_BASE}/api/actions/tree`);
        if (!res.ok) return;
        const tree = await res.json();
        const slugToId = new Map<string, string>();
        const walk = (nodes: any[]) => {
          for (const n of nodes) {
            slugToId.set(n.slug, n.id);
            if (n.children) walk(n.children);
          }
        };
        walk(tree);
        setSteps(PROTOCOL_STEPS.map(s => ({
          ...s,
          actionDefId: slugToId.get(s.slug) || '',
        })));
      } catch (e) {
        console.error('Failed to load actions:', e);
      }
    })();
  }, []);

  useEffect(() => {
    if (currentStepIdx < 0 || isPaused || !currentAction) return;
    timerRef.current = setInterval(() => {
      setStepElapsed(prev => prev + 1);
    }, 1000);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [currentStepIdx, isPaused, currentAction]);

  const isRunning = currentStepIdx >= 0 && currentAction != null;
  const activeStep = currentStepIdx >= 0 ? steps[currentStepIdx] : null;

  const handleStartProtocol = () => {
    if (!sessionId) {
      onStartSession();
      setTimeout(() => setCurrentStepIdx(0), 1500);
    } else {
      setCurrentStepIdx(0);
    }
  };

  const handleStartStep = useCallback((idx: number) => {
    const step = steps[idx];
    if (!step || !step.actionDefId || !sessionId) return;
    setCurrentStepIdx(idx);
    setStepElapsed(0);
    playStartSound();
    onStartAction(step.actionDefId, `Protocol: ${step.name}`);
  }, [steps, sessionId, onStartAction]);

  const handleStopStep = useCallback(() => {
    if (currentStepIdx < 0) return;
    const spikeId = currentAction?.actionSpikeId;
    onStopAction();
    playEndSound();

    const justFinishedIdx = currentStepIdx;
    const elapsed = stepElapsed;
    setCurrentStepIdx(-1);
    setStepElapsed(0);

    const fetchStats = async () => {
      if (!spikeId) return;
      await new Promise(r => setTimeout(r, 2000));
      try {
        const res = await fetch(`${API_BASE}/api/sessions/spike-stats/${spikeId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        setCompletedSteps(prev => {
          const next = new Map(prev);
          next.set(justFinishedIdx, {
            windowCount: data.windowCount,
            meanPFlow: data.meanPFlow,
            meanCmeRate: data.meanCmeRate,
            totalCmeVn: data.totalCmeVn,
            durationActual: elapsed,
          });
          return next;
        });
      } catch (e) {
        console.error('Failed to fetch spike stats:', e);
      }
    };
    fetchStats();

    const nextIdx = justFinishedIdx + 1;
    if (autoAdvance && nextIdx < steps.length) {
      setPauseCountdown(15);
    }
  }, [currentStepIdx, currentAction, stepElapsed, onStopAction, autoAdvance, steps.length]);

  useEffect(() => {
    if (!isRunning || !activeStep) return;
    const remaining = activeStep.durationSec - stepElapsed;
    if (remaining <= 0) {
      handleStopStep();
    } else if (remaining === 10 || remaining === 5 || remaining === 3) {
      playWarningBeep();
    }
  }, [stepElapsed, isRunning, activeStep, handleStopStep]);

  useEffect(() => {
    if (pauseCountdown < 0) return;
    if (pauseCountdown === 0) {
      setPauseCountdown(-1);
      const nextIdx = Array.from({ length: steps.length }, (_, i) => i)
        .find(i => !completedSteps.has(i));
      if (nextIdx !== undefined && sessionId) {
        handleStartStep(nextIdx);
      }
      return;
    }
    if (pauseCountdown === 3) playWarningBeep();
    pauseTimerRef.current = setTimeout(() => setPauseCountdown(p => p - 1), 1000);
    return () => { if (pauseTimerRef.current) clearTimeout(pauseTimerRef.current); };
  }, [pauseCountdown, steps.length, completedSteps, sessionId, handleStartStep]);

  const allDone = steps.length > 0 && completedSteps.size === steps.length;
  const totalRecordingTime = Array.from(completedSteps.values()).reduce((s, r) => s + r.durationActual, 0);

  const fmt = (s: number) => {
    const m = Math.floor(s / 60);
    const sec = s % 60;
    return `${m}:${sec.toString().padStart(2, '0')}`;
  };

  const card: React.CSSProperties = {
    background: '#1a1a2e', borderRadius: 12, padding: 16,
    border: '1px solid #333',
  };

  const extrapolatedDaily = Array.from(completedSteps.entries()).reduce((total, [idx, res]) => {
    const step = steps[idx];
    if (!step) return total;
    return total + res.meanCmeRate * step.dailyMinutes * 60;
  }, 0);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Header */}
      <div style={card}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <h2 style={{ margin: 0, fontSize: 18, color: '#64B5F6' }}>
              Measurement Protocol
            </h2>
            <div style={{ color: '#888', fontSize: 12, marginTop: 4 }}>
              8 base activities x 3 min each = 24 min total recording.
              Results extrapolate to a 9.5-hour day.
            </div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 13, color: '#aaa' }}>
              Progress: {completedSteps.size}/{steps.length}
            </div>
            <div style={{ fontSize: 11, color: '#666' }}>
              Recorded: {fmt(totalRecordingTime)}
            </div>
          </div>
        </div>

        {/* Progress bar */}
        <div style={{
          marginTop: 12, height: 6, background: '#333', borderRadius: 3, overflow: 'hidden',
        }}>
          <div style={{
            height: '100%', borderRadius: 3, transition: 'width 0.3s',
            width: `${steps.length > 0 ? (completedSteps.size / steps.length) * 100 : 0}%`,
            background: allDone ? '#4CAF50' : '#64B5F6',
          }} />
        </div>

        {!sessionId && currentStepIdx < 0 && (
          <button onClick={handleStartProtocol} style={{
            marginTop: 12, padding: '10px 24px', background: '#4CAF50', color: '#fff',
            border: 'none', borderRadius: 8, cursor: 'pointer', fontSize: 14, fontWeight: 600,
            width: '100%',
          }}>
            Start Protocol (begins EEG session)
          </button>
        )}
        {sessionId && currentStepIdx < 0 && !allDone && pauseCountdown < 0 && (
          <div style={{ marginTop: 8, color: '#888', fontSize: 12 }}>
            Session active. Click a step below to begin recording.
          </div>
        )}

        {/* Auto-advance toggle */}
        {sessionId && (
          <label style={{
            display: 'flex', alignItems: 'center', gap: 8,
            marginTop: 8, cursor: 'pointer', fontSize: 12, color: '#aaa',
          }}>
            <input
              type="checkbox" checked={autoAdvance}
              onChange={e => {
                setAutoAdvance(e.target.checked);
                if (!e.target.checked) setPauseCountdown(-1);
              }}
              style={{ accentColor: '#64B5F6' }}
            />
            Auto-advance to next step (15s pause between steps)
          </label>
        )}
      </div>

      {/* Pause countdown between steps */}
      {pauseCountdown > 0 && (() => {
        const nextIdx = Array.from({ length: steps.length }, (_, i) => i)
          .find(i => !completedSteps.has(i));
        const nextStep = nextIdx !== undefined ? steps[nextIdx] : null;
        return (
          <div style={{
            ...card, border: '2px solid #64B5F6', background: '#1a2a3e',
            textAlign: 'center',
          }}>
            <div style={{ color: '#64B5F6', fontSize: 14, marginBottom: 4 }}>
              Get ready for the next step
            </div>
            {nextStep && (
              <div style={{ color: '#ddd', fontSize: 18, fontWeight: 700, marginBottom: 8 }}>
                {nextStep.name}
              </div>
            )}
            {nextStep && (
              <div style={{ color: '#888', fontSize: 12, marginBottom: 8 }}>
                {nextStep.instruction}
              </div>
            )}
            <div style={{
              fontSize: 48, fontWeight: 700, color: '#64B5F6',
              fontVariantNumeric: 'tabular-nums',
            }}>
              {pauseCountdown}
            </div>
            <div style={{ fontSize: 11, color: '#888' }}>seconds until auto-start</div>
            <button onClick={() => {
              setPauseCountdown(-1);
            }} style={{
              marginTop: 8, padding: '6px 16px', background: '#333', color: '#aaa',
              border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 12,
            }}>
              Cancel auto-advance
            </button>
          </div>
        );
      })()}

      {/* Active step banner */}
      {isRunning && activeStep && (
        <div style={{
          ...card,
          border: '2px solid #FF9800', background: '#2a2000',
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ color: '#FF9800', fontWeight: 700, fontSize: 16 }}>
                Recording: {activeStep.name}
              </div>
              <div style={{ color: '#ccc', fontSize: 12, marginTop: 4 }}>
                {activeStep.instruction}
              </div>
            </div>
            <div style={{ textAlign: 'center', minWidth: 100 }}>
              <div style={{
                fontSize: 32, fontWeight: 700, fontVariantNumeric: 'tabular-nums',
                color: stepElapsed >= activeStep.durationSec - 10 ? '#F44336' : '#FF9800',
              }}>
                {fmt(activeStep.durationSec - stepElapsed)}
              </div>
              <div style={{ fontSize: 10, color: '#888' }}>remaining</div>
            </div>
          </div>

          {/* Step timer bar */}
          <div style={{
            marginTop: 10, height: 4, background: '#333', borderRadius: 2, overflow: 'hidden',
          }}>
            <div style={{
              height: '100%', borderRadius: 2, transition: 'width 1s linear',
              width: `${(stepElapsed / activeStep.durationSec) * 100}%`,
              background: stepElapsed >= activeStep.durationSec - 10 ? '#F44336' : '#FF9800',
            }} />
          </div>

          <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
            <button onClick={handleStopStep} style={{
              padding: '8px 20px', background: '#F44336', color: '#fff',
              border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 600,
            }}>
              Stop Early
            </button>
            <div style={{ color: '#888', fontSize: 11, display: 'flex', alignItems: 'center' }}>
              ~{Math.floor(stepElapsed / 5)} windows |
              Elapsed: {fmt(stepElapsed)}
            </div>
          </div>
        </div>
      )}

      {/* Steps grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        {steps.map((step, idx) => {
          const result = completedSteps.get(idx);
          const isActive = currentStepIdx === idx;
          const canStart = sessionId != null && !isRunning && !result;

          return (
            <div key={step.id} style={{
              ...card,
              border: isActive ? '2px solid #FF9800'
                : result ? '1px solid #4CAF50'
                : '1px solid #333',
              opacity: isRunning && !isActive ? 0.5 : 1,
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span style={{
                      width: 24, height: 24, borderRadius: '50%', display: 'flex',
                      alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700,
                      background: result ? '#4CAF50' : isActive ? '#FF9800' : '#333',
                      color: result || isActive ? '#fff' : '#888',
                    }}>
                      {result ? '\u2713' : idx + 1}
                    </span>
                    <span style={{
                      fontWeight: 600, fontSize: 14,
                      color: result ? '#4CAF50' : isActive ? '#FF9800' : '#ddd',
                    }}>
                      {step.name}
                    </span>
                  </div>
                  <div style={{ color: '#888', fontSize: 11, marginTop: 4, marginLeft: 32 }}>
                    c={step.complexity} | {step.durationSec / 60} min |
                    x{step.extrapolation} = {step.dailyMinutes} min/day
                  </div>
                </div>
              </div>

              {/* Instruction */}
              <div style={{
                color: '#777', fontSize: 11, marginTop: 6, marginLeft: 32,
                lineHeight: 1.4,
              }}>
                {step.instruction}
              </div>

              {/* Result */}
              {result && (
                <div style={{
                  marginTop: 8, marginLeft: 32, padding: '6px 10px',
                  background: '#1e3a2e', borderRadius: 6, fontSize: 11,
                }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
                    <span style={{ color: '#888' }}>Windows:</span>
                    <span style={{ color: '#aaa' }}>{result.windowCount}</span>
                    <span style={{ color: '#888' }}>Mean pFlow:</span>
                    <span style={{ color: '#aaa' }}>{result.meanPFlow.toFixed(3)}</span>
                    <span style={{ color: '#888' }}>CME rate:</span>
                    <span style={{ color: '#aaa' }}>{result.meanCmeRate.toFixed(2)} Vn/s</span>
                    <span style={{ color: '#888' }}>Total CME:</span>
                    <span style={{ color: '#aaa' }}>{result.totalCmeVn.toFixed(1)} Vn</span>
                    <span style={{ color: '#888' }}>Extrapolated:</span>
                    <span style={{ color: '#64B5F6' }}>
                      {(result.meanCmeRate * step.dailyMinutes * 60 / 1000).toFixed(1)}K Vn/day
                    </span>
                  </div>
                </div>
              )}

              {/* Start button */}
              {canStart && (
                <button onClick={() => handleStartStep(idx)} style={{
                  marginTop: 8, marginLeft: 32, padding: '6px 16px',
                  background: '#2a2a4e', color: '#64B5F6',
                  border: '1px solid #64B5F6', borderRadius: 6, cursor: 'pointer',
                  fontSize: 12, fontWeight: 600,
                }}>
                  Start Recording
                </button>
              )}
            </div>
          );
        })}
      </div>

      {/* Summary */}
      {allDone && (
        <div style={{
          ...card,
          border: '2px solid #4CAF50', background: '#1e3a2e',
        }}>
          <h3 style={{ margin: '0 0 8px', color: '#4CAF50', fontSize: 16 }}>
            Protocol Complete
          </h3>
          <div style={{ color: '#ccc', fontSize: 13, lineHeight: 1.6 }}>
            <div>Total recording time: <strong>{fmt(totalRecordingTime)}</strong></div>
            <div>Total windows: <strong>
              {Array.from(completedSteps.values()).reduce((s, r) => s + r.windowCount, 0)}
            </strong></div>
            <div>Extrapolated daily CME: <strong style={{ color: '#64B5F6' }}>
              {(extrapolatedDaily / 1000).toFixed(0)}K Vn
            </strong> (9.5h day)</div>
          </div>

          <div style={{
            marginTop: 12, padding: 10, background: '#111', borderRadius: 8, fontSize: 12,
          }}>
            <div style={{ color: '#aaa', marginBottom: 6, fontWeight: 600 }}>
              Per-activity extrapolation:
            </div>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 11 }}>
              <thead>
                <tr style={{ color: '#888' }}>
                  <th style={{ textAlign: 'left', padding: '3px 4px' }}>Activity</th>
                  <th style={{ textAlign: 'right', padding: '3px 4px' }}>c(t)</th>
                  <th style={{ textAlign: 'right', padding: '3px 4px' }}>Rate</th>
                  <th style={{ textAlign: 'right', padding: '3px 4px' }}>pFlow</th>
                  <th style={{ textAlign: 'right', padding: '3px 4px' }}>Daily</th>
                  <th style={{ textAlign: 'right', padding: '3px 4px' }}>Est. Vn</th>
                </tr>
              </thead>
              <tbody>
                {steps.map((step, idx) => {
                  const r = completedSteps.get(idx);
                  if (!r) return null;
                  return (
                    <tr key={step.id} style={{ borderTop: '1px solid #333' }}>
                      <td style={{ padding: '3px 4px', color: '#ccc' }}>{step.name}</td>
                      <td style={{ padding: '3px 4px', color: '#888', textAlign: 'right' }}>
                        {step.complexity}
                      </td>
                      <td style={{ padding: '3px 4px', color: '#aaa', textAlign: 'right' }}>
                        {r.meanCmeRate.toFixed(1)} Vn/s
                      </td>
                      <td style={{ padding: '3px 4px', color: '#aaa', textAlign: 'right' }}>
                        {r.meanPFlow.toFixed(3)}
                      </td>
                      <td style={{ padding: '3px 4px', color: '#888', textAlign: 'right' }}>
                        {step.dailyMinutes}m
                      </td>
                      <td style={{ padding: '3px 4px', color: '#64B5F6', textAlign: 'right', fontWeight: 600 }}>
                        {(r.meanCmeRate * step.dailyMinutes * 60 / 1000).toFixed(1)}K
                      </td>
                    </tr>
                  );
                })}
                <tr style={{ borderTop: '2px solid #4CAF50' }}>
                  <td style={{ padding: '4px', color: '#4CAF50', fontWeight: 700 }} colSpan={4}>
                    Total (9.5h day)
                  </td>
                  <td style={{ padding: '4px', color: '#888', textAlign: 'right' }}>570m</td>
                  <td style={{ padding: '4px', color: '#4CAF50', textAlign: 'right', fontWeight: 700 }}>
                    {(extrapolatedDaily / 1000).toFixed(0)}K
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div style={{
            marginTop: 12, padding: '10px 14px', background: '#1a1a2e',
            borderRadius: 8, border: '1px solid #64B5F6', color: '#64B5F6', fontSize: 12,
          }}>
            Recording complete. Send a message to process this data on quantum computer
            (Aer simulator + IBM Kingston real QPU), generate charts, and update the paper.
          </div>
        </div>
      )}
    </div>
  );
};
