import { Network, Workflow, GitBranch } from 'lucide-react'
import './ProcessVisualization.css'

export default function ProcessVisualization() {
  return (
    <div className="process-viz">
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">
            <Network size={20} />
            System Process Flow
          </h2>
        </div>

        <div className="viz-container">
          <div className="viz-section">
            <h3 className="viz-title">
              <Workflow size={18} />
              Online Inference Path (Latency-Critical)
            </h3>
            <div className="flow-diagram">
              <div className="flow-step">
                <div className="flow-box client">
                  <strong>Client/Dashboard</strong>
                  <span>Submit EEG window</span>
                </div>
                <div className="flow-arrow">↓ HTTP POST</div>
              </div>

              <div className="flow-step">
                <div className="flow-box api">
                  <strong>ASP.NET Core API</strong>
                  <span>InferenceController</span>
                  <span className="flow-detail">Validate request, log timestamp</span>
                </div>
                <div className="flow-arrow">↓ HTTP POST</div>
              </div>

              <div className="flow-step">
                <div className="flow-box qpu">
                  <strong>Quantum Backend (Python)</strong>
                  <span>Build VQC circuit</span>
                  <span className="flow-detail">
                    1. Encode features → Ry rotations<br/>
                    2. Entangle qubits → CX gates<br/>
                    3. Apply trained params → Ry, Rz<br/>
                    4. Measure 1024 shots
                  </span>
                </div>
                <div className="flow-arrow">↑ Response: p_flow</div>
              </div>

              <div className="flow-step">
                <div className="flow-box api">
                  <strong>CME Calculation</strong>
                  <span className="flow-formula">
                    CME = k × Energy × g(difficulty, p_flow)
                  </span>
                </div>
                <div className="flow-arrow">↓ Write</div>
              </div>

              <div className="flow-step">
                <div className="flow-box database">
                  <strong>SQL Server</strong>
                  <span>Store: InferenceRequestLog</span>
                  <span>Store: CmeWindowResult</span>
                </div>
                <div className="flow-arrow">↓ Return</div>
              </div>

              <div className="flow-step">
                <div className="flow-box client">
                  <strong>Response to Client</strong>
                  <span className="flow-result">
                    CME, p_flow, latency metrics
                  </span>
                </div>
              </div>

              <div className="timing-info">
                <strong>Typical Time:</strong> 1200-2000 ms<br/>
                <strong>Bottleneck:</strong> Quantum Backend (300-2000 ms)
              </div>
            </div>
          </div>

          <div className="viz-section">
            <h3 className="viz-title">
              <GitBranch size={18} />
              Training Job Path (Long-Running Background)
            </h3>
            <div className="flow-diagram">
              <div className="flow-step">
                <div className="flow-box client">
                  <strong>Client/Dashboard</strong>
                  <span>Submit training job</span>
                </div>
                <div className="flow-arrow">↓ HTTP POST</div>
              </div>

              <div className="flow-step">
                <div className="flow-box api">
                  <strong>TrainingController</strong>
                  <span>Create TrainingJob(status=Queued)</span>
                </div>
                <div className="flow-arrow">↓ Store in DB</div>
              </div>

              <div className="flow-step">
                <div className="flow-box database">
                  <strong>SQL Server</strong>
                  <span>TrainingJobs table</span>
                </div>
              </div>

              <div className="flow-divider">
                <span>Background Processing (5 sec polling)</span>
              </div>

              <div className="flow-step">
                <div className="flow-box worker">
                  <strong>TrainingWorkerService</strong>
                  <span>Detect queued job → Mark as Running</span>
                </div>
              </div>

              <div className="flow-step">
                <div className="flow-box-loop">
                  <strong>Metaheuristic Loop</strong>
                  <div className="loop-content">
                    <div className="loop-iteration">
                      <strong>For each generation (10):</strong>
                      <ol>
                        <li>Generate 5 candidate solutions (parameter sets)</li>
                        <li>For each candidate:
                          <ul>
                            <li>Generate test features</li>
                            <li>Call Quantum Backend → get p_flow</li>
                            <li>Compute fitness score</li>
                          </ul>
                        </li>
                        <li>Track best fitness found</li>
                        <li>Sleep 50-100ms (simulate CPU work)</li>
                        <li>Update DB: CompletedGenerations++</li>
                      </ol>
                    </div>
                  </div>
                </div>
                <div className="flow-arrow">↓ After all generations</div>
              </div>

              <div className="flow-step">
                <div className="flow-box worker">
                  <strong>Complete Training</strong>
                  <span>Update: status=Completed</span>
                  <span>Save: BestFitness, TotalQpuCalls</span>
                </div>
              </div>

              <div className="timing-info">
                <strong>Typical Time:</strong> 30-120 seconds<br/>
                <strong>QPU Calls:</strong> Generations × Candidates = 50 calls
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Algorithm Details Card */}
      <div className="card" style={{marginTop: '1.5rem'}}>
        <div className="card-header">
          <h2 className="card-title">Quantum Circuit Details (VQC)</h2>
        </div>
        
        <div className="circuit-diagram">
          <pre className="circuit-code">
{`4-Qubit Variational Quantum Classifier (VQC)

╔════════════════════════════════════════════════════════════╗
║  Layer 1: Feature Encoding (Angle Encoding)                ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  q₀: ──Ry(θ₀)──●────────────────────Ry(α₀)──Rz(β₀)──M    ║
║                │                                       │    ║
║  q₁: ──Ry(θ₁)──X──●─────────────────Ry(α₁)──Rz(β₁)──M    ║
║                   │                                    │    ║
║  q₂: ──Ry(θ₂)─────X──●──────────────Ry(α₂)──Rz(β₂)──M    ║
║                      │                                 │    ║
║  q₃: ──Ry(θ₃)────────X──●───────────Ry(α₃)──Rz(β₃)──M    ║
║                         │                              │    ║
║                         └──────────────────────────────●    ║
║                                                             ║
║  θᵢ = (feature[i] + 1) × π    [Feature Encoding]          ║
║  αᵢ, βᵢ = Trainable Params    [OPTIMIZED BY TRAINING]     ║
║                                                             ║
╚════════════════════════════════════════════════════════════╝

Parameters Being Optimized: {α₀, β₀, α₁, β₁, α₂, β₂, α₃, β₃}
                            = 8 rotation angles

Current (Simulated "Trained") Values:
  α = [0.5, 0.7, 0.9, 1.1]
  β = [1.2, 1.05, 0.9, 0.75]

Measurement: 1024 shots → probability distribution
Output: p_flow = P(q₀ = |1⟩) = flow state probability
`}
          </pre>
        </div>
      </div>

      {/* Petri Net Mapping Card */}
      <div className="card" style={{marginTop: '1.5rem'}}>
        <div className="card-header">
          <h2 className="card-title">Petri Net Model Mapping</h2>
        </div>
        
        <div className="petri-net-info">
          <p className="petri-intro">
            This implementation provides <strong>ground truth data</strong> to validate your Petri net model.
            Here's how to map the system to Petri net constructs:
          </p>

          <div className="mapping-grid">
            <div className="mapping-section">
              <h4>Places (States)</h4>
              <ul>
                <li><code>ClientReady</code> - Client ready to send request</li>
                <li><code>RequestInAPI</code> - Request being processed by API</li>
                <li><code>WaitingForQPU</code> - Queued for quantum backend</li>
                <li><code>InQPU</code> - Circuit executing on quantum hardware</li>
                <li><code>ComputingCME</code> - Calculating CME value</li>
                <li><code>WritingDB</code> - Persisting to database</li>
                <li><code>ResponseReady</code> - Sending response to client</li>
              </ul>
            </div>

            <div className="mapping-section">
              <h4>Transitions (Events)</h4>
              <ul>
                <li><code>SubmitRequest</code> - Client sends POST /api/inference/cme</li>
                <li><code>CallQPU</code> - API calls quantum backend</li>
                <li><code>StartQuantumExec</code> - Begin circuit execution</li>
                <li><code>FinishQuantumExec</code> - Circuit measurement complete</li>
                <li><code>CalculateCME</code> - Apply CME formula</li>
                <li><code>PersistResults</code> - Write to database</li>
                <li><code>ReturnResponse</code> - Send HTTP response</li>
              </ul>
            </div>

            <div className="mapping-section">
              <h4>Timing Parameters (For Petri Net)</h4>
              <table className="timing-table">
                <thead>
                  <tr>
                    <th>Transition</th>
                    <th>Distribution</th>
                    <th>Parameters</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>SubmitRequest</td>
                    <td>Poisson</td>
                    <td>λ = 1-10 req/s</td>
                  </tr>
                  <tr>
                    <td>CallQPU</td>
                    <td>Deterministic</td>
                    <td>~10 ms</td>
                  </tr>
                  <tr>
                    <td>StartQuantumExec</td>
                    <td>Uniform</td>
                    <td>U(300, 2000) ms</td>
                  </tr>
                  <tr>
                    <td>CalculateCME</td>
                    <td>Deterministic</td>
                    <td>~1 ms</td>
                  </tr>
                  <tr>
                    <td>PersistResults</td>
                    <td>Deterministic</td>
                    <td>~5-10 ms</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div className="petri-validation">
            <h4>Validation Strategy</h4>
            <ol>
              <li><strong>Collect Data</strong>: Run simulation client, record latencies from this system</li>
              <li><strong>Configure Petri Net</strong>: Use timing parameters from table above</li>
              <li><strong>Simulate</strong>: Run Petri net with same arrival rates</li>
              <li><strong>Compare</strong>:
                <ul>
                  <li>Average response times (should be within 5-10%)</li>
                  <li>P95/P99 latencies (tail behavior)</li>
                  <li>Throughput under load</li>
                  <li>Queue lengths (implicit in states)</li>
                </ul>
              </li>
              <li><strong>Thesis Conclusion</strong>: Petri net accurately models quantum ML system performance</li>
            </ol>
          </div>
        </div>
      </div>
    </div>
  )
}

