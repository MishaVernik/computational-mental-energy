import { useState } from 'react'
import { Zap, Loader } from 'lucide-react'
import { apiClient } from '../api/client'
import type { InferenceResponse } from '../types'
import './InferencePanel.css'

interface Props {
  onSubmit: () => void
}

export default function InferencePanel({ onSubmit }: Props) {
  const [sessionId, setSessionId] = useState('11111111-1111-1111-1111-111111111111')
  const [windowId, setWindowId] = useState(`window-${Date.now()}`)
  const [taskDifficulty, setTaskDifficulty] = useState(0.5)
  const [features, setFeatures] = useState('[0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]')
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<InferenceResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setResult(null)

    try {
      const featuresArray = JSON.parse(features) as number[]
      const response = await apiClient.submitInference({
        sessionId,
        windowId,
        features: featuresArray,
        taskDifficulty,
      })
      setResult(response)
      setWindowId(`window-${Date.now()}`) // Generate new window ID
      onSubmit()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit inference')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          <Zap size={20} />
          Online Inference
        </h2>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label className="form-label">
            Session ID
            <span className="form-hint">EEG recording session identifier</span>
          </label>
          <input
            type="text"
            className="form-input"
            value={sessionId}
            onChange={(e) => setSessionId(e.target.value)}
            placeholder="Session GUID"
            required
          />
        </div>

        <div className="form-group">
          <label className="form-label">
            Window ID
            <span className="form-hint">Unique ID for this 1-second EEG segment</span>
          </label>
          <input
            type="text"
            className="form-input"
            value={windowId}
            onChange={(e) => setWindowId(e.target.value)}
            placeholder="Window identifier"
            required
          />
        </div>

        <div className="form-group">
          <label className="form-label">
            Task Difficulty: {taskDifficulty.toFixed(2)}
            <span className="form-hint">
              0.0 = Very Easy | 0.5 = Moderate | 1.0 = Very Difficult
            </span>
          </label>
          <input
            type="range"
            className="form-slider"
            min="0"
            max="1"
            step="0.01"
            value={taskDifficulty}
            onChange={(e) => setTaskDifficulty(parseFloat(e.target.value))}
          />
        </div>

        <div className="form-group">
          <label className="form-label">
            EEG Features (8D vector)
            <span className="form-hint">
              Normalized features [-1, 1]: Alpha, Beta, Theta, Delta power bands, asymmetry, HRV, engagement
            </span>
          </label>
          <textarea
            className="form-input"
            value={features}
            onChange={(e) => setFeatures(e.target.value)}
            placeholder="[0.5, -0.3, 0.8, 0.1, -0.2, 0.6, 0.0, -0.4]"
            rows={3}
            required
          />
        </div>

        <button type="submit" className="button button-success" disabled={loading}>
          {loading ? (
            <>
              <Loader size={16} className="spin" />
              Computing...
            </>
          ) : (
            <>
              <Zap size={16} />
              Compute CME
            </>
          )}
        </button>
      </form>

      {error && (
        <div className="result-box error">
          <strong>Error:</strong> {error}
        </div>
      )}

      {result && (
        <div className="result-box success">
          <h4>Result</h4>
          <div className="result-grid">
            <div className="result-item">
              <span className="result-label">CME:</span>
              <span className="result-value highlight">{result.cme.toFixed(2)}</span>
            </div>
            <div className="result-item">
              <span className="result-label">Flow Probability:</span>
              <span className="result-value">{(result.pFlow * 100).toFixed(1)}%</span>
            </div>
            <div className="result-item">
              <span className="result-label">Shots Used:</span>
              <span className="result-value">{result.shotsUsed}</span>
            </div>
            <div className="result-item">
              <span className="result-label">Circuit Depth:</span>
              <span className="result-value">{result.depth}</span>
            </div>
            <div className="result-item">
              <span className="result-label">QPU Latency:</span>
              <span className="result-value">{result.qpuLatencyMs}ms</span>
            </div>
            <div className="result-item">
              <span className="result-label">Total Latency:</span>
              <span className="result-value">{result.totalLatencyMs}ms</span>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

