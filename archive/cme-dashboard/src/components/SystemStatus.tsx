import { useState, useEffect } from 'react'
import { Server, Activity, Cpu, Database, RefreshCw, Zap } from 'lucide-react'
import { apiClient } from '../api/client'
import type { DashboardSummary } from '../types'
import './SystemStatus.css'

interface Props {
  summary: DashboardSummary | null
  onRefresh: () => void
}

export default function SystemStatus({ summary, onRefresh }: Props) {
  const [activeModel, setActiveModel] = useState<any>(null)
  
  useEffect(() => {
    const loadActiveModel = async () => {
      try {
        const jobs = await apiClient.listTrainingJobs(50)
        const active = jobs.find((j: any) => j.isActiveModel)
        setActiveModel(active)
      } catch {
        // Ignore errors
      }
    }
    loadActiveModel()
    const interval = setInterval(loadActiveModel, 10000)
    return () => clearInterval(interval)
  }, [])
  const stats = [
    {
      icon: <Activity size={24} />,
      label: 'Total Requests',
      value: summary?.totalInferenceRequests || 0,
      color: '#3b82f6',
    },
    {
      icon: <Server size={24} />,
      label: 'Active Sessions',
      value: summary?.totalSessions || 0,
      color: '#10b981',
    },
    {
      icon: <Cpu size={24} />,
      label: 'Avg Response Time',
      value: summary ? `${Math.round(summary.averageResponseTimeMs)}ms` : '-',
      color: '#f59e0b',
    },
    {
      icon: <Database size={24} />,
      label: 'Training Jobs',
      value: Object.values(summary?.trainingJobsByStatus || {}).reduce((a, b) => a + b, 0),
      color: '#8b5cf6',
    },
  ]

  return (
    <div className="system-status">
      <div className="status-header">
        <h2 className="status-title">System Overview</h2>
        <button onClick={onRefresh} className="button button-secondary refresh-button">
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <div className="stats-grid">
        {stats.map((stat, index) => (
          <div key={index} className="stat-card" style={{ borderTopColor: stat.color }}>
            <div className="stat-icon" style={{ color: stat.color }}>
              {stat.icon}
            </div>
            <div className="stat-content">
              <div className="stat-label">{stat.label}</div>
              <div className="stat-value">{stat.value}</div>
            </div>
          </div>
        ))}
      </div>

      {summary && (
        <div className="performance-metrics">
          <h3>Performance Metrics</h3>
          <div className="metrics-row">
            <div className="metric">
              <span className="metric-label">Average CME:</span>
              <span className="metric-value">{summary.averageCme.toFixed(2)}</span>
            </div>
            <div className="metric">
              <span className="metric-label">P95 Latency:</span>
              <span className="metric-value">{Math.round(summary.p95ResponseTimeMs)}ms</span>
            </div>
            <div className="metric">
              <span className="metric-label">P99 Latency:</span>
              <span className="metric-value">{Math.round(summary.p99ResponseTimeMs)}ms</span>
            </div>
          </div>
        </div>
      )}
      
      {activeModel && (
        <div className="active-model-banner">
          <div className="banner-icon">
            <Zap size={18} />
          </div>
          <div className="banner-content">
            <div className="banner-title">
              🧠 Active Trained Model
            </div>
            <div className="banner-info">
              <span>Algorithm: <strong>{activeModel.algorithm}</strong></span>
              <span>•</span>
              <span>Best Fitness: <strong>{activeModel.bestFitness?.toFixed(3) || 'N/A'}</strong></span>
              <span>•</span>
              <span>Trained: <strong>{new Date(activeModel.completedAt).toLocaleDateString()}</strong></span>
            </div>
            <div className="banner-hint">
              All inference requests now use this trained model's parameters
            </div>
          </div>
        </div>
      )}
      
      {!activeModel && summary && summary.totalInferenceRequests > 0 && (
        <div className="no-model-banner">
          <div className="banner-icon">
            <Zap size={18} />
          </div>
          <div className="banner-content">
            <div className="banner-title">
              ⚠️ Using Default Parameters
            </div>
            <div className="banner-hint">
              No trained model available. Start a training job to optimize the quantum circuit!
            </div>
          </div>
        </div>
      )}
    </div>
  )
}


