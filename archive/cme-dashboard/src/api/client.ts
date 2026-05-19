import axios from 'axios'
import type {
  DashboardSummary,
  InferenceRequest,
  InferenceResponse,
  TrainingJob,
  StartTrainingRequest,
  CmeMetricsResponse,
} from '../types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 0, // 0 = unlimited timeout
  headers: {
    'Content-Type': 'application/json',
  },
})

export const apiClient = {
  // Dashboard
  async getDashboardSummary(): Promise<DashboardSummary> {
    const response = await api.get<DashboardSummary>('/dashboard/summary')
    return response.data
  },

  // Inference
  async submitInference(request: InferenceRequest): Promise<InferenceResponse> {
    const response = await api.post<InferenceResponse>('/inference/cme', request)
    return response.data
  },

  // Training
  async startTrainingJob(request: StartTrainingRequest): Promise<TrainingJob> {
    const response = await api.post<TrainingJob>('/training/start', request)
    return response.data
  },

  async getTrainingJob(jobId: string): Promise<TrainingJob> {
    const response = await api.get<TrainingJob>(`/training/${jobId}`)
    return response.data
  },

  async listTrainingJobs(limit: number = 20): Promise<TrainingJob[]> {
    const response = await api.get<TrainingJob[]>(`/training?limit=${limit}`)
    return response.data
  },

  // Experiments
  async createExperiment(request: any): Promise<any> {
    const response = await api.post('/experiments', request)
    return response.data
  },

  async getExperiment(id: string): Promise<any> {
    const response = await api.get(`/experiments/${id}`)
    return response.data
  },

  async listExperiments(limit: number = 20): Promise<any[]> {
    const response = await api.get(`/experiments?limit=${limit}`)
    return response.data
  },

  async completeExperiment(id: string): Promise<void> {
    await api.post(`/experiments/${id}/complete`)
  },

  async getExperimentMetrics(id: string): Promise<any> {
    const response = await api.get(`/experiments/${id}/metrics`)
    return response.data
  },

  async saveModelMetrics(id: string, metrics: any): Promise<void> {
    await api.post(`/experiments/${id}/modelMetrics`, metrics)
  },

  async getModelMetrics(id: string): Promise<any> {
    const response = await api.get(`/experiments/${id}/modelMetrics`)
    return response.data
  },

  async exportExperimentMetrics(id: string): Promise<Blob> {
    const response = await api.get(`/experiments/${id}/export`, {
      responseType: 'blob'
    })
    return response.data
  },

  // CME Metrics from Excel
  async computeCmeFromExcel(
    file: File,
    worksheetName?: string,
    configJson?: string
  ): Promise<CmeMetricsResponse> {
    const formData = new FormData()
    formData.append('file', file)
    if (worksheetName) formData.append('worksheetName', worksheetName)
    if (configJson) formData.append('configJson', configJson)

    const response = await api.post<CmeMetricsResponse>('/cme/compute-from-excel', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 0, // 0 = unlimited timeout
    })
    return response.data
  },

  async computeCmeFromExcelDownload(
    file: File,
    worksheetName?: string,
    configJson?: string
  ): Promise<Blob> {
    const formData = new FormData()
    formData.append('file', file)
    if (worksheetName) formData.append('worksheetName', worksheetName)
    if (configJson) formData.append('configJson', configJson)

    const response = await api.post('/cme/compute-from-excel-download', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      responseType: 'blob',
      timeout: 0, // 0 = unlimited timeout
    })
    return response.data
  },

  // Benchmarks
  async startBenchmark(config: any): Promise<string> {
    const response = await api.post<string>('/benchmarks/run', config)
    return response.data
  },

  async getBenchmark(runId: string): Promise<any> {
    const response = await api.get(`/benchmarks/${runId}`)
    return response.data
  },

  async getBenchmarkHistory(limit: number = 50): Promise<any[]> {
    const response = await api.get(`/benchmarks/history?limit=${limit}`)
    return response.data
  },

  async exportBenchmark(runId: string, format: 'json' | 'csv' = 'json'): Promise<Blob> {
    const response = await api.get(`/benchmarks/${runId}/export?format=${format}`, {
      responseType: 'blob'
    })
    return response.data
  },

  async getPetriNetParams(runId: string): Promise<any> {
    const response = await api.get(`/benchmarks/${runId}/petri-params`)
    return response.data
  },
}


