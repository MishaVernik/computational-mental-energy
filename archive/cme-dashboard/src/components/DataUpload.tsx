import { useState } from 'react'
import { Upload, FileText, CheckCircle, AlertCircle, FileSpreadsheet, Download } from 'lucide-react'
import { Line, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, ReferenceLine } from 'recharts'
import { apiClient } from '../api/client'
import type { CmeMetricsResponse } from '../types'
import './DataUpload.css'

export default function DataUpload() {
  const [csvText, setCsvText] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [processing, setProcessing] = useState(false)
  const [results, setResults] = useState<any>(null)
  const [error, setError] = useState<string | null>(null)
  const [startTime, setStartTime] = useState('')
  const [endTime, setEndTime] = useState('')
  const [taskDifficulty, setTaskDifficulty] = useState(0.7)
  const [excelFile, setExcelFile] = useState<File | null>(null)
  const [excelResults, setExcelResults] = useState<CmeMetricsResponse | null>(null)
  const [excelProcessing, setExcelProcessing] = useState(false)
  const [excelError, setExcelError] = useState<string | null>(null)
  const [processingProgress, setProcessingProgress] = useState<{ current: number; total: number; message: string } | null>(null)

  const processMindMonitorCsv = async (csvData: string) => {
    try {
      // Parse time strings to DateTime if provided
      let parsedStartTime: string | null = null;
      let parsedEndTime: string | null = null;
      
      if (startTime) {
        // Convert "HH:mm" format to ISO string
        const [hours, minutes] = startTime.split(':');
        const date = new Date();
        date.setHours(parseInt(hours), parseInt(minutes), 0, 0);
        parsedStartTime = date.toISOString();
      }
      
      if (endTime) {
        const [hours, minutes] = endTime.split(':');
        const date = new Date();
        date.setHours(parseInt(hours), parseInt(minutes), 0, 0);
        parsedEndTime = date.toISOString();
      }
      
      const response = await fetch('/api/mindmonitor/process', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          csvData,
          startTime: parsedStartTime,
          endTime: parsedEndTime,
          taskDifficulty,
          maxWindows: 100
        })
      })
      
      if (!response.ok) {
        const error = await response.text()
        throw new Error(error)
      }
      
      const data = await response.json()
      setResults({
        isMindMonitor: true,
        ...data
      })
    } catch (err) {
      throw err
    }
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      setFile(selectedFile)
      const reader = new FileReader()
      reader.onload = (event) => {
        setCsvText(event.target?.result as string)
      }
      reader.readAsText(selectedFile)
    }
  }

  const parseCsvRow = (row: string, headers: string[]) => {
    const values = row.split(',').map(v => v.trim())
    const obj: any = {}
    headers.forEach((header, index) => {
      obj[header] = values[index]
    })
    return obj
  }

  const handleProcessCsv = async () => {
    if (!csvText.trim()) {
      setError('Please paste CSV data or upload a file')
      return
    }

    setProcessing(true)
    setError(null)
    setResults(null)

    try {
      const lines = csvText.trim().split('\n')
      if (lines.length < 2) {
        throw new Error('CSV must have at least a header row and one data row')
      }

      const headers = lines[0].split(',').map(h => h.trim())
      
      // Detect format: Mind Monitor or Standard CSV
      const isMindMonitor = headers.some(h => h.includes('Delta_') || h.includes('Alpha_') || h.toLowerCase() === 'timestamp')
      
      if (isMindMonitor) {
        // Process Mind Monitor format
        await processMindMonitorCsv(csvText)
        return
      }
      
      // Validate headers (more lenient - just check for key columns)
      const hasRequiredFeatures = ['alpha', 'beta', 'theta', 'delta'].every(h => headers.includes(h))
      if (!hasRequiredFeatures) {
        throw new Error(`Missing required feature columns (alpha, beta, theta, delta). Did you mean to upload a Mind Monitor CSV?`)
      }

      // Process each row
      const processedResults = []
      const dataRows = lines.slice(1).filter(line => line.trim().length > 0)

      for (let i = 0; i < Math.min(dataRows.length, 10); i++) {  // Limit to 10 rows for demo
        const row = parseCsvRow(dataRows[i], headers)
        
        // Extract features
        const features = [
          parseFloat(row.alpha),
          parseFloat(row.beta),
          parseFloat(row.theta),
          parseFloat(row.delta),
          parseFloat(row.frontal_asym),
          parseFloat(row.parietal_asym),
          parseFloat(row.hrv),
          parseFloat(row.engagement),
        ]

        // Validate features
        if (features.some(f => isNaN(f))) {
          throw new Error(`Invalid numeric values in row ${i + 2}`)
        }

        if (features.some(f => f < -1 || f > 1)) {
          throw new Error(`Features must be in range [-1, 1] in row ${i + 2}`)
        }

        // Submit inference
        const response = await apiClient.submitInference({
          sessionId: row.session_id || 'batch-session',
          windowId: row.window_id || `batch-w-${i}`,
          features,
          taskDifficulty: row.task_difficulty ? parseFloat(row.task_difficulty) : 0.5,
        })

        processedResults.push({
          windowId: row.window_id,
          cme: response.cme,
          pFlow: response.pFlow,
          actualLabel: row.label || 'Unknown',
          latency: response.totalLatencyMs,
        })
      }

      setResults({
        processed: processedResults.length,
        total: dataRows.length,
        results: processedResults,
      })

    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to process CSV')
    } finally {
      setProcessing(false)
    }
  }

  const loadExampleData = () => {
    const example = `timestamp,session_id,window_id,alpha,beta,theta,delta,frontal_asym,parietal_asym,hrv,engagement,task_difficulty,label
1732038456.123,session_001,w_001,0.52,-0.31,0.78,0.11,-0.23,0.61,0.05,-0.42,0.6,Flow
1732038457.123,session_001,w_002,0.48,-0.28,0.81,0.09,-0.20,0.58,0.03,-0.39,0.6,Flow
1732038458.123,session_001,w_003,0.15,0.42,-0.62,0.31,0.87,-0.19,0.51,0.12,0.3,No_Flow`
    setCsvText(example)
  }

  const handleExcelFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      if (!selectedFile.name.match(/\.(xlsx|xls|csv)$/i)) {
        setExcelError('Please select an Excel file (.xlsx or .xls) or CSV file (.csv)')
        return
      }
      setExcelFile(selectedFile)
      setExcelError(null)
      setExcelResults(null)
    }
  }

  const handleProcessExcel = async () => {
    if (!excelFile) {
      setExcelError('Please select an Excel (.xlsx) or CSV (.csv) file')
      return
    }

    setExcelProcessing(true)
    setExcelError(null)
    setExcelResults(null)
    
    // Estimate total windows based on file size (rough estimate: ~500 bytes per window)
    const estimatedWindows = Math.max(10, Math.floor(excelFile.size / 500))
    setProcessingProgress({ current: 0, total: estimatedWindows, message: `Reading file (${(excelFile.size / 1024 / 1024).toFixed(2)} MB)...` })

    try {
      // Start a progress simulation that updates every 2 seconds
      const progressInterval = setInterval(() => {
        setProcessingProgress(prev => {
          if (!prev) return null
          // Simulate progress: increase by 1-2% every update, but don't exceed 95% until done
          const increment = Math.random() * 2 + 1
          const newCurrent = Math.min(prev.total * 0.95, prev.current + increment)
          const percent = Math.round((newCurrent / prev.total) * 100)
          return {
            ...prev,
            current: newCurrent,
            message: `Processing... ${percent}% (estimated ${Math.round(prev.total)} windows)`
          }
        })
      }, 2000)

      const data = await apiClient.computeCmeFromExcel(excelFile)
      
      clearInterval(progressInterval)
      
      // Update progress with actual results
      if (data.sessionSummaries[0]?.windowDetails) {
        const totalWindows = data.sessionSummaries[0].windowDetails.length
        setProcessingProgress({ 
          current: totalWindows, 
          total: totalWindows, 
          message: `Completed! Processed ${totalWindows} windows with quantum backend` 
        })
        
        // Clear progress after 2 seconds
        setTimeout(() => {
          setProcessingProgress(null)
        }, 2000)
      } else {
        setProcessingProgress(null)
      }
      
      setExcelResults(data)
    } catch (err) {
      setExcelError(err instanceof Error ? err.message : 'Failed to process Excel file')
      setProcessingProgress(null)
    } finally {
      setExcelProcessing(false)
    }
  }

  const handleDownloadExcel = async () => {
    if (!excelFile) return

    setExcelProcessing(true)
    setExcelError(null)

    try {
      const blob = await apiClient.computeCmeFromExcelDownload(excelFile)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = excelFile.name.replace(/\.(xlsx|xls)$/i, '_cme_results.xlsx')
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (err) {
      setExcelError(err instanceof Error ? err.message : 'Failed to download results')
    } finally {
      setExcelProcessing(false)
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          <Upload size={20} />
          Batch Processing (CSV Upload - Max 10 rows)
        </h2>
        <p style={{marginTop: '0.5rem', fontSize: '0.9rem', color: '#94a3b8'}}>
          For CSV files. For Excel files with unlimited rows, scroll down to <strong>"Excel CME Metrics Processing"</strong> section below.
        </p>
      </div>

      <div className="upload-section">
        <div className="upload-methods">
          <div className="upload-method">
            <label className="file-upload-label">
              <FileText size={20} />
              <span>Upload CSV File</span>
              <input
                type="file"
                accept=".csv"
                onChange={handleFileChange}
                className="file-input"
              />
            </label>
            {file && (
              <div className="file-info">
                <CheckCircle size={16} className="text-success" />
                <span>{file.name}</span>
              </div>
            )}
          </div>

          <div className="upload-or">OR</div>

          <div className="upload-method">
            <button onClick={loadExampleData} className="button button-secondary">
              Load Example Data
            </button>
          </div>
        </div>

        <div className="form-group">
          <label className="form-label">
            CSV Data
            <span className="form-hint">
              Required columns: timestamp, session_id, window_id, alpha, beta, theta, delta, frontal_asym, parietal_asym, hrv, engagement
            </span>
          </label>
          <textarea
            className="form-input csv-input"
            value={csvText}
            onChange={(e) => setCsvText(e.target.value)}
            placeholder="Paste CSV data here or upload file above..."
            rows={8}
          />
        </div>

        <div className="upload-actions">
          <button
            onClick={handleProcessCsv}
            className="button button-success"
            disabled={processing || !csvText.trim()}
          >
            {processing ? 'Processing...' : 'Process CSV (Max 10 rows)'}
          </button>
          <a
            href="/example_data/eeg_sample_data.csv"
            download
            className="button button-secondary"
          >
            Download Full Example CSV
          </a>
        </div>

        <div className="time-range-filters">
          <h4>Time Range Filter (for Mind Monitor data)</h4>
          <div className="time-inputs">
            <div className="form-group">
              <label>Start Time (optional)</label>
              <input
                type="time"
                className="form-input"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                placeholder="e.g., 19:55"
              />
            </div>
            <div className="form-group">
              <label>End Time (optional)</label>
              <input
                type="time"
                className="form-input"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                placeholder="e.g., 21:20"
              />
            </div>
            <div className="form-group">
              <label>Task Difficulty: {taskDifficulty.toFixed(2)}</label>
              <input
                type="range"
                className="form-slider"
                min="0"
                max="1"
                step="0.05"
                value={taskDifficulty}
                onChange={(e) => setTaskDifficulty(parseFloat(e.target.value))}
              />
            </div>
          </div>
          <p className="filter-hint">
            For Mind Monitor files: Specify time range to analyze (e.g., gym session 7:55 PM - 9:20 PM).
            Leave empty to process all data.
          </p>
        </div>

        <div className="format-help">
          <strong>Supported Formats:</strong>
          <ul style={{margin: '0.5rem 0', paddingLeft: '1.5rem'}}>
            <li><strong>Mind Monitor (Muse)</strong>: Auto-detected by TimeStamp, Delta_*, Theta_*, Alpha_*, Beta_*, Gamma_* columns</li>
            <li><strong>Standard CSV</strong>: timestamp, session_id, window_id, alpha, beta, theta, delta, frontal_asym, parietal_asym, hrv, engagement</li>
          </ul>
          See <code>example_data/DATA_FORMAT.md</code> for complete specification.
        </div>
      </div>

      {error && (
        <div className="result-box error">
          <AlertCircle size={18} />
          <span><strong>Error:</strong> {error}</span>
        </div>
      )}

      {results && !results.isMindMonitor && (
        <div className="result-box success">
          <h4>Batch Processing Complete</h4>
          <p>Processed {results.processed} of {results.total} rows</p>
          
          <div className="results-table">
            <div className="table-header">
              <div>Window ID</div>
              <div>CME</div>
              <div>p_flow</div>
              <div>Label</div>
              <div>Latency</div>
            </div>
            {results.results.map((r: any, i: number) => (
              <div key={i} className="table-row">
                <div>{r.windowId}</div>
                <div><strong>{r.cme.toFixed(2)}</strong></div>
                <div>{(r.pFlow * 100).toFixed(1)}%</div>
                <div>
                  <span className={`label-badge ${r.actualLabel === 'Flow' ? 'flow' : 'no-flow'}`}>
                    {r.actualLabel}
                  </span>
                </div>
                <div>{r.latency}ms</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {results && results.isMindMonitor && (
        <div className="result-box success">
          <h4>🧠 Mind Monitor Data Analysis Complete</h4>
          <div className="muse-summary">
            <div className="summary-grid">
              <div className="summary-item">
                <strong>Session ID:</strong> <code>{results.sessionId}</code>
              </div>
              <div className="summary-item">
                <strong>Time Range:</strong> {new Date(results.startTime).toLocaleTimeString()} - {new Date(results.endTime).toLocaleTimeString()}
              </div>
              <div className="summary-item">
                <strong>Duration:</strong> {results.summary.totalDurationMinutes.toFixed(1)} minutes
              </div>
              <div className="summary-item">
                <strong>Windows Processed:</strong> {results.processedWindows} / {results.totalWindows}
              </div>
            </div>

            <div className="flow-analysis">
              <h5>Flow State Analysis</h5>
              <div className="flow-stats">
                <div className="flow-stat">
                  <span className="stat-label">Average CME:</span>
                  <span className="stat-value highlight">{results.summary.avgCme.toFixed(2)}</span>
                </div>
                <div className="flow-stat">
                  <span className="stat-label">Peak CME:</span>
                  <span className="stat-value">{results.summary.maxCme.toFixed(2)}</span>
                </div>
                <div className="flow-stat">
                  <span className="stat-label">Avg Flow Probability:</span>
                  <span className="stat-value">{(results.summary.avgPFlow * 100).toFixed(1)}%</span>
                </div>
                <div className="flow-stat">
                  <span className="stat-label">Time in Flow:</span>
                  <span className="stat-value success">{results.summary.timeInFlowPercentage.toFixed(1)}%</span>
                </div>
              </div>
            </div>

            <div className="timeline-results">
              <h5>CME Over Time (First 20 windows)</h5>
              <div className="timeline-table">
                {results.results.slice(0, 20).map((r: any, i: number) => (
                  <div key={i} className="timeline-row">
                    <span className="time-label">{new Date(r.timestamp).toLocaleTimeString()}</span>
                    <div className="cme-bar-container">
                      <div className="cme-bar" style={{width: `${(r.cme / results.summary.maxCme) * 100}%`}}>
                        <span className="cme-value">{r.cme.toFixed(1)}</span>
                      </div>
                    </div>
                    <span className="flow-indicator" style={{color: r.pFlow > 0.6 ? '#6ee7b7' : '#94a3b8'}}>
                      {(r.pFlow * 100).toFixed(0)}%
                    </span>
                  </div>
                ))}
              </div>
              {results.results.length > 20 && (
                <p className="more-data">... and {results.results.length - 20} more windows</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Excel CME Processing Section */}
      <div style={{marginTop: '3rem', paddingTop: '2rem', borderTop: '3px solid #3b82f6', backgroundColor: '#1e293b', padding: '1.5rem', borderRadius: '0.5rem'}}>
        <h2 style={{marginBottom: '0.5rem', color: '#e2e8f0', fontSize: '1.5rem'}}>
          <FileSpreadsheet size={24} style={{display: 'inline', marginRight: '0.5rem', verticalAlign: 'middle'}} />
          Excel/CSV CME Metrics Processing
        </h2>
        <p style={{marginBottom: '1.5rem', color: '#94a3b8', fontSize: '1rem'}}>
          Upload an <strong style={{color: '#60a5fa'}}>Excel file (.xlsx)</strong> or <strong style={{color: '#60a5fa'}}>CSV file (.csv)</strong> with EEG window data to compute CME metrics per window and per session. 
          This processes <strong>all rows</strong> (no 10-row limit). Supports Mind Monitor CSV format.
        </p>

        <div className="upload-methods">
          <div className="upload-method">
            <label className="file-upload-label">
              <FileSpreadsheet size={20} />
              <span>Upload Excel/CSV File (.xlsx, .csv)</span>
              <input
                type="file"
                accept=".xlsx,.xls,.csv"
                onChange={handleExcelFileChange}
                className="file-input"
              />
            </label>
            {excelFile && (
              <div className="file-info">
                <CheckCircle size={16} className="text-success" />
                <span>{excelFile.name}</span>
              </div>
            )}
          </div>
        </div>

        <div className="upload-actions" style={{marginTop: '1rem'}}>
          <button
            onClick={handleProcessExcel}
            className="button button-success"
            disabled={excelProcessing || !excelFile}
          >
            {excelProcessing ? 'Processing...' : 'Compute CME Metrics'}
          </button>
          {processingProgress && (
            <div style={{marginTop: '0.5rem', padding: '0.75rem', backgroundColor: '#1e293b', borderRadius: '0.5rem', fontSize: '0.85rem'}}>
              <div style={{display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem'}}>
                <span style={{color: '#94a3b8'}}>{processingProgress.message}</span>
                <span style={{color: '#60a5fa'}}>
                  {processingProgress.current} / {processingProgress.total}
                  {processingProgress.total > 0 && ` (${Math.round((processingProgress.current / processingProgress.total) * 100)}%)`}
                </span>
              </div>
              {processingProgress.total > 0 && (
                <div style={{width: '100%', height: '8px', backgroundColor: '#334155', borderRadius: '4px', overflow: 'hidden'}}>
                  <div 
                    style={{
                      width: `${Math.min(100, (processingProgress.current / processingProgress.total) * 100)}%`,
                      height: '100%',
                      backgroundColor: '#60a5fa',
                      transition: 'width 0.3s ease'
                    }}
                  />
                </div>
              )}
            </div>
          )}
          <button
            onClick={handleDownloadExcel}
            className="button button-secondary"
            disabled={excelProcessing || !excelFile}
          >
            <Download size={16} style={{display: 'inline', marginRight: '0.25rem'}} />
            Download Results Excel
          </button>
        </div>

        {excelError && (
          <div className="result-box error" style={{marginTop: '1rem'}}>
            <AlertCircle size={18} />
            <span><strong>Error:</strong> {excelError}</span>
          </div>
        )}

        {excelResults && (
          <div className="result-box success" style={{marginTop: '1rem'}}>
            <h4>📊 CME Metrics Analysis Complete</h4>
            
            <div className="muse-summary">
              <div className="summary-grid">
                <div className="summary-item">
                  <strong>Total Sessions:</strong> {excelResults.globalSummary.totalSessions}
                </div>
                <div className="summary-item">
                  <strong>Mean CME/Session:</strong> {excelResults.globalSummary.meanCmeSession.toFixed(2)}
                </div>
                <div className="summary-item">
                  <strong>Median CME/Session:</strong> {excelResults.globalSummary.medianCmeSession.toFixed(2)}
                </div>
                <div className="summary-item">
                  <strong>Mean Flow Share:</strong> {(excelResults.globalSummary.meanFlowShare * 100).toFixed(1)}%
                </div>
              </div>

              <div className="flow-analysis">
                <h5>Flow State Distribution</h5>
                <div className="flow-stats">
                  <div className="flow-stat">
                    <span className="stat-label">Sessions with Flow ≥ 50%:</span>
                    <span className="stat-value">{excelResults.globalSummary.sessionsFlowShareGe05}</span>
                  </div>
                  <div className="flow-stat">
                    <span className="stat-label">Sessions with Flow ≥ 70%:</span>
                    <span className="stat-value">{excelResults.globalSummary.sessionsFlowShareGe07}</span>
                  </div>
                </div>
              </div>

              <div style={{marginTop: '1.5rem'}}>
                <h5>Session Details</h5>
                <div className="results-table">
                  <div className="table-header">
                    <div>Session ID</div>
                    <div>Windows</div>
                    <div>Duration (s)</div>
                    <div>Flow Share</div>
                    <div>Avg CME</div>
                    <div>CME Session</div>
                  </div>
                  {excelResults.sessionSummaries.slice(0, 20).map((session, i) => (
                    <div key={i} className="table-row">
                      <div>{session.sessionId}</div>
                      <div>{session.totalWindows}</div>
                      <div>{session.totalDurationSeconds.toFixed(1)}</div>
                      <div>{(session.flowShare * 100).toFixed(1)}%</div>
                      <div><strong>{session.avgCme.toFixed(2)}</strong></div>
                      <div><strong>{session.cmeSession.toFixed(2)}</strong></div>
                    </div>
                  ))}
                </div>
                {excelResults.sessionSummaries.length > 20 && (
                  <p className="more-data" style={{marginTop: '0.5rem'}}>
                    ... and {excelResults.sessionSummaries.length - 20} more sessions
                  </p>
                )}
              </div>

              {/* Flow State Timeline Chart */}
              {excelResults.sessionSummaries[0]?.windowDetails && excelResults.sessionSummaries[0].windowDetails.length > 0 && (
                <div style={{marginTop: '1.5rem'}}>
                  <h5>Flow State Timeline (Quantum Backend Results)</h5>
                  <div style={{marginBottom: '0.5rem', fontSize: '0.85rem', color: '#94a3b8'}}>
                    Flow threshold: {(excelResults.globalSummary.flowThreshold * 100).toFixed(0)}% | 
                    Flow windows: {excelResults.sessionSummaries[0].flowWindows} / {excelResults.sessionSummaries[0].totalWindows} 
                    ({(excelResults.sessionSummaries[0].flowShare * 100).toFixed(1)}%)
                  </div>
                  <div style={{height: '400px', marginTop: '1rem'}}>
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart data={excelResults.sessionSummaries[0].windowDetails.map((w, idx) => ({
                        time: new Date(w.timestamp).toLocaleTimeString(),
                        timestamp: new Date(w.timestamp).getTime(),
                        index: idx,
                        cme: w.cme,
                        pFlow: w.pFlow * 100,
                        isFlow: w.isFlow,
                        flowThreshold: excelResults.globalSummary.flowThreshold * 100,
                        // Background indicators: show threshold area, not 100%
                        flowIndicator: w.isFlow ? excelResults.globalSummary.flowThreshold * 100 : 0,
                        nonFlowIndicator: w.isFlow ? 0 : excelResults.globalSummary.flowThreshold * 100
                      }))}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                        <XAxis 
                          dataKey="time" 
                          stroke="#94a3b8"
                          style={{ fontSize: '0.75rem' }}
                          angle={-45}
                          textAnchor="end"
                          height={60}
                        />
                        <YAxis 
                          yAxisId="left"
                          stroke="#60a5fa"
                          label={{ value: 'CME', angle: -90, position: 'insideLeft', style: { fill: '#60a5fa' } }}
                        />
                        <YAxis 
                          yAxisId="right"
                          orientation="right"
                          stroke="#6ee7b7"
                          domain={[0, 100]}
                          label={{ value: 'p_flow (%)', angle: 90, position: 'insideRight', style: { fill: '#6ee7b7' } }}
                        />
                        <Tooltip 
                          contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: '0.5rem' }}
                          labelStyle={{ color: '#e2e8f0' }}
                          formatter={(value: any, name: string) => {
                            if (name === 'Flow State') {
                              return value === 1 ? 'IN FLOW ✓' : 'NOT IN FLOW ✗'
                            }
                            if (name === 'p_flow (%)') {
                              return [`${Number(value).toFixed(1)}%`, name]
                            }
                            if (name && name.includes('Flow Threshold')) {
                              return [`${Number(value).toFixed(0)}%`, name]
                            }
                            return [value, name]
                          }}
                          labelFormatter={(label) => `Time: ${label}`}
                        />
                        <Legend />
                        {/* CME Area - render first so it's in background */}
                        <Area 
                          yAxisId="left"
                          type="monotone" 
                          dataKey="cme" 
                          stroke="#60a5fa" 
                          fill="#60a5fa" 
                          fillOpacity={0.3}
                          name="CME"
                        />
                        {/* Flow State Background Areas - show as horizontal bands */}
                        <Area 
                          yAxisId="right"
                          type="monotone" 
                          dataKey="flowIndicator"
                          fill="#6ee7b7"
                          fillOpacity={0.15}
                          stroke="none"
                          name="Flow State"
                          stackId="flow"
                        />
                        <Area 
                          yAxisId="right"
                          type="monotone" 
                          dataKey="nonFlowIndicator"
                          fill="#ef4444"
                          fillOpacity={0.1}
                          stroke="none"
                          name="Non-Flow State"
                          stackId="flow"
                        />
                        {/* Flow Threshold Line - use ReferenceLine for constant threshold */}
                        <ReferenceLine 
                          yAxisId="right"
                          y={excelResults.globalSummary.flowThreshold * 100}
                          stroke="#fbbf24" 
                          strokeWidth={3}
                          strokeDasharray="8 4"
                          label={{ value: `${(excelResults.globalSummary.flowThreshold * 100).toFixed(0)}%`, position: "right", fill: '#fbbf24', fontSize: 12, fontWeight: 'bold' }}
                        />
                        {/* p_flow Line - render on top */}
                        <Line 
                          yAxisId="right"
                          type="monotone" 
                          dataKey="pFlow" 
                          stroke="#6ee7b7" 
                          strokeWidth={2.5}
                          name="p_flow (%)"
                          dot={{ r: 4, fill: '#6ee7b7', strokeWidth: 2 }}
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                  <div style={{marginTop: '0.5rem', fontSize: '0.75rem', color: '#64748b', display: 'flex', gap: '1rem', justifyContent: 'center', flexWrap: 'wrap'}}>
                    <span style={{color: '#6ee7b7'}}>■ Flow State (p_flow ≥ {(excelResults.globalSummary.flowThreshold * 100).toFixed(0)}%)</span>
                    <span style={{color: '#ef4444'}}>■ Non-Flow State (p_flow &lt; {(excelResults.globalSummary.flowThreshold * 100).toFixed(0)}%)</span>
                    <span style={{color: '#fbbf24'}}>--- Flow Threshold ({(excelResults.globalSummary.flowThreshold * 100).toFixed(0)}%)</span>
                  </div>
                </div>
              )}

              {/* Flow Periods */}
              {excelResults.sessionSummaries[0]?.flowPeriods && excelResults.sessionSummaries[0].flowPeriods.length > 0 && (
                <div style={{marginTop: '1.5rem'}}>
                  <h5>Flow State Periods</h5>
                  <div className="results-table">
                    <div className="table-header">
                      <div>Start Time</div>
                      <div>End Time</div>
                      <div>Duration (s)</div>
                      <div>Avg CME</div>
                      <div>Avg p_flow</div>
                    </div>
                    {excelResults.sessionSummaries[0].flowPeriods.map((period, i) => (
                      <div key={i} className="table-row" style={{backgroundColor: '#1e3a5f'}}>
                        <div>{new Date(period.startTime).toLocaleTimeString()}</div>
                        <div>{new Date(period.endTime).toLocaleTimeString()}</div>
                        <div><strong>{period.durationSeconds.toFixed(1)}</strong></div>
                        <div>{period.avgCme.toFixed(2)}</div>
                        <div><strong style={{color: '#6ee7b7'}}>{(period.avgPFlow * 100).toFixed(1)}%</strong></div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div style={{marginTop: '1.5rem', padding: '1rem', backgroundColor: '#1e293b', borderRadius: '0.5rem'}}>
                <h5>Configuration Used</h5>
                <div style={{fontSize: '0.85rem', color: '#94a3b8', fontFamily: 'monospace'}}>
                  <div>k = {excelResults.globalSummary.k.toFixed(6)}</div>
                  <div>Weights: w_delta={excelResults.globalSummary.wDelta}, w_theta={excelResults.globalSummary.wTheta}, 
                    w_alpha={excelResults.globalSummary.wAlpha}, w_beta={excelResults.globalSummary.wBeta}</div>
                  <div>Lambda: λ1={excelResults.globalSummary.lambda1}, λ2={excelResults.globalSummary.lambda2}, 
                    λ3={excelResults.globalSummary.lambda3}</div>
                  <div>Flow Threshold: {excelResults.globalSummary.flowThreshold}</div>
                  <div style={{marginTop: '0.5rem', color: '#6ee7b7'}}>
                    ✓ Using Quantum Backend with Trained Parameters
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

