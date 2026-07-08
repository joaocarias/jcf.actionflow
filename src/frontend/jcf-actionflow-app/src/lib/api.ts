import type {
  ActionSummary,
  ActionWriteResponse,
  CollectionSummary,
  CopyActionRequest,
  CopyActionResponse,
  DeleteActionResponse,
  FlowGraph,
  ImportResponse,
  ProblemDetails,
  ValidationIssue,
  WatsonAction,
  WorkspaceSummary,
} from './types'

const API_BASE_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5000').replace(/\/$/, '')

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, init)
  } catch {
    throw new ApiError('Não foi possível conectar à API. Verifique se o backend está no ar.', 0)
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => undefined)) as ProblemDetails | undefined
    throw new ApiError(
      problem?.detail ?? problem?.title ?? `Falha na requisição (HTTP ${response.status}).`,
      response.status,
      problem,
    )
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function importWorkspace(file: File): Promise<ImportResponse> {
  const form = new FormData()
  form.append('file', file)
  return request<ImportResponse>('/api/workspaces', { method: 'POST', body: form })
}

export function getWorkspaceSummary(sessionId: string): Promise<WorkspaceSummary> {
  return request(`/api/workspaces/${sessionId}`)
}

export function getCollections(sessionId: string): Promise<CollectionSummary[]> {
  return request(`/api/workspaces/${sessionId}/collections`)
}

export function getActions(sessionId: string): Promise<ActionSummary[]> {
  return request(`/api/workspaces/${sessionId}/actions`)
}

export function getActionDetail(sessionId: string, actionId: string): Promise<WatsonAction> {
  return request(`/api/workspaces/${sessionId}/actions/${encodeURIComponent(actionId)}`)
}

export function getGraph(sessionId: string, level: 'actions' | 'steps' = 'actions'): Promise<FlowGraph> {
  return request(`/api/workspaces/${sessionId}/graph?level=${level}`)
}

export function validateWorkspace(sessionId: string): Promise<ValidationIssue[]> {
  return request(`/api/workspaces/${sessionId}/validate`)
}

export async function exportWorkspace(sessionId: string): Promise<string> {
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}/api/workspaces/${sessionId}/export`)
  } catch {
    throw new ApiError('Não foi possível conectar à API. Verifique se o backend está no ar.', 0)
  }
  if (!response.ok) {
    throw new ApiError(`Falha ao exportar (HTTP ${response.status}).`, response.status)
  }
  return response.text()
}

export function copyOrMoveAction(
  sessionId: string,
  actionId: string,
  body: CopyActionRequest,
): Promise<CopyActionResponse> {
  return request(`/api/workspaces/${sessionId}/actions/${encodeURIComponent(actionId)}/copy`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(body),
  })
}

export function renameAction(sessionId: string, actionId: string, title: string): Promise<ActionWriteResponse> {
  return request(`/api/workspaces/${sessionId}/actions/${encodeURIComponent(actionId)}`, {
    method: 'PATCH',
    headers: jsonHeaders,
    body: JSON.stringify({ title }),
  })
}

export function deleteAction(sessionId: string, actionId: string, force = false): Promise<DeleteActionResponse> {
  return request(`/api/workspaces/${sessionId}/actions/${encodeURIComponent(actionId)}?force=${force}`, {
    method: 'DELETE',
  })
}
