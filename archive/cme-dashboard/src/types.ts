export interface DashboardSummary {
  totalInferenceRequests: number
  averageCme: number
  averageResponseTimeMs: number
  p95ResponseTimeMs: number
  p99ResponseTimeMs: number
  trainingJobsByStatus: Record<string, number>
  totalSessions: number
}

export interface InferenceRequest {
  sessionId: string
  windowId: string
  features: number[]
  taskDifficulty: number
}

export interface InferenceResponse {
  cme: number
  pFlow: number
  shotsUsed: number
  depth: number
  qpuLatencyMs: number
  totalLatencyMs: number
}

export interface TrainingJob {
  id: string
  status: string
  createdAt: string
  startedAt?: string
  completedAt?: string
  totalGenerations: number
  completedGenerations: number
  bestFitness?: number
  totalQpuCalls: number
  errorMessage?: string
}

export interface StartTrainingRequest {
  totalGenerations?: number
  algorithm?: string
}

export interface RequestMetrics {
  timestamp: number
  latency: number
  success: boolean
  error?: string
}

export interface SimulationResults {
  onlineMetrics: {
    total: number
    successful: number
    failed: number
    avgLatency: number
    p95Latency: number
    p99Latency: number
    throughput: number
    latencies: number[]
  }
  trainingMetrics: {
    total: number
    completed: number
    running: number
    failed: number
    avgCompletionTime: number
  }
  duration: number
}

// Experiment tracking types
export interface Experiment {
  id: string
  name: string
  startedAt: string
  finishedAt?: string
  durationSeconds: number
  onlineArrivalRate: number
  numberOfClients: number
  trainingArrivalRate: number
  status: string
  notes?: string
}

export interface CreateExperimentRequest {
  name: string
  durationSeconds: number
  onlineArrivalRate: number
  numberOfClients: number
  trainingArrivalRate: number
  notes?: string
}

export interface InferenceMetrics {
  totalRequests: number
  successCount: number
  errorCount: number
  errorRate: number
  avgLatencyMs: number
  minLatencyMs: number
  maxLatencyMs: number
  p50LatencyMs: number
  p90LatencyMs: number
  p95LatencyMs: number
  p99LatencyMs: number
  throughputReqPerSec: number
  latencyHistogram?: Record<string, number>
}

export interface QpuMetrics {
  totalQpuCalls: number
  avgQpuCallDurationMs: number
  minQpuCallDurationMs: number
  maxQpuCallDurationMs: number
  totalQpuBusyMs: number
  qpuUtilization: number
  inferenceCalls: number
  trainingCalls: number
  qpuBusyMsInference: number
  qpuBusyMsTraining: number
}

export interface TrainingMetrics {
  totalJobs: number
  completedJobs: number
  failedJobs: number
  runningJobs: number
  completionRate: number
  avgJobDurationSec: number
  minJobDurationSec: number
  maxJobDurationSec: number
  p50JobDurationSec: number
  p90JobDurationSec: number
  p95JobDurationSec: number
  byAlgorithm?: Record<string, AlgorithmStats>
}

export interface AlgorithmStats {
  jobCount: number
  avgDurationSec: number
  avgBestFitness: number
  totalQpuCalls: number
}

export interface ComparisonMetrics {
  modelAvgLatencyMs: number
  modelP95LatencyMs?: number
  modelThroughputReqPerSec: number
  modelQpuUtilization: number
  modelAvgJobDurationSec?: number
  mapeLatency: number
  mapeP95Latency: number
  mapeThroughput: number
  mapeQpuUtilization: number
  mapeJobDuration?: number
  overallMape: number
  verdict: string
}

export interface ExperimentMetrics {
  experimentId: string
  timeWindowMs: number
  inference: InferenceMetrics
  qpu: QpuMetrics
  training: TrainingMetrics
  comparison?: ComparisonMetrics
}

export interface SaveModelMetricsRequest {
  modelAvgLatencyMs: number
  modelP95LatencyMs?: number
  modelThroughputReqPerSec: number
  modelQpuUtilization: number
  modelAvgJobDurationSec?: number
  notes?: string
}

// CME Metrics from Excel
export interface GlobalMetricsDto {
  totalSessions: number
  meanCmeSession: number
  medianCmeSession: number
  meanFlowShare: number
  sessionsFlowShareGe05: number
  sessionsFlowShareGe07: number
  k: number
  wDelta: number
  wTheta: number
  wAlpha: number
  wBeta: number
  lambda1: number
  lambda2: number
  lambda3: number
  flowThreshold: number
}

export interface SessionMetricsDto {
  sessionId: string
  userId?: string
  totalWindows: number
  totalDurationSeconds: number
  flowWindows: number
  flowDurationSeconds: number
  flowShare: number
  longestFlowStreakSeconds: number
  avgCme: number
  maxCme: number
  cmeSession: number
  flowPeriods: FlowStatePeriodDto[]
  windowDetails: WindowMetricsDto[]
}

export interface FlowStatePeriodDto {
  startTime: string
  endTime: string
  durationSeconds: number
  avgCme: number
  avgPFlow: number
}

export interface WindowMetricsDto {
  timestamp: string
  cme: number
  pFlow: number
  isFlow: boolean
}

export interface CmeMetricsResponse {
  globalSummary: GlobalMetricsDto
  sessionSummaries: SessionMetricsDto[]
}
