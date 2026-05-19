import React, { useState, useEffect } from 'react';
import { useSignalR } from '../hooks/useSignalR';
import { useAuth } from '../contexts/AuthContext';
import { ConnectionStatus } from '../components/ConnectionStatus';
import { DeviceControl } from '../components/DeviceControl';
import { FlowStateGauge } from '../components/FlowStateGauge';
import { CmeTimeSeries } from '../components/CmeTimeSeries';
import { RawEegChart } from '../components/RawEegChart';
import { SpectralHeatmap } from '../components/SpectralHeatmap';
import { DataStoragePanel } from '../components/DataStoragePanel';
import { ClassicalAnalysisPanel } from '../components/ClassicalAnalysisPanel';
import { InferenceModeToggle, type InferenceMode } from '../components/InferenceModeToggle';
import { ActionSelector } from '../components/ActionSelector';
import { ActionSegments } from '../components/ActionSegments';
import { EnergyForecast } from '../components/EnergyForecast';
import { DayJournal } from '../components/DayJournal';
import { MeasurementProtocol } from '../components/MeasurementProtocol';
import { HeadTwin3D } from '../components/HeadTwin3D';

type Tab = 'live' | 'journal' | 'data' | 'analysis' | 'measure';

export default function DashboardPage() {
  const {
    status,
    sessionId,
    sessionStartTime,
    eegHistory,
    cmeHistory,
    latestCme,
    latestEeg,
    calibration,
    currentAction,
    actionStoppedVersion,
    resetSession,
    setInferenceMode,
    startSession,
    stopSession,
    startAction,
    stopAction,
  } = useSignalR();
  const { email, logout } = useAuth();
  const [tab, setTab] = useState<Tab>('live');
  const [inferenceMode, setInferenceModeState] = useState<InferenceMode>('quantum');
  const [, setTick] = useState(0);
  const [segmentVersion, setSegmentVersion] = useState(0);

  const isReceiving = cmeHistory.length > 0;
  const handleSegmentSaved = () => setSegmentVersion(v => v + 1);

  useEffect(() => {
    if (actionStoppedVersion > 0) {
      const t = setTimeout(() => setSegmentVersion(v => v + 1), 1500);
      return () => clearTimeout(t);
    }
  }, [actionStoppedVersion]);

  useEffect(() => {
    const interval = setInterval(() => setTick(t => t + 1), 1000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    setInferenceMode(inferenceMode);
  }, [inferenceMode, setInferenceMode]);

  const handleInferenceModeChange = (mode: InferenceMode) => {
    setInferenceModeState(mode);
  };

  const tabs: { id: Tab; label: string }[] = [
    { id: 'live', label: 'Live CME' },
    { id: 'measure', label: 'Measure' },
    { id: 'journal', label: 'Day Journal' },
    { id: 'data', label: 'Data & Actions' },
    { id: 'analysis', label: 'Analysis' },
  ];

  return (
    <div style={{
      minHeight: '100vh', background: '#0d0d1a', color: '#eee',
      fontFamily: "'Segoe UI', system-ui, -apple-system, sans-serif",
    }}>
      <ConnectionStatus
        status={status}
        sessionId={sessionId}
        sessionStartTime={sessionStartTime}
        totalWindows={cmeHistory.length}
        onStartSession={startSession}
        onStopSession={stopSession}
      />

      <div style={{ padding: '0 16px 16px', maxWidth: 1400, margin: '0 auto' }}>
        <div style={{
          display: 'flex', gap: 4, marginBottom: 16, borderBottom: '1px solid #333',
        }}>
          {tabs.map(({ id, label }) => (
            <button
              key={id}
              onClick={() => setTab(id)}
              style={{
                padding: '12px 20px', background: 'none', color: tab === id ? '#64B5F6' : '#888',
                border: 'none', borderBottom: tab === id ? '2px solid #64B5F6' : '2px solid transparent',
                cursor: 'pointer', fontSize: 13, marginBottom: -1,
              }}
            >
              {label}
            </button>
          ))}
        </div>

        {/* ── TAB 1: LIVE CME ─────────────────────────────────── */}
        {tab === 'live' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 16 }}>
            {/* Main column: CME chart + EEG */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <CmeTimeSeries history={cmeHistory} calibration={calibration} currentAction={currentAction} />
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                <FlowStateGauge latest={latestCme} />
                <EnergyForecast sessionId={sessionId} totalWindows={cmeHistory.length} />
              </div>
              <HeadTwin3D latestEeg={latestEeg} latestCme={latestCme} />
              <ActionSegments sessionId={sessionId} refreshKey={segmentVersion} />
              <RawEegChart history={eegHistory} />
              <SpectralHeatmap latest={latestEeg} />
            </div>

            {/* Sidebar: controls + activity + energy */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <InferenceModeToggle value={inferenceMode} onChange={handleInferenceModeChange} />
              <DeviceControl
                hubStatus={status}
                isReceiving={isReceiving}
                totalWindows={cmeHistory.length}
                onReset={resetSession}
                sessionId={sessionId}
                onStopSession={stopSession}
              />
              <ActionSelector
                currentAction={currentAction}
                onStartAction={startAction}
                onStopAction={stopAction}
                sessionId={sessionId}
                onSegmentSaved={handleSegmentSaved}
              />
            </div>
          </div>
        )}

        {/* ── TAB: MEASUREMENT PROTOCOL ─────────────────────── */}
        {tab === 'measure' && (
          <MeasurementProtocol
            sessionId={sessionId}
            currentAction={currentAction}
            onStartSession={startSession}
            onStopSession={stopSession}
            onStartAction={startAction}
            onStopAction={stopAction}
          />
        )}

        {/* ── TAB 2: DAY JOURNAL ─────────────────────────────── */}
        {tab === 'journal' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 16 }}>
            <DayJournal refreshKey={segmentVersion} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <ActionSelector
                currentAction={currentAction}
                onStartAction={startAction}
                onStopAction={stopAction}
                sessionId={sessionId}
                onSegmentSaved={handleSegmentSaved}
              />
              <EnergyForecast
                sessionId={sessionId}
                totalWindows={cmeHistory.length}
              />
            </div>
          </div>
        )}

        {/* ── TAB 3: DATA & ACTIONS ──────────────────────────── */}
        {tab === 'data' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 340px', gap: 16 }}>
            {/* Main column: chart + action analysis */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <CmeTimeSeries history={cmeHistory} calibration={calibration} currentAction={currentAction} />
              <ActionSegments sessionId={sessionId} refreshKey={segmentVersion} />
            </div>

            {/* Sidebar: recording + annotation + energy */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <ActionSelector
                currentAction={currentAction}
                onStartAction={startAction}
                onStopAction={stopAction}
                sessionId={sessionId}
                onSegmentSaved={handleSegmentSaved}
              />
              <DataStoragePanel
                sessionId={sessionId}
                totalWindows={cmeHistory.length}
                onStartSession={startSession}
                onStopSession={stopSession}
              />
              <EnergyForecast
                sessionId={sessionId}
                totalWindows={cmeHistory.length}
              />
            </div>
          </div>
        )}

        {/* ── TAB 3: ANALYSIS ────────────────────────────────── */}
        {tab === 'analysis' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 340px', gap: 16 }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <ClassicalAnalysisPanel onStopSession={stopSession} />
            </div>
            <div style={{ color: '#888', fontSize: 12 }}>
              <p>Run classical NN analysis on stored EEG windows to generate flow labels.</p>
              <p>Select a session to analyze, or use &quot;All sessions&quot; for the full dataset.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
