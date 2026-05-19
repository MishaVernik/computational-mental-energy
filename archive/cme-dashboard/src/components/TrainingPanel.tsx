import { useState } from 'react'
import { Cpu, Loader } from 'lucide-react'
import { apiClient } from '../api/client'
import type { TrainingJob } from '../types'
import './TrainingPanel.css'

interface Props {
  onSubmit: () => void
}

const algorithms = [
  { id: 'genetic', name: 'Genetic Algorithm', description: 'Evolution-based: selection, crossover, mutation' },
  { id: 'pso', name: 'Particle Swarm Optimization', description: 'Swarm intelligence: particles explore search space' },
  { id: 'aco', name: 'Ant Colony Optimization', description: 'Ant foraging: pheromone trails guide search' },
  { id: 'simulated_annealing', name: 'Simulated Annealing', description: 'Temperature-based probabilistic search' },
]

export default function TrainingPanel({ onSubmit }: Props) {
  const [totalGenerations, setTotalGenerations] = useState(10)
  const [algorithm, setAlgorithm] = useState('genetic')
  const [loading, setLoading] = useState(false)
  const [job, setJob] = useState<TrainingJob | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setJob(null)

    try {
      const response = await apiClient.startTrainingJob({ totalGenerations, algorithm })
      setJob(response)
      onSubmit()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start training job')
    } finally {
      setLoading(false)
    }
  }

  const selectedAlgorithm = algorithms.find(a => a.id === algorithm)

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

  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          <Cpu size={20} />
          Training Jobs
        </h2>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label">
            Metaheuristic Algorithm
            <span className="form-hint">
              Choose optimization algorithm for circuit parameter search
            </span>
          </label>
          <select
            className="form-select"
            value={algorithm}
            onChange={(e) => setAlgorithm(e.target.value)}
          >
            {algorithms.map((alg) => (
              <option key={alg.id} value={alg.id}>
                {alg.name}
              </option>
            ))}
          </select>
          {selectedAlgorithm && (
            <div className="algorithm-description">
              <strong>{selectedAlgorithm.name}:</strong> {selectedAlgorithm.description}
            </div>
          )}
        </div>

        <div className="form-group">
          <label className="form-label">
            Total Generations: {totalGenerations}
            <span className="form-hint">
              Number of optimization iterations (more = better convergence, but slower)
            </span>
          </label>
          <input
            type="range"
            className="form-slider"
            min="5"
            max="50"
            step="5"
            value={totalGenerations}
            onChange={(e) => setTotalGenerations(parseInt(e.target.value))}
          />
          <div className="slider-hint">
            <strong>What's being optimized:</strong> 8 quantum circuit parameters (rotation angles: α₀-α₃, β₀-β₃) 
            to maximize flow state detection accuracy on labeled EEG data. Each generation evaluates 5 candidate parameter sets.
          </div>
        </div>

        <button type="submit" className="button" disabled={loading}>
          {loading ? (
            <>
              <Loader size={16} className="spin" />
              Starting Job...
            </>
          ) : (
            <>
              <Cpu size={16} />
              Start Training Job
            </>
          )}
        </button>
      </form>

      {error && (
        <div className="result-box error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {job && (
        <div className="result-box success">
          <h4>Training Job Created</h4>
          <div className="job-details">
            <div className="job-detail-row">
              <span className="job-label">Job ID:</span>
              <code className="job-code">{job.id}</code>
            </div>
            <div className="job-detail-row">
              <span className="job-label">Status:</span>
              <span className={getStatusClass(job.status)}>{job.status}</span>
            </div>
            <div className="job-detail-row">
              <span className="job-label">Total Generations:</span>
              <span>{job.totalGenerations}</span>
            </div>
            <div className="job-detail-row">
              <span className="job-label">Created:</span>
              <span>{new Date(job.createdAt).toLocaleString()}</span>
            </div>
          </div>
          <div className="job-hint">
            The background worker will process this job automatically. Check "Recent Activity" below for updates.
          </div>
        </div>
      )}
    </div>
  )
}

