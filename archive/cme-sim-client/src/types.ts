/**
 * Type definitions for CME simulation client.
 */

export interface SimulationConfig {
  apiBaseUrl: string;
  duration: number; // seconds
  onlineRate: number; // requests per second
  trainRate: number; // jobs per minute
  clients: number; // number of parallel clients/sessions
}

export interface InferenceRequest {
  sessionId: string;
  windowId: string;
  features: number[];
  taskDifficulty: number;
}

export interface InferenceResponse {
  cme: number;
  pFlow: number;
  shotsUsed: number;
  depth: number;
  qpuLatencyMs: number;
  totalLatencyMs: number;
}

export interface TrainingJobResponse {
  id: string;
  status: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  totalGenerations: number;
  completedGenerations: number;
  bestFitness?: number;
  totalQpuCalls: number;
}

export interface RequestMetrics {
  timestamp: number;
  latency: number;
  success: boolean;
  error?: string;
}

export interface SimulationResults {
  onlineMetrics: {
    total: number;
    successful: number;
    failed: number;
    avgLatency: number;
    p95Latency: number;
    p99Latency: number;
    throughput: number;
    latencies: number[];
  };
  trainingMetrics: {
    total: number;
    completed: number;
    running: number;
    failed: number;
    avgCompletionTime: number;
  };
  duration: number;
}


