import { Handle, Position, type NodeProps } from '@xyflow/react'
import type { GroupColor } from '../../lib/graphColors'
import type { EnvironmentStepCount } from '../../lib/types'

export interface ActionNodeData {
  [key: string]: unknown
  label: string
  actionId: string
  isSystem: boolean
  stepCount: number
  color: GroupColor
  missingInEnvironments: string[]
  stepCountMismatches: EnvironmentStepCount[]
  unusedVariables: string[]
}

export function ActionNode({ data }: NodeProps) {
  const d = data as ActionNodeData
  const isMissing = d.missingInEnvironments.length > 0
  const hasStepMismatch = d.stepCountMismatches.length > 0
  const hasUnusedVariables = d.unusedVariables.length > 0
  const borderColor = isMissing ? '#dc2626' : hasStepMismatch ? '#d97706' : d.color.border
  const ringClass = isMissing ? 'ring-2 ring-red-500' : hasStepMismatch ? 'ring-2 ring-amber-500' : ''

  const tooltip = [
    isMissing ? `Não existe em: ${d.missingInEnvironments.join(', ')}` : null,
    hasStepMismatch
      ? `Steps diferentes: ${d.stepCountMismatches.map((m) => `${m.env} (${m.stepCount})`).join(', ')}`
      : null,
    hasUnusedVariables ? `Atribuídas mas nunca lidas: ${d.unusedVariables.join(', ')}` : null,
  ]
    .filter(Boolean)
    .join(' · ')

  return (
    <div
      style={{ background: d.color.bg, borderColor }}
      className={`w-[220px] rounded-lg border-2 px-3 py-2 shadow-sm ${ringClass}`}
      title={tooltip || undefined}
    >
      <Handle type="target" position={Position.Left} className="!h-2 !w-2 !border-none !bg-slate-400" />
      <div className="flex items-center gap-1">
        <p className="truncate text-sm font-semibold" style={{ color: d.color.text }}>
          {d.label}
        </p>
        {isMissing && <span className="shrink-0 text-xs text-red-600">⚠</span>}
        {!isMissing && hasStepMismatch && <span className="shrink-0 text-xs text-amber-600">⚠</span>}
        {!isMissing && !hasStepMismatch && hasUnusedVariables && (
          <span className="shrink-0 text-xs text-purple-600">⚠</span>
        )}
      </div>
      <p className="mt-0.5 truncate font-mono text-[10px] text-slate-500">{d.actionId}</p>
      <p className="mt-1 text-[10px] text-slate-500">
        {d.stepCount} step(s){d.isSystem ? ' · sistema' : ''}
      </p>
      {isMissing && (
        <p className="mt-1 truncate text-[10px] font-semibold text-red-600">
          falta em {d.missingInEnvironments.join(', ')}
        </p>
      )}
      {hasStepMismatch && (
        <p className="mt-1 truncate text-[10px] font-semibold text-amber-600">
          steps diferentes ({d.stepCountMismatches.map((m) => `${m.env}: ${m.stepCount}`).join(', ')})
        </p>
      )}
      {hasUnusedVariables && (
        <p className="mt-1 truncate text-[10px] font-semibold text-purple-600">
          {d.unusedVariables.length} variável(is) não usada(s)
        </p>
      )}
      <Handle type="source" position={Position.Right} className="!h-2 !w-2 !border-none !bg-slate-400" />
    </div>
  )
}

export interface StepNodeData {
  [key: string]: unknown
  label: string
  stepId: string
  actionId: string
  resolverType: string
  color: GroupColor
}

export function StepNode({ data }: NodeProps) {
  const d = data as StepNodeData
  return (
    <div
      style={{ background: d.color.bg, borderColor: d.color.border }}
      className="w-[200px] rounded-lg border-2 px-3 py-2 shadow-sm"
    >
      <Handle type="target" position={Position.Left} className="!h-2 !w-2 !border-none !bg-slate-400" />
      <p className="truncate text-xs font-semibold" style={{ color: d.color.text }}>
        {d.label}
      </p>
      <p className="mt-0.5 truncate font-mono text-[10px] text-slate-500">
        {d.actionId} · {d.stepId}
      </p>
      <p className="mt-1 text-[10px] uppercase tracking-wide text-slate-500">{d.resolverType}</p>
      <Handle type="source" position={Position.Right} className="!h-2 !w-2 !border-none !bg-slate-400" />
    </div>
  )
}

