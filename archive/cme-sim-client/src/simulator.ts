/**
 * Load simulator for CME system.
 * 
 * Generates realistic traffic patterns:
 * - Online inference requests (Poisson-like arrivals)
 * - Training job submissions
 * - Measures latency, throughput, failures
 */
import { CmeApiClient } from './api-client.js';
import type { SimulationConfig, RequestMetrics, SimulationResults } from './types.js';

export class LoadSimulator {
  private apiClient: CmeApiClient;
  private config: SimulationConfig;
  private onlineMetrics: RequestMetrics[] = [];
  private trainingJobs: string[] = [];
  private sessionIds: string[] = [];
  private running = false;
  private startTime = 0;

  constructor(config: SimulationConfig) {
    this.config = config;
    this.apiClient = new CmeApiClient(config.apiBaseUrl);
    
    // Generate session IDs for parallel clients
    this.sessionIds = Array.from(
      { length: config.clients },
      (_, i) => `sim-session-${Date.now()}-${i}`
    );
  }

  async run(): Promise<SimulationResults> {
    console.log('\n=== CME Simulation Started ===');
    console.log(`Duration: ${this.config.duration}s | Online Rate: ${this.config.onlineRate} req/s | Training Rate: ${this.config.trainRate} jobs/min | Clients: ${this.config.clients}\n`);

    this.running = true;
    this.startTime = Date.now();

    // Start parallel tasks
    const tasks: Promise<void>[] = [];

    // Online inference traffic
    for (let i = 0; i < this.config.clients; i++) {
      tasks.push(this.runOnlineInferenceLoop(this.sessionIds[i]));
    }

    // Training job submissions
    if (this.config.trainRate > 0) {
      tasks.push(this.runTrainingJobLoop());
    }

    // Progress reporter
    tasks.push(this.runProgressReporter());

    // Wait for simulation duration
    await this.sleep(this.config.duration * 1000);
    this.running = false;

    // Wait for all tasks to finish
    await Promise.allSettled(tasks);

    return this.generateResults();
  }

  private async runOnlineInferenceLoop(sessionId: string): Promise<void> {
    const intervalMs = 1000 / (this.config.onlineRate / this.config.clients);
    let windowCounter = 0;

    while (this.running) {
      const requestStart = Date.now();

      try {
        // Generate random EEG features
        const features = this.generateRandomFeatures();
        const taskDifficulty = Math.random();

        const response = await this.apiClient.computeCme({
          sessionId,
          windowId: `window-${windowCounter++}`,
          features,
          taskDifficulty,
        });

        const latency = Date.now() - requestStart;

        this.onlineMetrics.push({
          timestamp: requestStart,
          latency,
          success: true,
        });

      } catch (error) {
        const latency = Date.now() - requestStart;
        this.onlineMetrics.push({
          timestamp: requestStart,
          latency,
          success: false,
          error: error instanceof Error ? error.message : 'Unknown error',
        });
      }

      // Wait for next request (approximate Poisson process)
      await this.sleep(intervalMs);
    }
  }

  private async runTrainingJobLoop(): Promise<void> {
    const intervalMs = (60 * 1000) / this.config.trainRate;

    while (this.running) {
      try {
        const response = await this.apiClient.startTrainingJob();
        this.trainingJobs.push(response.id);
        console.log(`[${this.getElapsedSeconds()}s] Training job started: ${response.id}`);
      } catch (error) {
        console.error('Failed to start training job:', error);
      }

      await this.sleep(intervalMs);
    }
  }

  private async runProgressReporter(): Promise<void> {
    while (this.running) {
      await this.sleep(5000); // Report every 5 seconds

      const elapsed = this.getElapsedSeconds();
      const onlineCount = this.onlineMetrics.length;
      const successCount = this.onlineMetrics.filter(m => m.success).length;
      const recentLatencies = this.onlineMetrics.slice(-20).filter(m => m.success).map(m => m.latency);
      const avgLatency = recentLatencies.length > 0
        ? Math.round(recentLatencies.reduce((a, b) => a + b, 0) / recentLatencies.length)
        : 0;

      console.log(
        `[${elapsed.toString().padStart(5)}s] Online: ${onlineCount} (${successCount} ok) | ` +
        `Training: ${this.trainingJobs.length} | Avg latency: ${avgLatency}ms`
      );
    }
  }

  private async generateResults(): Promise<SimulationResults> {
    const duration = (Date.now() - this.startTime) / 1000;

    // Calculate online inference metrics
    const successfulRequests = this.onlineMetrics.filter(m => m.success);
    const latencies = successfulRequests.map(m => m.latency).sort((a, b) => a - b);

    const avgLatency = latencies.length > 0
      ? latencies.reduce((a, b) => a + b, 0) / latencies.length
      : 0;

    const p95Latency = this.calculatePercentile(latencies, 0.95);
    const p99Latency = this.calculatePercentile(latencies, 0.99);
    const throughput = successfulRequests.length / duration;

    // Calculate training job metrics
    let completedCount = 0;
    let runningCount = 0;
    let failedCount = 0;
    const completionTimes: number[] = [];

    for (const jobId of this.trainingJobs) {
      try {
        const job = await this.apiClient.getTrainingJob(jobId);
        if (job.status === 'Completed') {
          completedCount++;
          if (job.startedAt && job.completedAt) {
            const duration = new Date(job.completedAt).getTime() - new Date(job.startedAt).getTime();
            completionTimes.push(duration / 1000);
          }
        } else if (job.status === 'Running') {
          runningCount++;
        } else if (job.status === 'Failed') {
          failedCount++;
        }
      } catch {
        // Job might still be queued or API error
      }
    }

    const avgCompletionTime = completionTimes.length > 0
      ? completionTimes.reduce((a, b) => a + b, 0) / completionTimes.length
      : 0;

    return {
      onlineMetrics: {
        total: this.onlineMetrics.length,
        successful: successfulRequests.length,
        failed: this.onlineMetrics.length - successfulRequests.length,
        avgLatency: Math.round(avgLatency),
        p95Latency: Math.round(p95Latency),
        p99Latency: Math.round(p99Latency),
        throughput: Math.round(throughput * 100) / 100,
        latencies,
      },
      trainingMetrics: {
        total: this.trainingJobs.length,
        completed: completedCount,
        running: runningCount,
        failed: failedCount,
        avgCompletionTime: Math.round(avgCompletionTime * 10) / 10,
      },
      duration,
    };
  }

  private generateRandomFeatures(): number[] {
    // Generate 8 random features in range [-1, 1]
    return Array.from({ length: 8 }, () => Math.random() * 2 - 1);
  }

  private calculatePercentile(sortedValues: number[], percentile: number): number {
    if (sortedValues.length === 0) return 0;

    const index = Math.ceil(sortedValues.length * percentile) - 1;
    const clampedIndex = Math.max(0, Math.min(sortedValues.length - 1, index));

    return sortedValues[clampedIndex];
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private getElapsedSeconds(): number {
    return Math.floor((Date.now() - this.startTime) / 1000);
  }
}


