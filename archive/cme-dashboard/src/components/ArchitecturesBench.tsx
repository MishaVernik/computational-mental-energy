import { useState, useEffect } from 'react'
import { apiClient } from '../api/client'
import { Play, FileJson, FileSpreadsheet, Loader2 } from 'lucide-react'
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  CartesianGrid
} from 'recharts'
import './ArchitecturesBench.css'

interface BenchmarkScenario {
  name: string
  architecture: 'A_Monolith' | 'B_SyncMicroservices' | 'C_Brokered'
  activeClients: number
  requestsPerClient: number
  workersCount: number
  maxConcurrentQpuCalls: number
  qpuBackends?: number
  shots?: number
  circuitDepth?: number
  thinkTimeMs?: number
  selected: boolean
}

interface BenchmarkResult {
  runId: string
  name: string
  architecture: string
  status: any
  avgLatencyMs: number
  p95LatencyMs: number
  p99LatencyMs: number
  throughputRps: number
  failRate: number
  successCount: number
  failCount: number
}

export default function ArchitecturesBench() {
  const [scenarios, setScenarios] = useState<BenchmarkScenario[]>([
    {
      name: 'Monolith - Low Load',
      architecture: 'A_Monolith',
      activeClients: 5,
      requestsPerClient: 20,
      workersCount: 1,
      maxConcurrentQpuCalls: 1,
      qpuBackends: 1,
      shots: 256,
      circuitDepth: 4,
      thinkTimeMs: 100,
      selected: false
    },
    {
      name: 'Sync Microservices - Low Load',
      architecture: 'B_SyncMicroservices',
      activeClients: 5,
      requestsPerClient: 20,
      workersCount: 1,
      maxConcurrentQpuCalls: 1,
      qpuBackends: 1,
      shots: 256,
      circuitDepth: 4,
      thinkTimeMs: 100,
      selected: false
    },
    {
      name: 'Brokered - Low Load',
      architecture: 'C_Brokered',
      activeClients: 5,
      requestsPerClient: 20,
      workersCount: 4,
      maxConcurrentQpuCalls: 2,
      qpuBackends: 1,
      shots: 256,
      circuitDepth: 4,
      thinkTimeMs: 100,
      selected: false
    },
    {
      name: 'Monolith - Burst (More Clients, Shots)',
      architecture: 'A_Monolith',
      activeClients: 40,
      requestsPerClient: 20,
      workersCount: 4,
      maxConcurrentQpuCalls: 2,
      qpuBackends: 1,
      shots: 512,
      circuitDepth: 4,
      thinkTimeMs: 10,
      selected: false
    },
    {
      name: 'Monolith - Dual QPUs High Shots',
      architecture: 'A_Monolith',
      activeClients: 12,
      requestsPerClient: 40,
      workersCount: 2,
      maxConcurrentQpuCalls: 2,
      qpuBackends: 2,
      shots: 2048,
      circuitDepth: 6,
      thinkTimeMs: 50,
      selected: false
    },
    {
      name: 'Sync Microservices - High Requests',
      architecture: 'B_SyncMicroservices',
      activeClients: 25,
      requestsPerClient: 80,
      workersCount: 4,
      maxConcurrentQpuCalls: 4,
      qpuBackends: 2,
      shots: 1024,
      circuitDepth: 5,
      thinkTimeMs: 25,
      selected: false
    },
    {
      name: 'Brokered - Wide Workers',
      architecture: 'C_Brokered',
      activeClients: 30,
      requestsPerClient: 60,
      workersCount: 16,
      maxConcurrentQpuCalls: 8,
      qpuBackends: 2,
      shots: 512,
      circuitDepth: 4,
      thinkTimeMs: 20,
      selected: false
    },
    {
      name: 'Brokered - Heavy Shots',
      architecture: 'C_Brokered',
      activeClients: 12,
      requestsPerClient: 40,
      workersCount: 8,
      maxConcurrentQpuCalls: 4,
      qpuBackends: 1,
      shots: 4096,
      circuitDepth: 6,
      thinkTimeMs: 30,
      selected: false
    }
  ])
  const [results, setResults] = useState<BenchmarkResult[]>([])
  const [running, setRunning] = useState(false)
  const [loading, setLoading] = useState(false)
  const [levelFilter, setLevelFilter] = useState<'all' | 'low' | 'mid' | 'high'>('all')

  useEffect(() => {
    loadHistory()
  }, [])

  const loadHistory = async () => {
    try {
      setLoading(true)
      const history = await apiClient.getBenchmarkHistory(20)
      setResults(history)
    } catch (err) {
      console.error('Failed to load benchmark history', err)
    } finally {
      setLoading(false)
    }
  }

  const handleRunSelected = async () => {
    const selectedScenarios = scenarios.filter(s => s.selected)
    if (selectedScenarios.length === 0) {
      alert('Please select at least one scenario')
      return
    }

    setRunning(true)
    try {
      for (const scenario of selectedScenarios) {
        const config = {
          name: scenario.name,
          architecture: scenario.architecture,
          activeClients: scenario.activeClients,
          requestsPerClient: scenario.requestsPerClient,
          workersCount: scenario.workersCount,
          maxConcurrentQpuCalls: scenario.maxConcurrentQpuCalls,
          thinkTimeMs: scenario.thinkTimeMs ?? 100,
          qpuBackends: scenario.qpuBackends ?? 1,
          shots: scenario.shots ?? 256,
          circuitDepth: scenario.circuitDepth ?? 4,
          trainingEnabled: false,
          networkProfile: { meanMs: 5.0, stdMs: 2.0 },
          dbProfile: { meanMs: 10.0, stdMs: 3.0 },
          brokerProfile: { meanMs: 2.0, stdMs: 1.0, mode: 'Exponential' }
        }
        await apiClient.startBenchmark(config)
      }
      // Wait a bit then refresh
      setTimeout(() => {
        loadHistory()
      }, 2000)
    } catch (err) {
      console.error('Failed to start benchmarks', err)
      alert('Failed to start benchmarks: ' + (err instanceof Error ? err.message : 'Unknown error'))
    } finally {
      setRunning(false)
    }
  }

  const handleRunAll = async () => {
    setScenarios(scenarios.map(s => ({ ...s, selected: true })))
    await handleRunSelected()
  }

  const handleExport = async (runId: string, format: 'json' | 'csv') => {
    try {
      const blob = await apiClient.exportBenchmark(runId, format)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `benchmark_${runId}.${format}`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)
    } catch (err) {
      console.error('Failed to export benchmark', err)
      alert('Failed to export benchmark')
    }
  }

  const toggleScenario = (index: number) => {
    setScenarios(scenarios.map((s, i) => 
      i === index ? { ...s, selected: !s.selected } : s
    ))
  }

  const getArchitectureLabel = (arch: string) => {
    switch (arch) {
      case 'A_Monolith': return 'Monolith'
      case 'B_SyncMicroservices': return 'Sync Microservices'
      case 'C_Brokered': return 'Brokered'
      case '0': return 'Monolith'
      case '1': return 'Sync Microservices'
      case '2': return 'Brokered'
      default: return arch
    }
  }

  const normalizeStatus = (status: any) => {
    if (status === 0 || status === '0') return 'Pending'
    if (status === 1 || status === '1') return 'Running'
    if (status === 2 || status === '2') return 'Completed'
    if (status === 3 || status === '3') return 'Failed'
    return status?.toString?.() ?? ''
  }

  const getStatusColor = (status: any) => {
    switch (status) {
      case 'Completed': return '#10b981'
      case 'Running': return '#3b82f6'
      case 'Failed': return '#ef4444'
      case 2: return '#10b981'
      case 1: return '#3b82f6'
      case 3: return '#ef4444'
      default: return '#6b7280'
    }
  }

  const getLevel = (name: string): 'low' | 'mid' | 'high' | 'unknown' => {
    const n = name.toLowerCase()
    if (n.includes('low')) return 'low'
    if (n.includes('mid')) return 'mid'
    if (n.includes('high')) return 'high'
    return 'unknown'
  }

  const filteredResults = results.filter(r => {
    if (levelFilter === 'all') return true
    return getLevel(r.name) === levelFilter
  })

  const runningResults = results.filter(r => normalizeStatus(r.status) === 'Running')

  return (
    <div className="architectures-bench">
      <div className="bench-header">
        <h2>Architectures Benchmark</h2>
        <p>Run load tests across different architectures and compare performance metrics</p>
      </div>

      {/* Scenario Builder */}
      <div className="bench-section">
        <h3>Scenario Builder</h3>
        <div className="scenarios-table">
          <table>
            <thead>
              <tr>
                <th style={{ width: '40px' }}>Select</th>
                <th>Name</th>
                <th>Architecture</th>
                <th>Clients</th>
                <th>Requests/Client</th>
                <th>Workers</th>
                <th>Max QPU Calls</th>
              <th>QPU Backends</th>
              <th>Shots</th>
              </tr>
            </thead>
            <tbody>
              {scenarios.map((scenario, index) => (
                <tr key={index}>
                  <td>
                    <input
                      type="checkbox"
                      checked={scenario.selected}
                      onChange={() => toggleScenario(index)}
                    />
                  </td>
                  <td>{scenario.name}</td>
                  <td>{getArchitectureLabel(scenario.architecture)}</td>
                  <td>{scenario.activeClients}</td>
                  <td>{scenario.requestsPerClient}</td>
                  <td>{scenario.workersCount}</td>
                  <td>{scenario.maxConcurrentQpuCalls}</td>
                <td>{scenario.qpuBackends ?? 1}</td>
                <td>{scenario.shots ?? 256}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="bench-actions">
          <button
            className="button button-primary"
            onClick={handleRunSelected}
            disabled={running || scenarios.filter(s => s.selected).length === 0}
          >
            {running ? <Loader2 className="spinner" size={16} /> : <Play size={16} />}
            Run Selected
          </button>
          <button
            className="button button-secondary"
            onClick={handleRunAll}
            disabled={running}
          >
            {running ? <Loader2 className="spinner" size={16} /> : <Play size={16} />}
            Run All
          </button>
        </div>
      </div>

      {/* Results Table */}
      <div className="bench-section">
        <h3>Results</h3>
        {loading ? (
          <div className="loading">Loading results...</div>
        ) : (
          <div className="results-table">
            <div className="bench-filters">
              <div className="filter-group">
                <span>View:</span>
                {(['all', 'low', 'mid', 'high'] as const).map(level => (
                  <button
                    key={level}
                    className={`button button-chip ${levelFilter === level ? 'active' : ''}`}
                    onClick={() => setLevelFilter(level)}
                  >
                    {level === 'all' ? 'All' : level.toUpperCase()}
                  </button>
                ))}
              </div>
              {runningResults.length > 0 && (
                <div className="running-indicator">
                  <Loader2 className="spinner inline" size={14} />
                  <span>{runningResults.length} run(s) in progress:</span>
                  <span className="running-list">
                    {runningResults.map(r => r.name).join(', ')}
                  </span>
                </div>
              )}
            </div>
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Architecture</th>
                  <th>Status</th>
                  <th>Avg Latency (ms)</th>
                  <th>P95 Latency (ms)</th>
                  <th>P99 Latency (ms)</th>
                  <th>Throughput (req/s)</th>
                  <th>Fail Rate</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredResults.length === 0 ? (
                  <tr>
                    <td colSpan={9} style={{ textAlign: 'center', padding: '2rem' }}>
                      No benchmark results yet. Run a scenario to see results.
                    </td>
                  </tr>
                ) : (
                  filteredResults.map((result) => (
                    <tr key={result.runId}>
                      <td>{result.name}</td>
                      <td>{getArchitectureLabel(result.architecture)}</td>
                      <td>
                        <span
                          className="status-badge"
                          style={{ backgroundColor: getStatusColor(result.status) }}
                        >
                          {normalizeStatus(result.status)}
                        </span>
                      </td>
                      <td>{result.avgLatencyMs.toFixed(2)}</td>
                      <td>{result.p95LatencyMs.toFixed(2)}</td>
                      <td>{result.p99LatencyMs.toFixed(2)}</td>
                      <td>{result.throughputRps.toFixed(2)}</td>
                      <td>{(result.failRate * 100).toFixed(1)}%</td>
                      <td>
                        <div className="action-buttons">
                          <button
                            className="icon-button"
                            onClick={() => handleExport(result.runId, 'json')}
                            title="Export JSON"
                          >
                            <FileJson size={16} />
                          </button>
                          <button
                            className="icon-button"
                            onClick={() => handleExport(result.runId, 'csv')}
                            title="Export CSV"
                          >
                            <FileSpreadsheet size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Charts */}
      {filteredResults.length > 0 && (
        <div className="bench-section">
          <h3>Charts</h3>
          <div className="charts-grid">
            <div className="chart-card">
              <h4>Throughput (req/s)</h4>
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={filteredResults}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} interval={0} angle={-20} textAnchor="end" height={70} />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="throughputRps" name="Throughput" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            </div>
            <div className="chart-card">
              <h4>Latency (ms)</h4>
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={filteredResults}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} interval={0} angle={-20} textAnchor="end" height={70} />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="avgLatencyMs" name="Avg" fill="#10b981" />
                  <Bar dataKey="p95LatencyMs" name="P95" fill="#f59e0b" />
                  <Bar dataKey="p99LatencyMs" name="P99" fill="#ef4444" />
                </BarChart>
              </ResponsiveContainer>
            </div>
            <div className="chart-card">
              <h4>Fail Rate (%)</h4>
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={filteredResults}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} interval={0} angle={-20} textAnchor="end" height={70} />
                  <YAxis tickFormatter={(v) => `${(v * 100).toFixed(0)}%`} />
                  <Tooltip formatter={(value: number) => `${(value * 100).toFixed(2)}%`} />
                  <Legend />
                  <Bar dataKey="failRate" name="Fail Rate" fill="#ef4444" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

