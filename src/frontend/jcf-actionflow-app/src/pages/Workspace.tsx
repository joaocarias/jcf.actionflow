import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { IssueList } from '../components/IssueList'
import { ApiError, getActions, getCollections, getWorkspaceSummary, validateWorkspace } from '../lib/api'
import type { ActionSummary, CollectionSummary, ValidationIssue, WorkspaceSummary } from '../lib/types'

type LoadState =
  | { status: 'loading' }
  | {
      status: 'ready'
      summary: WorkspaceSummary
      collections: CollectionSummary[]
      systemActions: ActionSummary[]
      orphanActions: ActionSummary[]
      issues: ValidationIssue[]
    }
  | { status: 'error'; message: string }

export function Workspace() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const [state, setState] = useState<LoadState>({ status: 'loading' })

  useEffect(() => {
    if (!sessionId) return
    let cancelled = false
    setState({ status: 'loading' })

    Promise.all([
      getWorkspaceSummary(sessionId),
      getCollections(sessionId),
      getActions(sessionId),
      validateWorkspace(sessionId),
    ])
      .then(([summary, collections, actions, issues]) => {
        if (cancelled) return
        setState({
          status: 'ready',
          summary,
          collections,
          systemActions: actions.filter((a) => a.isSystemAction),
          orphanActions: actions.filter((a) => !a.isSystemAction && a.isOrphan),
          issues,
        })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setState({
          status: 'error',
          message: error instanceof ApiError ? error.message : 'Falha ao carregar o workspace.',
        })
      })

    return () => {
      cancelled = true
    }
  }, [sessionId])

  if (state.status === 'loading') {
    return (
      <div className="mx-auto max-w-4xl px-6 py-16">
        <p className="text-sm text-slate-500">Carregando...</p>
      </div>
    )
  }

  if (state.status === 'error') {
    return (
      <div className="mx-auto max-w-4xl px-6 py-16">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="text-sm font-medium text-red-800">{state.message}</p>
        </div>
        <Link to="/" className="mt-4 inline-block text-sm font-medium text-indigo-600 hover:text-indigo-700">
          Importar um novo fluxo
        </Link>
      </div>
    )
  }

  const { summary, collections, systemActions, orphanActions, issues } = state

  return (
    <div className="mx-auto max-w-4xl px-6 py-16">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{summary.name ?? 'Fluxo importado'}</h1>
          <p className="mt-1 text-sm text-slate-500">
            {summary.businessActionCount} actions de negócio · {summary.systemActionCount} de sistema ·{' '}
            {summary.intentCount} intents · {summary.variableCount} variáveis · {summary.collectionCount} collections
          </p>
        </div>
        <Link
          to={`/workspaces/${sessionId}/graph`}
          className="shrink-0 rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500"
        >
          Ver grafo
        </Link>
      </div>

      <div className="mt-6">
        <IssueList issues={issues} />
      </div>

      <div className="mt-8 space-y-8">
        {collections.map((collection) => (
          <ActionSection
            key={collection.collectionId}
            sessionId={sessionId!}
            title={collection.title ?? collection.collectionId}
            rows={collection.actions.map((a) => ({ id: a.actionId, title: a.title, stepCount: a.stepCount }))}
            emptyLabel="Nenhuma action nesta collection."
          />
        ))}

        {systemActions.length > 0 && (
          <ActionSection
            sessionId={sessionId!}
            title="Actions de sistema"
            rows={systemActions.map((a) => ({ id: a.actionId, title: a.title, stepCount: a.stepCount }))}
          />
        )}

        {orphanActions.length > 0 && (
          <ActionSection
            sessionId={sessionId!}
            title="Sem collection"
            rows={orphanActions.map((a) => ({ id: a.actionId, title: a.title, stepCount: a.stepCount }))}
          />
        )}
      </div>
    </div>
  )
}

type ActionRow = { id: string; title: string | null; stepCount: number }

function ActionSection({
  sessionId,
  title,
  rows,
  emptyLabel,
}: {
  sessionId: string
  title: string
  rows: ActionRow[]
  emptyLabel?: string
}) {
  return (
    <section>
      <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">{title}</h2>
      <ul className="mt-3 divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 bg-white">
        {rows.map((row) => (
          <li key={row.id}>
            <Link
              to={`/workspaces/${sessionId}/actions/${row.id}`}
              className="flex items-center justify-between px-4 py-3 text-sm transition-colors hover:bg-slate-50"
            >
              <div>
                <p className="font-medium text-slate-800">{row.title ?? row.id}</p>
                <p className="font-mono text-xs text-slate-400">{row.id}</p>
              </div>
              <span className="text-xs text-slate-500">{row.stepCount} step(s)</span>
            </Link>
          </li>
        ))}
        {rows.length === 0 && emptyLabel && <li className="px-4 py-3 text-sm text-slate-400">{emptyLabel}</li>}
      </ul>
    </section>
  )
}
