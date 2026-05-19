import { useState, useEffect } from 'react'
import { Beaker, Download, Save, TrendingUp, Cpu, Activity, AlertTriangle } from 'lucide-react'
import { apiClient } from '../api/client'
import type { ExperimentMetrics } from '../types'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts'
import './ExperimentResults.css'

interface Props {
  experimentId: string
}

export default function ExperimentResults({ experimentId }: Props) {
  const [metrics, setMetrics] = useState<ExperimentMetrics | null>(null)
  const [experiment, setExperiment] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showModelForm, setShowModelForm] = useState(false)
  const [modelInputs, setModelInputs] = useState({
    modelAvgLatencyMs: '',
    modelP95LatencyMs: '',
    modelThroughputReqPerSec: '',
    modelQpuUtilization: '',
    modelAvgJobDurationSec: '',
    notes: ''
  })

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 10000) // Refresh every 10 seconds
    return () => clearInterval(interval)
  }, [experimentId])

  const loadData = async () => {
    try {
      const [expData, metricsData] = await Promise.all([
        apiClient.getExperiment(experimentId),
        apiClient.getExperimentMetrics(experimentId)
      ])
      setExperiment(expData)
      setMetrics(metricsData)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data')
    } finally {
      setLoading(false)
    }
  }

  const handleSaveModelMetrics = async () => {
    try {
      await apiClient.saveModelMetrics(experimentId, {
        modelAvgLatencyMs: parseFloat(modelInputs.modelAvgLatencyMs),
        modelP95LatencyMs: modelInputs.modelP95LatencyMs ? parseFloat(modelInputs.modelP95LatencyMs) : undefined,
        modelThroughputReqPerSec: parseFloat(modelInputs.modelThroughputReqPerSec),
        modelQpuUtilization: parseFloat(modelInputs.modelQpuUtilization),
        modelAvgJobDurationSec: modelInputs.modelAvgJobDurationSec ? parseFloat(modelInputs.modelAvgJobDurationSec) : undefined,
        notes: modelInputs.notes
      })
      setShowModelForm(false)
      await loadData() // Reload to show comparison
    } catch (err) {
      alert('Failed to save model metrics: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  const handleExport = async () => {
    try {
      const blob = await apiClient.exportExperimentMetrics(experimentId)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `experiment_${experimentId}_metrics.csv`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (err) {
      alert('Failed to export: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner"></div>
        <p>Loading experiment results...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="error-banner">
        <AlertTriangle size={20} />
        <span><strong>Error:</strong> {error}</span>
      </div>
    )
  }

  if (!metrics || !experiment) {
    return <div className="error-banner">No data available</div>
  }

  const latencyHistogramData = metrics.inference.latencyHistogram
    ? Object.entries(metrics.inference.latencyHistogram).map(([name, value]) => ({ name, value }))
    : []

  const qpuTypeData = [
    { name: 'Inference', value: metrics.qpu.inferenceCalls, color: '#3b82f6' },
    { name: 'Training', value: metrics.qpu.trainingCalls, color: '#10b981' }
  ]

  return (
    <div className="experiment-results">
      {/* Header */}
      <div className="results-header">
        <div className="header-info">
          <Beaker size={32} className="header-icon" />
          <div>
            <h1>{experiment.name}</h1>
            <p className="experiment-meta">
              Experiment ID: <code>{experimentId}</code> • 
              Status: <span className={`status-badge ${experiment.status.toLowerCase()}`}>{experiment.status}</span>
            </p>
            <p className="experiment-params">
              Duration: {experiment.durationSeconds}s • 
              Arrival Rate: {experiment.onlineArrivalRate} req/s • 
              Clients: {experiment.numberOfClients}
            </p>
          </div>
        </div>
        <button onClick={handleExport} className="button button-secondary">
          <Download size={16} />
          Export CSV
        </button>
      </div>

      {/* Summary Cards */}
      <div className="summary-cards">
        <div className="metric-card">
          <div className="metric-icon" style={{color: '#3b82f6'}}>
            <Activity size={24} />
          </div>
          <div className="metric-content">
            <div className="metric-label">Avg Latency</div>
            <div className="metric-value">{metrics.inference.avgLatencyMs.toFixed(0)} ms</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon" style={{color: '#f59e0b'}}>
            <TrendingUp size={24} />
          </div>
          <div className="metric-content">
            <div className="metric-label">P95 Latency</div>
            <div className="metric-value">{metrics.inference.p95LatencyMs.toFixed(0)} ms</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon" style={{color: '#10b981'}}>
            <Activity size={24} />
          </div>
          <div className="metric-content">
            <div className="metric-label">Throughput</div>
            <div className="metric-value">{metrics.inference.throughputReqPerSec.toFixed(2)} req/s</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon" style={{color: '#8b5cf6'}}>
            <Cpu size={24} />
          </div>
          <div className="metric-content">
            <div className="metric-label">QPU Utilization</div>
            <div className="metric-value">{(metrics.qpu.qpuUtilization * 100).toFixed(1)}%</div>
          </div>
        </div>
      </div>

      {/* Detailed Sections */}
      <div className="metrics-grid">
        {/* Online Inference */}
        <div className="card">
          <h3 className="section-title">📊 Online Inference Metrics</h3>
          <table className="metrics-table">
            <tbody>
              <tr>
                <td>Total Requests</td>
                <td className="metric-value-cell">{metrics.inference.totalRequests}</td>
              </tr>
              <tr>
                <td>Success Count</td>
                <td className="metric-value-cell success">{metrics.inference.successCount}</td>
              </tr>
              <tr>
                <td>Error Count</td>
                <td className="metric-value-cell error">{metrics.inference.errorCount}</td>
              </tr>
              <tr>
                <td>Error Rate</td>
                <td className="metric-value-cell">{(metrics.inference.errorRate * 100).toFixed(2)}%</td>
              </tr>
              <tr className="separator">
                <td>Avg Latency</td>
                <td className="metric-value-cell highlight">{metrics.inference.avgLatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>Min Latency</td>
                <td className="metric-value-cell">{metrics.inference.minLatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>Max Latency</td>
                <td className="metric-value-cell">{metrics.inference.maxLatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>P50 (Median)</td>
                <td className="metric-value-cell">{metrics.inference.p50LatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>P90</td>
                <td className="metric-value-cell">{metrics.inference.p90LatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>P95</td>
                <td className="metric-value-cell highlight">{metrics.inference.p95LatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>P99</td>
                <td className="metric-value-cell">{metrics.inference.p99LatencyMs.toFixed(2)} ms</td>
              </tr>
              <tr className="separator">
                <td>Throughput</td>
                <td className="metric-value-cell highlight">{metrics.inference.throughputReqPerSec.toFixed(3)} req/s</td>
              </tr>
            </tbody>
          </table>

          {latencyHistogramData.length > 0 && (
            <div className="chart-container">
              <h4>Latency Distribution</h4>
              <ResponsiveContainer width="100%" height={200}>
                <BarChart data={latencyHistogramData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                  <XAxis dataKey="name" stroke="#94a3b8" />
                  <YAxis stroke="#94a3b8" />
                  <Tooltip
                    contentStyle={{
                      background: '#1e293b',
                      border: '1px solid #334155',
                      borderRadius: '0.5rem',
                      color: '#e2e8f0',
                    }}
                  />
                  <Bar dataKey="value" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>

        {/* QPU Metrics */}
        <div className="card">
          <h3 className="section-title">⚛️ QPU Utilization Metrics</h3>
          <table className="metrics-table">
            <tbody>
              <tr>
                <td>Total QPU Calls</td>
                <td className="metric-value-cell">{metrics.qpu.totalQpuCalls}</td>
              </tr>
              <tr>
                <td>Avg Call Duration</td>
                <td className="metric-value-cell">{metrics.qpu.avgQpuCallDurationMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>Min Call Duration</td>
                <td className="metric-value-cell">{metrics.qpu.minQpuCallDurationMs.toFixed(2)} ms</td>
              </tr>
              <tr>
                <td>Max Call Duration</td>
                <td className="metric-value-cell">{metrics.qpu.maxQpuCallDurationMs.toFixed(2)} ms</td>
              </tr>
              <tr className="separator">
                <td>Total Busy Time</td>
                <td className="metric-value-cell">{(metrics.qpu.totalQpuBusyMs / 1000).toFixed(2)} s</td>
              </tr>
              <tr>
                <td>QPU Utilization</td>
                <td className="metric-value-cell highlight">{(metrics.qpu.qpuUtilization * 100).toFixed(2)}%</td>
              </tr>
              <tr className="separator">
                <td>Inference Calls</td>
                <td className="metric-value-cell">{metrics.qpu.inferenceCalls}</td>
              </tr>
              <tr>
                <td>Training Calls</td>
                <td className="metric-value-cell">{metrics.qpu.trainingCalls}</td>
              </tr>
              <tr>
                <td>Inference Time</td>
                <td className="metric-value-cell">{(metrics.qpu.qpuBusyMsInference / 1000).toFixed(2)} s</td>
              </tr>
              <tr>
                <td>Training Time</td>
                <td className="metric-value-cell">{(metrics.qpu.qpuBusyMsTraining / 1000).toFixed(2)} s</td>
              </tr>
            </tbody>
          </table>

          {metrics.qpu.totalQpuCalls > 0 && (
            <div className="chart-container">
              <h4>QPU Time Usage</h4>
              <ResponsiveContainer width="100%" height={200}>
                <PieChart>
                  <Pie
                    data={qpuTypeData}
                    cx="50%"
                    cy="50%"
                    outerRadius={60}
                    fill="#8884d8"
                    dataKey="value"
                    label={(entry) => `${entry.name}: ${entry.value}`}
                  >
                    {qpuTypeData.map((entry, index) => (
                      <Cell key={index} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      </div>

      {/* Training Metrics */}
      {metrics.training.totalJobs > 0 && (
        <div className="card">
          <h3 className="section-title">🔧 Training Job Metrics</h3>
          <div className="training-summary">
            <div className="training-stat">
              <span className="stat-label">Total Jobs:</span>
              <span className="stat-value">{metrics.training.totalJobs}</span>
            </div>
            <div className="training-stat">
              <span className="stat-label">Completed:</span>
              <span className="stat-value success">{metrics.training.completedJobs}</span>
            </div>
            <div className="training-stat">
              <span className="stat-label">Failed:</span>
              <span className="stat-value error">{metrics.training.failedJobs}</span>
            </div>
            <div className="training-stat">
              <span className="stat-label">Completion Rate:</span>
              <span className="stat-value">{(metrics.training.completionRate * 100).toFixed(1)}%</span>
            </div>
            <div className="training-stat">
              <span className="stat-label">Avg Duration:</span>
              <span className="stat-value">{metrics.training.avgJobDurationSec.toFixed(2)} s</span>
            </div>
            <div className="training-stat">
              <span className="stat-label">P95 Duration:</span>
              <span className="stat-value">{metrics.training.p95JobDurationSec.toFixed(2)} s</span>
            </div>
          </div>

          {metrics.training.byAlgorithm && Object.keys(metrics.training.byAlgorithm).length > 0 && (
            <div className="algorithm-comparison">
              <h4>Algorithm Comparison</h4>
              <table className="metrics-table">
                <thead>
                  <tr>
                    <th>Algorithm</th>
                    <th>Jobs</th>
                    <th>Avg Duration (s)</th>
                    <th>Avg Fitness</th>
                    <th>Total QPU Calls</th>
                  </tr>
                </thead>
                <tbody>
                  {Object.entries(metrics.training.byAlgorithm).map(([alg, stats]) => (
                    <tr key={alg}>
                      <td><span className="algorithm-badge">{alg}</span></td>
                      <td>{stats.jobCount}</td>
                      <td>{stats.avgDurationSec.toFixed(2)}</td>
                      <td>{stats.avgBestFitness.toFixed(3)}</td>
                      <td>{stats.totalQpuCalls}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Model Comparison */}
      <div className="card">
        <div className="section-header">
          <h3 className="section-title">🎯 Petri Net vs Real System Comparison</h3>
          {!metrics.comparison && (
            <button onClick={() => setShowModelForm(!showModelForm)} className="button button-secondary">
              <Save size={16} />
              {showModelForm ? 'Cancel' : 'Enter Model Metrics'}
            </button>
          )}
        </div>

        {showModelForm && (
          <div className="model-form">
            <p className="form-info">
              Enter metrics from your PetriObjModelPaint / CPN Tools simulation:
            </p>
            <div className="form-grid">
              <div className="form-group">
                <label>Model Avg Latency (ms)</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-input"
                  value={modelInputs.modelAvgLatencyMs}
                  onChange={(e) => setModelInputs({...modelInputs, modelAvgLatencyMs: e.target.value})}
                  placeholder="e.g., 1187"
                />
              </div>
              <div className="form-group">
                <label>Model P95 Latency (ms)</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-input"
                  value={modelInputs.modelP95LatencyMs}
                  onChange={(e) => setModelInputs({...modelInputs, modelP95LatencyMs: e.target.value})}
                  placeholder="e.g., 2298"
                />
              </div>
              <div className="form-group">
                <label>Model Throughput (req/s)</label>
                <input
                  type="number"
                  step="0.001"
                  className="form-input"
                  value={modelInputs.modelThroughputReqPerSec}
                  onChange={(e) => setModelInputs({...modelInputs, modelThroughputReqPerSec: e.target.value})}
                  placeholder="e.g., 1.95"
                />
              </div>
              <div className="form-group">
                <label>Model QPU Utilization (0-1)</label>
                <input
                  type="number"
                  step="0.001"
                  min="0"
                  max="1"
                  className="form-input"
                  value={modelInputs.modelQpuUtilization}
                  onChange={(e) => setModelInputs({...modelInputs, modelQpuUtilization: e.target.value})}
                  placeholder="e.g., 0.85"
                />
              </div>
              <div className="form-group">
                <label>Model Avg Job Duration (s) (optional)</label>
                <input
                  type="number"
                  step="0.01"
                  className="form-input"
                  value={modelInputs.modelAvgJobDurationSec}
                  onChange={(e) => setModelInputs({...modelInputs, modelAvgJobDurationSec: e.target.value})}
                  placeholder="e.g., 45.5"
                />
              </div>
              <div className="form-group">
                <label>Notes</label>
                <input
                  type="text"
                  className="form-input"
                  value={modelInputs.notes}
                  onChange={(e) => setModelInputs({...modelInputs, notes: e.target.value})}
                  placeholder="e.g., PetriObjModelPaint sim with 10 replications"
                />
              </div>
            </div>
            <button onClick={handleSaveModelMetrics} className="button button-success">
              <Save size={16} />
              Save and Calculate MAPE
            </button>
          </div>
        )}

        {metrics.comparison && (
          <div className="comparison-results">
            <div className={`verdict-banner ${getVerdictClass(metrics.comparison.overallMape)}`}>
              <strong>Overall MAPE: {(metrics.comparison.overallMape * 100).toFixed(2)}%</strong>
              <span>{metrics.comparison.verdict}</span>
            </div>

            <table className="comparison-table">
              <thead>
                <tr>
                  <th>Metric</th>
                  <th>Real System</th>
                  <th>Petri Net Model</th>
                  <th>MAPE (%)</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>Avg Latency (ms)</td>
                  <td>{metrics.inference.avgLatencyMs.toFixed(2)}</td>
                  <td>{metrics.comparison.modelAvgLatencyMs.toFixed(2)}</td>
                  <td className={getMapeClass(metrics.comparison.mapeLatency)}>
                    {(metrics.comparison.mapeLatency * 100).toFixed(2)}%
                  </td>
                  <td>{getMapeStatus(metrics.comparison.mapeLatency)}</td>
                </tr>
                {metrics.comparison.modelP95LatencyMs && (
                  <tr>
                    <td>P95 Latency (ms)</td>
                    <td>{metrics.inference.p95LatencyMs.toFixed(2)}</td>
                    <td>{metrics.comparison.modelP95LatencyMs.toFixed(2)}</td>
                    <td className={getMapeClass(metrics.comparison.mapeP95Latency)}>
                      {(metrics.comparison.mapeP95Latency * 100).toFixed(2)}%
                    </td>
                    <td>{getMapeStatus(metrics.comparison.mapeP95Latency)}</td>
                  </tr>
                )}
                <tr>
                  <td>Throughput (req/s)</td>
                  <td>{metrics.inference.throughputReqPerSec.toFixed(3)}</td>
                  <td>{metrics.comparison.modelThroughputReqPerSec.toFixed(3)}</td>
                  <td className={getMapeClass(metrics.comparison.mapeThroughput)}>
                    {(metrics.comparison.mapeThroughput * 100).toFixed(2)}%
                  </td>
                  <td>{getMapeStatus(metrics.comparison.mapeThroughput)}</td>
                </tr>
                <tr>
                  <td>QPU Utilization</td>
                  <td>{(metrics.qpu.qpuUtilization * 100).toFixed(2)}%</td>
                  <td>{(metrics.comparison.modelQpuUtilization * 100).toFixed(2)}%</td>
                  <td className={getMapeClass(metrics.comparison.mapeQpuUtilization)}>
                    {(metrics.comparison.mapeQpuUtilization * 100).toFixed(2)}%
                  </td>
                  <td>{getMapeStatus(metrics.comparison.mapeQpuUtilization)}</td>
                </tr>
                {metrics.comparison.mapeJobDuration !== null && metrics.comparison.mapeJobDuration !== undefined && (
                  <tr>
                    <td>Avg Job Duration (s)</td>
                    <td>{metrics.training.avgJobDurationSec.toFixed(2)}</td>
                    <td>{metrics.comparison.modelAvgJobDurationSec?.toFixed(2)}</td>
                    <td className={getMapeClass(metrics.comparison.mapeJobDuration)}>
                      {(metrics.comparison.mapeJobDuration * 100).toFixed(2)}%
                    </td>
                    <td>{getMapeStatus(metrics.comparison.mapeJobDuration)}</td>
                  </tr>
                )}
              </tbody>
            </table>

            <div className="comparison-footer">
              <p>
                <strong>MAPE Interpretation:</strong> &lt;10% = Excellent, 10-20% = Good, &gt;20% = Needs Refinement
              </p>
            </div>
          </div>
        )}

        {!metrics.comparison && !showModelForm && (
          <div className="no-comparison">
            <p>No Petri net model metrics entered yet.</p>
            <p>Click "Enter Model Metrics" to add results from your simulation for comparison.</p>
          </div>
        )}
      </div>
    </div>
  )
}

function getVerdictClass(mape: number): string {
  if (mape < 0.10) return 'excellent'
  if (mape < 0.20) return 'good'
  return 'needs-work'
}

function getMapeClass(mape: number): string {
  if (mape < 0.10) return 'mape-excellent'
  if (mape < 0.20) return 'mape-good'
  return 'mape-poor'
}

function getMapeStatus(mape: number): string {
  if (mape < 0.10) return '✅ Excellent'
  if (mape < 0.20) return '✓ Good'
  return '⚠️ High Error'
}

