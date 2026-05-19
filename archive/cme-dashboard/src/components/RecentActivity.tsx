import { useState, useEffect } from 'react'
import { Clock, Loader } from 'lucide-react'
import { apiClient } from '../api/client'
import type { TrainingJob } from '../types'
import './RecentActivity.css'

export default function RecentActivity() {
  const [jobs, setJobs] = useState<TrainingJob[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const fetchJobs = async () => {
    try {
      const data = await apiClient.listTrainingJobs(10)
      setJobs(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch jobs')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchJobs()
    const interval = setInterval(fetchJobs, 10000) // Refresh every 10 seconds
    return () => clearInterval(interval)
  }, [])

  const getStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'status-badge completed'
      case 'running':
        return 'status-badge running'
      case 'queued':
        return 'status-badge queued'
      case 'failed':
        return 'status-badge failed'
      default:
        return 'status-badge'
    }
  }

  const formatDuration = (job: TrainingJob) => {
    if (!job.startedAt) return 'Not started'
    const start = new Date(job.startedAt).getTime()
    const end = job.completedAt ? new Date(job.completedAt).getTime() : Date.now()
    const duration = Math.round((end - start) / 1000)
    return `${duration}s`
  }

  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          <Clock size={20} />
          Recent Training Jobs
        </h2>
        <button onClick={fetchJobs} className="button button-secondary small-button">
          Refresh
        </button>
      </div>

      {loading && (
        <div className="activity-loading">
          <Loader size={24} className="spin" />
          <span>Loading jobs...</span>
        </div>
      )}

      {error && (
        <div className="activity-error">
          {error}
        </div>
      )}

      {!loading && !error && jobs.length === 0 && (
        <div className="activity-empty">
          No training jobs yet. Submit a job using the Training Jobs panel above.
        </div>
      )}

      {!loading && !error && jobs.length > 0 && (
        <div className="jobs-table">
          <div className="table-header">
            <div className="table-cell">Job ID</div>
            <div className="table-cell">Algorithm</div>
            <div className="table-cell">Status</div>
            <div className="table-cell">Progress</div>
            <div className="table-cell">Duration</div>
            <div className="table-cell">QPU Calls</div>
            <div className="table-cell">Best Fitness</div>
          </div>
          {jobs.map((job) => (
            <div key={job.id} className="table-row">
              <div className="table-cell">
                <code className="job-id">{job.id.slice(0, 8)}...</code>
              </div>
              <div className="table-cell">
                <span className="algorithm-badge">
                  {(job as any).algorithm || 'genetic'}
                </span>
              </div>
              <div className="table-cell">
                <span className={getStatusClass(job.status)}>{job.status}</span>
              </div>
              <div className="table-cell">
                <div className="progress-container">
                  <div
                    className="progress-bar"
                    style={{
                      width: `${(job.completedGenerations / job.totalGenerations) * 100}%`,
                    }}
                  />
                  <span className="progress-text">
                    {job.completedGenerations}/{job.totalGenerations}
                  </span>
                </div>
              </div>
              <div className="table-cell">{formatDuration(job)}</div>
              <div className="table-cell">{job.totalQpuCalls}</div>
              <div className="table-cell">
                {job.bestFitness ? job.bestFitness.toFixed(3) : '-'}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}


