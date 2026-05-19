import { useState, useEffect } from 'react'
import { Activity, Workflow, Database, BarChart3, Beaker, Gauge } from 'lucide-react'
import Tabs from './components/Tabs'
import SystemStatus from './components/SystemStatus'
import InferencePanel from './components/InferencePanel'
import TrainingPanel from './components/TrainingPanel'
import MetricsChart from './components/MetricsChart'
import RecentActivity from './components/RecentActivity'
import InfoPanel from './components/InfoPanel'
import ProcessVisualization from './components/ProcessVisualization'
import DataUpload from './components/DataUpload'
import ExperimentsList from './components/ExperimentsList'
import ExperimentResults from './components/ExperimentResults'
import ArchitecturesBench from './components/ArchitecturesBench'
import { apiClient } from './api/client'
import type { DashboardSummary } from './types'
import './App.css'

function App() {
  const [activeTab, setActiveTab] = useState('control')
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedExperimentId, setSelectedExperimentId] = useState<string | null>(null)

  const tabs = [
    { id: 'control', label: 'Control Panel', icon: <Activity size={16} /> },
    { id: 'process', label: 'Process Flow', icon: <Workflow size={16} /> },
    { id: 'data', label: 'Data Upload', icon: <Database size={16} /> },
    { id: 'experiments', label: 'Experiments', icon: <Beaker size={16} /> },
    { id: 'benchmarks', label: 'Architectures Bench', icon: <Gauge size={16} /> },
    { id: 'metrics', label: 'Analytics', icon: <BarChart3 size={16} /> },
  ]

  const fetchSummary = async () => {
    try {
      const data = await apiClient.getDashboardSummary()
      setSummary(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchSummary()
    const interval = setInterval(fetchSummary, 5000) // Refresh every 5 seconds
    return () => clearInterval(interval)
  }, [])

  return (
    <div className="app">
      {/* Header */}
      <header className="header">
        <div className="header-content">
          <div className="header-title">
            <Activity className="header-icon" size={32} />
            <div>
              <h1>CME Quantum ML System</h1>
              <p>Real-time monitoring and control dashboard</p>
            </div>
          </div>
          <div className="header-badge">
            Quantum ML Model
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="main-content">
        {loading && (
          <div className="loading-container">
            <div className="spinner"></div>
            <p>Loading dashboard...</p>
          </div>
        )}

        {error && (
          <div className="error-banner">
            <strong>Error:</strong> {error}
            <button onClick={fetchSummary} className="retry-button">
              Retry
            </button>
          </div>
        )}

        {!loading && !error && (
          <>
            {/* System Status Cards */}
            <SystemStatus summary={summary} onRefresh={fetchSummary} />

            {/* Tabbed Interface */}
            <Tabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />

            {/* Control Panel Tab */}
            {activeTab === 'control' && (
              <>
                {/* Algorithm Explanation */}
                <InfoPanel title="What's Being Computed & Optimized? (Click to expand)">
              <h4>🔬 Quantum Circuit: Variational Quantum Classifier (VQC)</h4>
              <p>
                A 4-qubit quantum circuit that classifies EEG features to predict "flow" mental state probability.
              </p>
              <div className="formula">
                Circuit Layers:<br/>
                1. Angle Encoding: EEG features → R<sub>y</sub>(θ) rotations<br/>
                2. Entangling: CNOT gates create quantum correlations<br/>
                3. Variational: Trainable R<sub>y</sub>, R<sub>z</sub> parameters<br/>
                4. Measurement: 1024 shots → p_flow
              </div>

              <h4>🎯 Metaheuristic: Evolutionary Algorithm (Genetic Algorithm Style)</h4>
              <p>
                Optimizes quantum circuit parameters (rotation angles) to maximize classification accuracy on EEG data.
              </p>
              <ul>
                <li><strong>What's Optimized:</strong> 8 rotation angles {`{θ₀, φ₀, θ₁, φ₁, θ₂, φ₂, θ₃, φ₃}`}</li>
                <li><strong>How:</strong> Generate candidates → Evaluate on quantum backend → Select best → Repeat</li>
                <li><strong>Goal:</strong> Find parameters that maximize flow state detection accuracy</li>
              </ul>

              <h4>💡 CME Formula</h4>
              <div className="formula">
                CME = k × Energy(features) × g(difficulty, p_flow)<br/>
                <br/>
                Where:<br/>
                • Energy = Σ|feature_i| (EEG activity level)<br/>
                • g = (1 + p_flow) × (0.5 + difficulty)<br/>
                • k = 10.0 (scaling constant)
              </div>

              <div className="highlight-box">
                <strong>⚠️ Note:</strong> This is an <strong>imitation model</strong> for performance analysis.
                Parameters are fixed (simulating "trained" state). Real training would update these parameters
                using actual labeled EEG data.
              </div>

            
            </InfoPanel>

            {/* Interactive Panels */}
            <div className="grid-2">
              <InferencePanel onSubmit={fetchSummary} />
              <TrainingPanel onSubmit={fetchSummary} />
            </div>

            {/* Metrics Visualization */}
            <MetricsChart summary={summary} />

            {/* Recent Activity */}
            <RecentActivity />
              </>
            )}

            {/* Process Flow Tab */}
            {activeTab === 'process' && (
              <ProcessVisualization />
            )}

            {/* Data Upload Tab */}
            {activeTab === 'data' && (
              <DataUpload />
            )}

            {/* Experiments Tab */}
            {activeTab === 'experiments' && (
              <>
                {selectedExperimentId ? (
                  <>
                    <button
                      onClick={() => setSelectedExperimentId(null)}
                      className="button button-secondary"
                      style={{marginBottom: '1rem'}}
                    >
                      ← Back to Experiments List
                    </button>
                    <ExperimentResults experimentId={selectedExperimentId} />
                  </>
                ) : (
                  <ExperimentsList onSelectExperiment={setSelectedExperimentId} />
                )}
              </>
            )}

            {/* Architectures Bench Tab */}
            {activeTab === 'benchmarks' && (
              <ArchitecturesBench />
            )}

            {/* Analytics Tab */}
            {activeTab === 'metrics' && (
              <>
                <MetricsChart summary={summary} />
                <div style={{marginTop: '1.5rem'}}>
                  <RecentActivity />
                </div>
              </>
            )}
          </>
        )}
      </main>

      {/* Footer */}
      <footer className="footer">
        <p>
          CME Quantum ML System • Imitation Model for Performance Analysis •{' '}
          <a href="https://github.com" target="_blank" rel="noopener noreferrer">
            Documentation
          </a>
        </p>
      </footer>
    </div>
  )
}

export default App

