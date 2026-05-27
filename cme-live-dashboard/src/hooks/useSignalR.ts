import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import type {
  EegWindowData, CmeResult, CalibrationProgress, CalibrationComplete,
  ActiveAction, ActionStarted, ActionStopped
} from '../types';

import { getHubUrl } from '../runtimeApi';

const HUB_URL = getHubUrl();
const MAX_HISTORY = 300;

export function useSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');
  const [eegHistory, setEegHistory] = useState<EegWindowData[]>([]);
  const [cmeHistory, setCmeHistory] = useState<CmeResult[]>([]);
  const [latestCme, setLatestCme] = useState<CmeResult | null>(null);
  const [latestEeg, setLatestEeg] = useState<EegWindowData | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [sessionStartTime, setSessionStartTime] = useState<number | null>(null);
  const [calibration, setCalibration] = useState<CalibrationProgress | null>(null);
  const [calibrationResult, setCalibrationResult] = useState<CalibrationComplete | null>(null);
  const [currentAction, setCurrentAction] = useState<ActiveAction | null>(null);
  const [actionStoppedVersion, setActionStoppedVersion] = useState(0);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on('ReceiveRawEeg', (data: EegWindowData) => {
      setLatestEeg(data);
      setEegHistory(prev => {
        const next = [...prev, data];
        return next.length > MAX_HISTORY ? next.slice(-MAX_HISTORY) : next;
      });
    });

    connection.on('ReceiveCmeResult', (result: CmeResult) => {
      setLatestCme(result);
      setCmeHistory(prev => {
        const next = [...prev, result];
        return next.length > MAX_HISTORY ? next.slice(-MAX_HISTORY) : next;
      });
    });

    connection.on('CalibrationProgress', (data: CalibrationProgress) => {
      setCalibration(data);
    });

    connection.on('CalibrationComplete', (data: CalibrationComplete) => {
      setCalibrationResult(data);
      setCalibration({ windowsCollected: 24, windowsNeeded: 24, isComplete: true,
        actionSlug: data.actionSlug, actionName: data.actionName });
    });

    connection.on('ActionStarted', (data: ActionStarted) => {
      setCurrentAction({
        actionDefId: data.actionDefId,
        actionSpikeId: data.actionSpikeId,
        name: data.name,
        slug: data.slug,
        difficulty: data.difficulty,
        startedAt: data.startedAt,
      });
    });

    connection.on('ActionStopped', (_data: ActionStopped) => {
      setCurrentAction(null);
      setActionStoppedVersion(v => v + 1);
    });

    connection.on('Connected', () => {
      console.log('Hub confirmed connection');
    });

    connection.on('SessionStarted', (data: { sessionId: string }) => {
      console.log('Session started:', data.sessionId);
      setSessionId(data.sessionId);
      setSessionStartTime(Date.now());
      setCalibration(null);
      setCalibrationResult(null);
      setCurrentAction(null);
    });

    connection.on('SessionEnded', (data: { sessionId: string | null; message?: string }) => {
      console.log('Session ended:', data.message ?? data.sessionId);
      setSessionId(null);
      setSessionStartTime(null);
      setCurrentAction(null);
    });

    connection.on('Error', (err: { message: string }) => {
      console.error('Hub error:', err.message);
    });

    connection.onreconnecting(() => setStatus('connecting'));
    connection.onreconnected(() => setStatus('connected'));
    connection.onclose(() => setStatus('disconnected'));

    const start = async () => {
      setStatus('connecting');
      try {
        await connection.start();
        setStatus('connected');
        console.log('SignalR connected');
      } catch (err) {
        console.error('SignalR connection error:', err);
        setStatus('disconnected');
        setTimeout(start, 3000);
      }
    };

    connectionRef.current = connection;
    start();

    return () => {
      connection.stop();
    };
  }, []);

  const resetSession = useCallback(() => {
    setCmeHistory([]);
    setEegHistory([]);
    setLatestCme(null);
    setLatestEeg(null);
    setCalibration(null);
    setCalibrationResult(null);
    setCurrentAction(null);
  }, []);

  const setInferenceMode = useCallback((mode: 'classical' | 'quantum' | 'hybrid') => {
    const conn = connectionRef.current;
    if (conn?.state === signalR.HubConnectionState.Connected) {
      conn.invoke('SetInferenceMode', mode).catch(err => console.error('SetInferenceMode failed:', err));
    }
  }, []);

  const startSession = useCallback(() => {
    const conn = connectionRef.current;
    if (conn?.state === signalR.HubConnectionState.Connected) {
      conn.invoke('StartSession', 'muse-athena-user').catch(err => console.error('StartSession failed:', err));
    }
  }, []);

  const stopSession = useCallback((sid?: string | null) => {
    const conn = connectionRef.current;
    if (conn?.state === signalR.HubConnectionState.Connected) {
      conn.invoke('StopSession', sid ?? '').catch(err => console.error('StopSession failed:', err));
    }
  }, []);

  const startAction = useCallback((actionDefId: string, description?: string) => {
    const conn = connectionRef.current;
    if (conn?.state === signalR.HubConnectionState.Connected) {
      conn.invoke('StartAction', actionDefId, description ?? null)
        .catch(err => console.error('StartAction failed:', err));
    }
  }, []);

  const stopAction = useCallback(() => {
    const conn = connectionRef.current;
    if (conn?.state === signalR.HubConnectionState.Connected) {
      conn.invoke('StopAction').catch(err => console.error('StopAction failed:', err));
    }
  }, []);

  return {
    status,
    sessionId,
    sessionStartTime,
    eegHistory,
    cmeHistory,
    latestCme,
    latestEeg,
    calibration,
    calibrationResult,
    currentAction,
    actionStoppedVersion,
    resetSession,
    setInferenceMode,
    startSession,
    stopSession,
    startAction,
    stopAction,
  };
}
