import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { EnvironmentFlag, StepCountMismatchFlag, UnusedVariablesFlag } from '../components/EnvironmentFlag'
import { IssueList } from '../components/IssueList'
import {
  ApiError,
  copyOrMoveAction,
  deleteAction,
  getActionDetail,
  getActions,
  getCollections,
  renameAction,
} from '../lib/api'
import type {
  ActionSummary,
  CollectionSummary,
  CopyActionResponse,
  CopyMode,
  ReferenceStrategy,
  ValidationIssue,
  WatsonAction,
  WatsonStep,
} from '../lib/types'
import { SYSTEM_ACTION_IDS, extractOutputText, formatCondition, isFreeTextQuestion, rootActionId } from '../lib/watson'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; message: string }

export function ActionDetail() {
  const { sessionId, actionId } = useParams<{ sessionId: string; actionId: string }>()
  const navigate = useNavigate()

  const [state, setState] = useState<LoadState>({ status: 'loading' })
  const [action, setAction] = useState<WatsonAction | null>(null)
  const [collections, setCollections] = useState<CollectionSummary[]>([])
  const [actions, setActions] = useState<ActionSummary[]>([])
  const [lastIssues, setLastIssues] = useState<ValidationIssue[]>([])

  useEffect(() => {
    if (!sessionId || !actionId) return
    let cancelled = false
    setState({ status: 'loading' })
    setLastIssues([])

    Promise.all([getActionDetail(sessionId, actionId), getCollections(sessionId), getActions(sessionId)])
      .then(([loadedAction, loadedCollections, loadedActions]) => {
        if (cancelled) return
        setAction(loadedAction)
        setCollections(loadedCollections)
        setActions(loadedActions)
        setState({ status: 'ready' })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setState({
          status: 'error',
          message: error instanceof ApiError ? error.message : 'Falha ao carregar a action.',
        })
      })

    return () => {
      cancelled = true
    }
  }, [sessionId, actionId])

  const backLink = (
    <Link to={`/workspaces/${sessionId}`} className="text-sm font-medium text-indigo-600 hover:text-indigo-700">
      ← Voltar para o workspace
    </Link>
  )

  if (state.status === 'loading') {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        {backLink}
        <p className="mt-6 text-sm text-slate-500">Carregando...</p>
      </div>
    )
  }

  if (state.status === 'error' || !action || !sessionId) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        {backLink}
        <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="text-sm font-medium text-red-800">
            {state.status === 'error' ? state.message : 'Action não encontrada.'}
          </p>
        </div>
      </div>
    )
  }

  const isSystem = SYSTEM_ACTION_IDS.has(action.action)
  const conditionLabel = formatCondition(action.condition)
  const currentActionSummary = actions.find((a) => a.actionId === action.action)
  const missingInEnvironments = currentActionSummary?.missingInEnvironments ?? []
  const stepCountMismatches = currentActionSummary?.stepCountMismatches ?? []
  const unusedVariables = currentActionSummary?.unusedVariables ?? []

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      {backLink}

      <div className="mt-4 flex items-start justify-between gap-4">
        <TitleHeading
          action={action}
          sessionId={sessionId}
          disabled={isSystem}
          onRenamed={(updated, issues) => {
            setAction(updated)
            setLastIssues(issues)
          }}
        />
        {isSystem && (
          <span className="shrink-0 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
            action de sistema
          </span>
        )}
        <EnvironmentFlag missingInEnvironments={missingInEnvironments} />
        <StepCountMismatchFlag stepCount={action.steps.length} mismatches={stepCountMismatches} />
        <UnusedVariablesFlag unusedVariables={unusedVariables} />
      </div>

      <dl className="mt-4 flex flex-wrap gap-x-6 gap-y-1 text-xs text-slate-500">
        {conditionLabel && (
          <div>
            <dt className="inline font-medium">condição </dt>
            <dd className="inline">
              <code className="rounded bg-slate-100 px-1 py-0.5">{conditionLabel}</code>
            </dd>
          </div>
        )}
        {action.next_action && (
          <div>
            <dt className="inline font-medium">próxima action </dt>
            <dd className="inline font-mono">{action.next_action}</dd>
          </div>
        )}
        {action.launch_mode && (
          <div>
            <dt className="inline font-medium">launch mode </dt>
            <dd className="inline">{action.launch_mode}</dd>
          </div>
        )}
      </dl>

      <div className="mt-4">
        <IssueList issues={lastIssues} />
      </div>

      {!isSystem && (
        <div className="mt-6 space-y-4">
          <CopyMoveForm sessionId={sessionId} action={action} collections={collections} onIssues={setLastIssues} />
          <DeleteControl
            sessionId={sessionId}
            action={action}
            onDeleted={() => navigate(`/workspaces/${sessionId}`)}
          />
        </div>
      )}

      <ol className="mt-8 space-y-3">
        {action.steps.map((step, index) => (
          <StepCard key={step.step} step={step} index={index} unusedVariables={unusedVariables} />
        ))}
        {action.steps.length === 0 && <p className="text-sm text-slate-400">Esta action não tem steps.</p>}
      </ol>
    </div>
  )
}

