import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { TrendingUp } from 'lucide-react'
import type { DashboardSummary } from '../types'
import './MetricsChart.css'

interface Props {
  summary: DashboardSummary | null
}

export default function MetricsChart({ summary }: Props) {
  const latencyData = [
    {
      name: 'Average',
      value: summary?.averageResponseTimeMs || 0,
    },
    {
      name: 'P95',
      value: summary?.p95ResponseTimeMs || 0,
    },
    {
      name: 'P99',
      value: summary?.p99ResponseTimeMs || 0,
    },
  ]

  const jobsData = Object.entries(summary?.trainingJobsByStatus || {}).map(([status, count]) => ({
    name: status,
    count,
  }))

  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          <TrendingUp size={20} />
          Performance Metrics
        </h2>
      </div>

      <div className="charts-container">
        {/* Latency Chart */}
        <div className="chart-section">
          <h3 className="chart-title">Response Time Distribution (ms)</h3>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={latencyData}>
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

        {/* Training Jobs Chart */}
        {jobsData.length > 0 && (
          <div className="chart-section">
            <h3 className="chart-title">Training Jobs by Status</h3>
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={jobsData}>
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
                <Bar dataKey="count" fill="#10b981" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>

      {summary && summary.totalInferenceRequests > 0 && (
        <div className="metrics-summary">
          <div className="summary-item">
            <span className="summary-label">Total Requests Processed:</span>
            <span className="summary-value">{summary.totalInferenceRequests}</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Average CME Value:</span>
            <span className="summary-value">{summary.averageCme.toFixed(2)}</span>
          </div>
          <div className="summary-item">
            <span className="summary-label">Throughput:</span>
            <span className="summary-value">
              {summary.totalInferenceRequests > 0
                ? `~${(summary.totalInferenceRequests / (summary.averageResponseTimeMs / 1000)).toFixed(2)} req/s`
                : 'N/A'}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

