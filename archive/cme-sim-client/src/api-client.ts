/**
 * HTTP client for CME API.
 */
import axios, { AxiosInstance, AxiosError } from 'axios';
import type { InferenceRequest, InferenceResponse, TrainingJobResponse } from './types.js';

export class CmeApiClient {
  private client: AxiosInstance;

  constructor(baseURL: string) {
    this.client = axios.create({
      baseURL,
      timeout: 60000, // 60 seconds (QPU can be slow)
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async computeCme(request: InferenceRequest): Promise<InferenceResponse> {
    try {
      const response = await this.client.post<InferenceResponse>('/api/inference/cme', request);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError;
        throw new Error(`API error: ${axiosError.response?.status} ${axiosError.message}`);
      }
      throw error;
    }
  }

  async startTrainingJob(totalGenerations?: number): Promise<TrainingJobResponse> {
    try {
      const response = await this.client.post<TrainingJobResponse>('/api/training/start', {
        totalGenerations,
      });
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError;
        throw new Error(`API error: ${axiosError.response?.status} ${axiosError.message}`);
      }
      throw error;
    }
  }

  async getTrainingJob(jobId: string): Promise<TrainingJobResponse> {
    try {
      const response = await this.client.get<TrainingJobResponse>(`/api/training/${jobId}`);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError;
        throw new Error(`API error: ${axiosError.response?.status} ${axiosError.message}`);
      }
      throw error;
    }
  }

  async getDashboardSummary(): Promise<any> {
    try {
      const response = await this.client.get('/api/dashboard/summary');
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError;
        throw new Error(`API error: ${axiosError.response?.status} ${axiosError.message}`);
      }
      throw error;
    }
  }
}