function TitleHeading({
  action,
  sessionId,
  disabled,
  onRenamed,
}: {
  action: WatsonAction
  sessionId: string
  disabled: boolean
  onRenamed: (action: WatsonAction, issues: ValidationIssue[]) => void
}) {
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState(action.title ?? action.action)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!editing) {
    return (
      <div>
        <div className="flex items-center gap-2">
          <h1 className="text-2xl font-semibold text-slate-900">{action.title ?? action.action}</h1>
          {!disabled && (
            <button
              type="button"
              onClick={() => {
                setValue(action.title ?? action.action)
                setEditing(true)
              }}
              className="text-xs font-medium text-indigo-600 hover:text-indigo-700"
            >
              editar
            </button>
          )}
        </div>
        <p className="mt-1 font-mono text-xs text-slate-400">{action.action}</p>
      </div>
    )
  }

  async function save() {
    setSaving(true)
    setError(null)
    try {
      const result = await renameAction(sessionId, action.action, value)
      onRenamed(result.action, result.issues)
      setEditing(false)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Falha ao renomear.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="w-full">
      <div className="flex items-center gap-2">
        <input
          value={value}
          onChange={(event) => setValue(event.target.value)}
          disabled={saving}
          autoFocus
          className="flex-1 rounded-md border border-slate-300 px-2 py-1 text-lg font-semibold text-slate-900 focus:border-indigo-400 focus:outline-none"
        />
        <button
          type="button"
          onClick={() => void save()}
          disabled={saving || value.trim().length === 0}
          className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-50"
        >
          Salvar
        </button>
        <button
          type="button"
          onClick={() => setEditing(false)}
          disabled={saving}
          className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
        >
          Cancelar
        </button>
      </div>
      <p className="mt-1 font-mono text-xs text-slate-400">{action.action}</p>
      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
    </div>
  )
}

