import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { EnvironmentFlag, StepCountMismatchFlag, UnusedVariablesFlag } from '../components/EnvironmentFlag'
import { IssueList } from '../components/IssueList'
import {
  ApiError,
  exportWorkspace,
  getActions,
  getCollections,
  getVariables,
  getWorkspaceSummary,
  validateWorkspace,
} from '../lib/api'
import { downloadTextFile } from '../lib/download'
import type {
  ActionSummary,
  CollectionSummary,
  EnvironmentStepCount,
  ValidationIssue,
  VariableUsage,
  WorkspaceSummary,
} from '../lib/types'

type LoadState =
  | { status: 'loading' }
  | {
      status: 'ready'
      summary: WorkspaceSummary
      collections: CollectionSummary[]
      systemActions: ActionSummary[]
      orphanActions: ActionSummary[]
      issues: ValidationIssue[]
      variables: VariableUsage[]
    }
  | { status: 'error'; message: string }

export function Workspace() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const [state, setState] = useState<LoadState>({ status: 'loading' })
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const [downloading, setDownloading] = useState(false)

  useEffect(() => {
    if (!sessionId) return
    let cancelled = false
    setState({ status: 'loading' })

    Promise.all([
      getWorkspaceSummary(sessionId),
      getCollections(sessionId),
      getActions(sessionId),
      validateWorkspace(sessionId),
      getVariables(sessionId),
    ])
      .then(([summary, collections, actions, issues, variables]) => {
        if (cancelled) return
        setState({
          status: 'ready',
          summary,
          collections,
          systemActions: actions.filter((a) => a.isSystemAction),
          orphanActions: actions.filter((a) => !a.isSystemAction && a.isOrphan),
          issues,
          variables,
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

  const { summary, collections, systemActions, orphanActions, issues, variables } = state

  const handleDownload = async () => {
    if (!sessionId) return
    setDownloadError(null)
    setDownloading(true)
    try {
      const json = await exportWorkspace(sessionId)
      const safeName = (summary.name ?? sessionId).replace(/[^a-zA-Z0-9_-]+/g, '_')
      downloadTextFile(`${safeName}.json`, json)
    } catch (error) {
      setDownloadError(error instanceof ApiError ? error.message : 'Falha ao gerar o JSON para download.')
    } finally {
      setDownloading(false)
    }
  }

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
        <div className="flex shrink-0 items-center gap-2">
          <button
            type="button"
            onClick={handleDownload}
            disabled={downloading}
            className="rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
          >
            {downloading ? 'Gerando...' : 'Baixar JSON'}
          </button>
          <Link
            to={`/workspaces/${sessionId}/graph`}
            className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500"
          >
            Ver grafo
          </Link>
        </div>
      </div>

      {downloadError && (
        <p className="mt-3 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">{downloadError}</p>
      )}

      <div className="mt-6">
        <IssueList issues={issues} />
      </div>

      <div className="mt-8 space-y-8">
        {collections.map((collection) => (
          <ActionSection
            key={collection.collectionId}
            sessionId={sessionId!}
            title={collection.title ?? collection.collectionId}
            rows={collection.actions.map((a) => ({
              id: a.actionId,
              title: a.title,
              stepCount: a.stepCount,
              missingInEnvironments: a.missingInEnvironments,
              stepCountMismatches: a.stepCountMismatches,
              unusedVariables: a.unusedVariables,
            }))}
            emptyLabel="Nenhuma action nesta collection."
          />
        ))}

        {systemActions.length > 0 && (
          <ActionSection
            sessionId={sessionId!}
            title="Actions de sistema"
            rows={systemActions.map((a) => ({
              id: a.actionId,
              title: a.title,
              stepCount: a.stepCount,
              missingInEnvironments: a.missingInEnvironments,
              stepCountMismatches: a.stepCountMismatches,
              unusedVariables: a.unusedVariables,
            }))}
          />
        )}

        {orphanActions.length > 0 && (
          <ActionSection
            sessionId={sessionId!}
            title="Sem collection"
            rows={orphanActions.map((a) => ({
              id: a.actionId,
              title: a.title,
              stepCount: a.stepCount,
              missingInEnvironments: a.missingInEnvironments,
              stepCountMismatches: a.stepCountMismatches,
              unusedVariables: a.unusedVariables,
            }))}
          />
        )}

        <VariablesSection sessionId={sessionId!} variables={variables} />
      </div>
    </div>
  )
}

function VariablesSection({ sessionId, variables }: { sessionId: string; variables: VariableUsage[] }) {
  if (variables.length === 0) return null

  return (
    <section>
      <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
        Variáveis <span className="font-normal normal-case text-slate-400">(workspace.variables)</span>
      </h2>
      <div className="mt-3 overflow-hidden rounded-lg border border-slate-200 bg-white">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 bg-slate-50 text-xs font-medium uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-4 py-2">Variável</th>
              <th className="px-4 py-2">Tipo</th>
              <th className="px-4 py-2">Setada (set)</th>
              <th className="px-4 py-2">Usada depois (get)</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {variables.map((v) => (
              <tr key={v.variable} className={!v.isUsed ? 'bg-purple-50/40' : undefined}>
                <td className="px-4 py-2 font-mono text-xs text-slate-700">{v.variable}</td>
                <td className="px-4 py-2 text-xs text-slate-500">{v.dataType ?? '—'}</td>
                <td className="px-4 py-2">
                  <VariableActionsCell
                    sessionId={sessionId}
                    ok={v.isSet}
                    actionIds={v.setInActions}
                    hasDefaultValue={v.hasDefaultValue}
                  />
                </td>
                <td className="px-4 py-2">
                  <VariableActionsCell sessionId={sessionId} ok={v.isUsed} actionIds={v.usedInActions} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function VariableActionsCell({
  sessionId,
  ok,
  actionIds,
  hasDefaultValue,
}: {
  sessionId: string
  ok: boolean
  actionIds: string[]
  hasDefaultValue?: boolean
}) {
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <span className={`text-xs font-semibold ${ok ? 'text-emerald-600' : 'text-red-600'}`}>{ok ? '✓' : '✗'}</span>
      {hasDefaultValue && (
        <span
          title="Definida via initial_value da declaração, não por nenhuma action"
          className="inline-flex shrink-0 items-center gap-1 rounded-full bg-sky-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-sky-700"
        >
          ℹ default
        </span>
      )}
      {actionIds.map((actionId) => (
        <Link
          key={actionId}
          to={`/workspaces/${sessionId}/actions/${actionId}`}
          className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] text-slate-600 hover:bg-slate-200"
        >
          {actionId}
        </Link>
      ))}
    </div>
  )
}

type ActionRow = {
  id: string
  title: string | null
  stepCount: number
  missingInEnvironments: string[]
  stepCountMismatches: EnvironmentStepCount[]
  unusedVariables: string[]
}

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
                <div className="flex items-center gap-2">
                  <p className="font-medium text-slate-800">{row.title ?? row.id}</p>
                  <EnvironmentFlag missingInEnvironments={row.missingInEnvironments} />
                  <StepCountMismatchFlag stepCount={row.stepCount} mismatches={row.stepCountMismatches} />
                  <UnusedVariablesFlag unusedVariables={row.unusedVariables} />
                </div>
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
