const API_BASE = import.meta.env.VITE_API_BASE ?? ''

let authToken: string | null = localStorage.getItem('tg_token')

export function setToken(token: string) {
  authToken = token
  localStorage.setItem('tg_token', token)
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
    ...(options?.headers as Record<string, string>),
  }
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`${res.status} ${text}`)
  }
  return res.json() as Promise<T>
}

export interface DashboardData {
  totalSpendTodayUsd: number
  perProvider: { provider: string; totalCost: number; requestCount: number }[]
  topConsumers: { apiKeyId: string; apiKeyName: string; totalCost: number; requestCount: number }[]
  spendLast7Days: { date: string; spendUsd: number }[]
}

export interface ApiKeyRecord {
  id: string
  name: string
  projectName: string
  dailyLimitUsd: number
  isActive: boolean
  createdAt: string
  spentTodayUsd: number
  isBlocked: boolean
}

export interface CreateKeyPayload {
  name: string
  projectName: string
  dailyLimitUsd: number
}

export interface CreatedKey extends ApiKeyRecord {
  key: string
}

export const api = {
  getDashboard: () => request<DashboardData>('/api/dashboard'),
  listKeys: () => request<ApiKeyRecord[]>('/api/keys'),
  createKey: (payload: CreateKeyPayload) =>
    request<CreatedKey>('/api/keys', { method: 'POST', body: JSON.stringify(payload) }),
  deleteKey: (id: string) =>
    request<void>(`/api/keys/${id}`, { method: 'DELETE' }),
  getToken: (secret: string) =>
    request<{ token: string }>('/api/auth/token', {
      method: 'POST',
      body: JSON.stringify({ adminSecret: secret }),
    }),
}
