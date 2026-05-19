import { useState, useEffect } from 'react'
import { Beaker, Plus, BarChart } from 'lucide-react'
import { apiClient } from '../api/client'
import type { Experiment } from '../types'
import './ExperimentsList.css'

interface Props {
  onSelectExperiment: (id: string) => void
}

export default function ExperimentsList({ onSelectExperiment }: Props) {
  const [experiments, setExperiments] = useState<Experiment[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [newExp, setNewExp] = useState({
    name: '',
    durationSeconds: 300,
    onlineArrivalRate: 2.0,
    numberOfClients: 5,
    trainingArrivalRate: 0.1,
    notes: ''
  })

  const loadExperiments = async () => {
    try {
      const data = await apiClient.listExperiments(50)
      setExperiments(data)
    } catch (err) {
      console.error('Failed to load experiments:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadExperiments()
  }, [])

  const handleCreate = async () => {
    try {
      await apiClient.createExperiment(newExp)
      setShowCreateForm(false)
      setNewExp({
        name: '',
        durationSeconds: 300,
        onlineArrivalRate: 2.0,
        numberOfClients: 5,
        trainingArrivalRate: 0.1,
        notes: ''
      })
      await loadExperiments()
    } catch (err) {
      alert('Failed to create experiment: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  return (
    <div className="experiments-list">
      <div className="list-header">
        <h2 className="list-title">
          <Beaker size={20} />
          Experiments & Performance Analysis
        </h2>
        <button onClick={() => setShowCreateForm(!showCreateForm)} className="button">
          <Plus size={16} />
          New Experiment
        </button>
      </div>

      {showCreateForm && (
        <div className="card create-form">
          <h3>Create New Experiment</h3>
          <div className="form-grid-2">
            <div className="form-group">
              <label>Experiment Name</label>
              <input
                type="text"
                className="form-input"
                value={newExp.name}
                onChange={(e) => setNewExp({...newExp, name: e.target.value})}
                placeholder="e.g., Light Load Test - 0.5 req/s"
              />
            </div>
            <div className="form-group">
              <label>Duration (seconds)</label>
              <input
                type="number"
                className="form-input"
                value={newExp.durationSeconds}
                onChange={(e) => setNewExp({...newExp, durationSeconds: parseInt(e.target.value)})}
              />
            </div>
            <div className="form-group">
              <label>Online Arrival Rate (req/s)</label>
              <input
                type="number"
                step="0.1"
                className="form-input"
                value={newExp.onlineArrivalRate}
                onChange={(e) => setNewExp({...newExp, onlineArrivalRate: parseFloat(e.target.value)})}
              />
            </div>
            <div className="form-group">
              <label>Number of Clients</label>
              <input
                type="number"
                className="form-input"
                value={newExp.numberOfClients}
                onChange={(e) => setNewExp({...newExp, numberOfClients: parseInt(e.target.value)})}
              />
            </div>
            <div className="form-group">
              <label>Training Arrival Rate (jobs/min)</label>
              <input
                type="number"
                step="0.01"
                className="form-input"
                value={newExp.trainingArrivalRate}
                onChange={(e) => setNewExp({...newExp, trainingArrivalRate: parseFloat(e.target.value)})}
              />
            </div>
            <div className="form-group">
              <label>Notes</label>
              <input
                type="text"
                className="form-input"
                value={newExp.notes}
                onChange={(e) => setNewExp({...newExp, notes: e.target.value})}
                placeholder="Optional notes"
              />
            </div>
          </div>
          <div className="form-actions">
            <button onClick={handleCreate} className="button button-success">Create Experiment</button>
            <button onClick={() => setShowCreateForm(false)} className="button button-secondary">Cancel</button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="loading-small">Loading experiments...</div>
      ) : experiments.length === 0 ? (
        <div className="empty-state">
          <Beaker size={48} />
          <p>No experiments yet. Create one to start collecting metrics.</p>
        </div>
      ) : (
        <div className="experiments-grid">
          {experiments.map((exp) => (
            <div key={exp.id} className="experiment-card" onClick={() => onSelectExperiment(exp.id)}>
              <div className="exp-card-header">
                <h3>{exp.name}</h3>
                <span className={`status-badge ${exp.status.toLowerCase()}`}>{exp.status}</span>
              </div>
              <div className="exp-card-body">
                <div className="exp-param">
                  <span className="param-label">Started:</span>
                  <span>{new Date(exp.startedAt).toLocaleString()}</span>
                </div>
                <div className="exp-param">
                  <span className="param-label">Duration:</span>
                  <span>{exp.durationSeconds}s</span>
                </div>
                <div className="exp-param">
                  <span className="param-label">Arrival Rate:</span>
                  <span>{exp.onlineArrivalRate} req/s</span>
                </div>
                <div className="exp-param">
                  <span className="param-label">Clients:</span>
                  <span>{exp.numberOfClients}</span>
                </div>
              </div>
              <div className="exp-card-footer">
                <button className="view-button">
                  <BarChart size={14} />
                  View Results
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

