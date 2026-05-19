export interface BandPowers {
  delta: number;
  theta: number;
  alpha: number;
  beta: number;
  gamma: number;
}

export interface EegWindowData {
  timestamp: string;
  channels: Record<string, BandPowers>;
  channelQuality?: Record<string, number>;
  quality: number;
  taskDifficulty: number;
  touching: boolean;
  sourceMode?: string;
}

export type WindowClass = 'clean' | 'artifact' | 'rejected' | 'calibrating';

export interface CmeResult {
  timestamp: string;
  cmeVn: number;
  cmeIndex: number;
  pFlow: number;
  classicalPFlow?: number;
  isFlow: boolean;
  eBand: number;
  shotsUsed: number;
  depth: number;
  qpuLatencyMs: number;
  totalLatencyMs: number;
  cmeSessionVn: number;
  totalWindows: number;
  taskDifficulty: number;
  channels?: Record<string, BandPowers>;
  windowClass: WindowClass;
  channelQuality?: Record<string, number>;
  actionName?: string;
  actionSlug?: string;
}

export interface CalibrationProgress {
  windowsCollected: number;
  windowsNeeded: number;
  isComplete: boolean;
  actionSlug?: string;
  actionName?: string;
}

export interface CalibrationComplete {
  kappa: number;
  featureMin: number[];
  featureMax: number[];
  actionSlug?: string;
  actionName?: string;
}

// ─── Action definitions ──────────────────────────────────────

export interface ActionTreeNode {
  id: string;
  name: string;
  slug: string;
  description?: string;
  defaultDifficulty: number;
  icon?: string;
  isSystem: boolean;
  children?: ActionTreeNode[];
}

export interface ActionStarted {
  actionDefId: string;
  actionSpikeId: string;
  name: string;
  slug: string;
  difficulty: number;
  startedAt: string;
}

export interface ActionStopped {
  actionSpikeId: string;
  name: string;
  slug: string;
  stoppedAt: string;
}

export interface ActiveAction {
  actionDefId: string;
  actionSpikeId: string;
  name: string;
  slug: string;
  difficulty: number;
  startedAt: string;
}

// ─── Energy forecast ─────────────────────────────────────────

export interface ActionEnergyBreakdown {
  actionName: string;
  actionSlug: string;
  spent: number;
  avgRatePerMin: number;
  minutes: number;
  windows: number;
}

export interface EnergyForecast {
  energySpentToday: number;
  currentRatePerMin: number;
  projectedTotal: number;
  remainingHours: number;
  totalWindows: number;
  sessionMinutes: number;
  perAction: ActionEnergyBreakdown[];
}