function CopyMoveForm({
  sessionId,
  action,
  collections,
  onIssues,
}: {
  sessionId: string
  action: WatsonAction
  collections: CollectionSummary[]
  onIssues: (issues: ValidationIssue[]) => void
}) {
  const navigate = useNavigate()
  const [targetCollection, setTargetCollection] = useState(collections[0]?.collectionId ?? '')
  const [mode, setMode] = useState<CopyMode>('copy')
  const [titlePrefix, setTitlePrefix] = useState('')
  const [referenceStrategy, setReferenceStrategy] = useState<ReferenceStrategy>('keep')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CopyActionResponse | null>(null)
  const [replacedId, setReplacedId] = useState<string | null>(null)
  const [conflict, setConflict] = useState<{ actionId: string; title: string | null } | null>(null)

  async function submit() {
    if (!targetCollection) {
      setError('Escolha a collection destino.')
      return
    }

    if (mode === 'copy') {
      const target = collections.find((c) => c.collectionId === targetCollection)
      const sourceRoot = rootActionId(action.action)
      const existing = target?.actions.find(
        (a) => a.actionId !== action.action && rootActionId(a.actionId) === sourceRoot,
      )
      if (existing) {
        setConflict(existing)
        return
      }
    }

    await runCopyOrMove(null)
  }

  async function runCopyOrMove(replaceActionId: string | null) {
    setConflict(null)
    setSubmitting(true)
    setError(null)
    setResult(null)
    try {
      const response = await copyOrMoveAction(sessionId, action.action, {
        targetCollection,
        mode,
        titlePrefix: mode === 'copy' && titlePrefix.trim().length > 0 ? titlePrefix : null,
        referenceStrategy,
        replaceActionId,
      })
      setResult(response)
      setReplacedId(replaceActionId)
      onIssues(response.issues)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Falha ao copiar/mover a action.')
    } finally {
      setSubmitting(false)
    }
  }

  if (collections.length === 0) return null

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      {conflict && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4">
          <div className="w-full max-w-sm rounded-lg bg-white p-5 shadow-xl">
            <h3 className="text-sm font-semibold text-slate-900">Já existe uma cópia nesta collection</h3>
            <p className="mt-2 text-sm text-slate-600">
              <span className="font-mono">{conflict.actionId}</span>
              {conflict.title ? ` (${conflict.title})` : ''} parece ser uma cópia desta mesma action. Deseja
              substituí-la pelo conteúdo atual ou criar uma nova cópia?
            </p>
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setConflict(null)}
                className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={() => void runCopyOrMove(null)}
                className="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                Nova cópia
              </button>
              <button
                type="button"
                onClick={() => void runCopyOrMove(conflict.actionId)}
                className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500"
              >
                Substituir
              </button>
            </div>
          </div>
        </div>
      )}

      <h2 className="text-sm font-semibold text-slate-800">Copiar ou mover</h2>
      <div className="mt-3 flex flex-wrap items-end gap-3">
        <label className="flex flex-col gap-1 text-xs text-slate-600">
          Collection destino
          <select
            value={targetCollection}
            onChange={(event) => setTargetCollection(event.target.value)}
            className="rounded-md border border-slate-300 px-2 py-1.5 text-sm"
          >
            {collections.map((collection) => (
              <option key={collection.collectionId} value={collection.collectionId}>
                {collection.title ?? collection.collectionId}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-xs text-slate-600">
          Modo
          <select
            value={mode}
            onChange={(event) => setMode(event.target.value as CopyMode)}
            className="rounded-md border border-slate-300 px-2 py-1.5 text-sm"
          >
            <option value="copy">Copiar</option>
            <option value="move">Mover</option>
          </select>
        </label>

        {mode === 'copy' && (
          <>
            <label className="flex flex-col gap-1 text-xs text-slate-600">
              Prefixo do título (opcional)
              <input
                value={titlePrefix}
                onChange={(event) => setTitlePrefix(event.target.value)}
                placeholder="prod/"
                className="w-32 rounded-md border border-slate-300 px-2 py-1.5 text-sm"
              />
            </label>

            <label className="flex flex-col gap-1 text-xs text-slate-600">
              Referências
              <select
                value={referenceStrategy}
                onChange={(event) => setReferenceStrategy(event.target.value as ReferenceStrategy)}
                className="rounded-md border border-slate-300 px-2 py-1.5 text-sm"
              >
                <option value="keep">Manter</option>
                <option value="remap">Remapear</option>
              </select>
            </label>
          </>
        )}

        <button
          type="button"
          onClick={() => void submit()}
          disabled={submitting}
          className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-50"
        >
          {submitting ? 'Executando...' : 'Executar'}
        </button>
      </div>

      {error && <p className="mt-3 text-xs text-red-600">{error}</p>}

      {result && (
        <div className="mt-3 space-y-2">
          <p className="text-xs text-emerald-700">
            {mode === 'copy' ? (
              <>
                {replacedId ? 'Action substituída: ' : 'Action copiada como '}
                <span className="font-mono">{result.action.action}</span>.{' '}
                <button
                  type="button"
                  onClick={() => navigate(`/workspaces/${sessionId}/actions/${result.action.action}`)}
                  className="font-medium text-indigo-600 hover:text-indigo-700"
                >
                  {replacedId ? 'Ver action' : 'Ver action copiada'}
                </button>
              </>
            ) : (
              'Action movida com sucesso.'
            )}
          </p>
          {result.warnings.length > 0 && (
            <ul className="space-y-1 text-xs text-amber-700">
              {result.warnings.map((warning, index) => (
                <li key={index}>⚠ {warning}</li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}

function DeleteControl({
  sessionId,
  action,
  onDeleted,
}: {
  sessionId: string
  action: WatsonAction
  onDeleted: () => void
}) {
  const [phase, setPhase] = useState<'idle' | 'confirming' | 'blocked' | 'deleting' | 'error'>('idle')
  const [referencedBy, setReferencedBy] = useState<string[]>([])
  const [message, setMessage] = useState('')

  async function performDelete(force: boolean) {
    setPhase('deleting')
    try {
      await deleteAction(sessionId, action.action, force)
      onDeleted()
    } catch (err) {
      if (err instanceof ApiError && err.status === 409 && err.problem?.referencedBy) {
        setReferencedBy(err.problem.referencedBy)
        setPhase('blocked')
        return
      }
      setMessage(err instanceof ApiError ? err.message : 'Falha ao excluir a action.')
      setPhase('error')
    }
  }

  if (phase === 'idle') {
    return (
      <div>
        <button
          type="button"
          onClick={() => setPhase('confirming')}
          className="text-sm font-medium text-red-600 hover:text-red-700"
        >
          Excluir action
        </button>
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-red-200 bg-red-50 p-4">
      {phase === 'confirming' && (
        <>
          <p className="text-sm text-red-800">Excluir esta action? Essa operação não pode ser desfeita.</p>
          <div className="mt-3 flex gap-2">
            <button
              type="button"
              onClick={() => void performDelete(false)}
              className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-500"
            >
              Excluir
            </button>
            <button
              type="button"
              onClick={() => setPhase('idle')}
              className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
            >
              Cancelar
            </button>
          </div>
        </>
      )}

      {phase === 'deleting' && <p className="text-sm text-red-800">Excluindo...</p>}

      {phase === 'blocked' && (
        <>
          <p className="text-sm font-medium text-red-800">
            Esta action é referenciada em {referencedBy.length} ponto(s):
          </p>
          <ul className="mt-2 space-y-1 text-xs text-red-700">
            {referencedBy.map((ref) => (
              <li key={ref} className="font-mono">
                {ref}
              </li>
            ))}
          </ul>
          <div className="mt-3 flex gap-2">
            <button
              type="button"
              onClick={() => void performDelete(true)}
              className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-500"
            >
              Excluir mesmo assim
            </button>
            <button
              type="button"
              onClick={() => setPhase('idle')}
              className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100"
            >
              Cancelar
            </button>
          </div>
        </>
      )}

      {phase === 'error' && (
        <>
          <p className="text-sm text-red-800">{message}</p>
          <button
            type="button"
            onClick={() => setPhase('idle')}
            className="mt-2 text-sm font-medium text-slate-600 hover:text-slate-800"
          >
            Fechar
          </button>
        </>
      )}
    </div>
  )
}

function StepCard({ step, index, unusedVariables }: { step: WatsonStep; index: number; unusedVariables: string[] }) {
  const conditionLabel = formatCondition(step.condition)
  const outputText = extractOutputText(step.output)
  const invoke = step.resolver.invoke_action
  const unusedByThisStep = [step.variable, invoke?.result_variable].filter(
    (name): name is string => Boolean(name) && unusedVariables.includes(name!),
  )

  return (
    <li className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-100 text-[10px] font-semibold text-slate-500">
            {index + 1}
          </span>
          <p className="text-sm font-medium text-slate-800">{step.title ?? step.step}</p>
        </div>
        <span className="shrink-0 rounded bg-indigo-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-indigo-600">
          {step.resolver.type}
        </span>
      </div>
      <p className="mt-1 font-mono text-[11px] text-slate-400">{step.step}</p>

      {conditionLabel && (
        <p className="mt-2 text-xs text-slate-600">
          <span className="font-medium">se </span>
          <code className="rounded bg-slate-100 px-1 py-0.5">{conditionLabel}</code>
        </p>
      )}

      {outputText && <p className="mt-2 rounded-md bg-slate-50 p-2 text-xs italic text-slate-600">"{outputText}"</p>}

      {unusedByThisStep.length > 0 && (
        <p className="mt-2 text-xs text-purple-700">
          ⚠ atribui <code className="rounded bg-purple-50 px-1">{unusedByThisStep.join(', ')}</code>, nunca lida depois
        </p>
      )}

      {isFreeTextQuestion(step.question) && (
        <p className="mt-2 text-xs text-slate-500">Aguarda resposta livre do usuário.</p>
      )}

      {(step.next_step || invoke) && (
        <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
          {step.next_step && (
            <span>
              próximo step: <span className="font-mono text-slate-700">{step.next_step}</span>
            </span>
          )}
          {invoke && (
            <span>
              invoca: <span className="font-mono text-slate-700">{invoke.action}</span>
              {invoke.result_variable && (
                <>
                  {' '}
                  → <span className="font-mono text-slate-700">{invoke.result_variable}</span>
                </>
              )}
            </span>
          )}
        </div>
      )}
    </li>
  )
}
