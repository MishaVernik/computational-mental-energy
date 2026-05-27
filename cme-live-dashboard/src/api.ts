import { getApiBase } from './runtimeApi';

const API_BASE = getApiBase();

export const api = {
  base: API_BASE,
  get: async <T>(path: string): Promise<T> => {
    const r = await fetch(`${API_BASE}${path}`);
    if (!r.ok) throw new Error(r.statusText || `HTTP ${r.status}`);
    return r.json() as Promise<T>;
  },
  post: async (path: string, body?: unknown) => {
    const r = await fetch(`${API_BASE}${path}`, {
      method: 'POST',
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });
    if (!r.ok) throw new Error(r.statusText || `HTTP ${r.status}`);
    return r.json();
  },
};

export interface SessionDto {
  id: string;
  userId: string;
  startedAt: string;
  windowCount: number;
}

export interface LabelStatsDto {
  total: number;
  flowCount: number;
  notFlowCount: number;
  unlabeledCount: number;
}

export interface AnalyzeClassicalResult {
  analyzed: number;
  labeled: number;
}
