#!/usr/bin/env node

/**
 * CME Simulation Client - CLI Entry Point
 * 
 * Generates load on the CME quantum ML system and measures performance.
 */
import { Command } from 'commander';
import { LoadSimulator } from './simulator.js';
import type { SimulationConfig } from './types.js';

const program = new Command();

program
  .name('cme-sim-client')
  .description('Load simulation client for CME quantum ML system')
  .version('1.0.0');

program
  .option('-d, --duration <seconds>', 'Simulation duration in seconds', '60')
  .option('-o, --onlineRate <rate>', 'Online inference requests per second', '1')
  .option('-t, --trainRate <rate>', 'Training jobs per minute', '0.1')
  .option('-c, --clients <count>', 'Number of parallel client sessions', '1')
  .option('-u, --url <url>', 'API base URL', 'http://localhost:5000')
  .parse(process.argv);

const options = program.opts();

const config: SimulationConfig = {
  apiBaseUrl: options.url,
  duration: parseInt(options.duration),
  onlineRate: parseFloat(options.onlineRate),
  trainRate: parseFloat(options.trainRate),
  clients: parseInt(options.clients),
};

// Validate configuration
if (config.duration <= 0) {
  console.error('Error: Duration must be positive');
  process.exit(1);
}

if (config.onlineRate < 0 || config.trainRate < 0) {
  console.error('Error: Rates must be non-negative');
  process.exit(1);
}

if (config.clients <= 0) {
  console.error('Error: Number of clients must be positive');
  process.exit(1);
}

// Run simulation
const simulator = new LoadSimulator(config);

simulator
  .run()
  .then(results => {
    console.log('\n=== Simulation Complete ===\n');

    console.log('Online Inference Metrics:');
    console.log(`  Total requests:    ${results.onlineMetrics.total}`);
    console.log(`  Successful:        ${results.onlineMetrics.successful}`);
    console.log(`  Failed:            ${results.onlineMetrics.failed}`);
    console.log(`  Avg latency:       ${results.onlineMetrics.avgLatency} ms`);
    console.log(`  P95 latency:       ${results.onlineMetrics.p95Latency} ms`);
    console.log(`  P99 latency:       ${results.onlineMetrics.p99Latency} ms`);
    console.log(`  Throughput:        ${results.onlineMetrics.throughput} req/s`);

    console.log('\nTraining Job Metrics:');
    console.log(`  Total submitted:   ${results.trainingMetrics.total}`);
    console.log(`  Completed:         ${results.trainingMetrics.completed}`);
    console.log(`  Running:           ${results.trainingMetrics.running}`);
    console.log(`  Failed:            ${results.trainingMetrics.failed}`);
    if (results.trainingMetrics.avgCompletionTime > 0) {
      console.log(`  Avg completion:    ${results.trainingMetrics.avgCompletionTime} s`);
    }

    console.log(`\nTotal duration: ${Math.round(results.duration)} seconds\n`);
  })
  .catch(error => {
    console.error('\n=== Simulation Failed ===');
    console.error(error);
    process.exit(1);
  });


