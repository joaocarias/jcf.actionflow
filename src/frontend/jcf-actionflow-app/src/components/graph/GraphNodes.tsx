import { Handle, Position, type NodeProps } from '@xyflow/react'
import type { GroupColor } from '../../lib/graphColors'

export interface ActionNodeData {
  [key: string]: unknown
  label: string
  actionId: string
  isSystem: boolean
  stepCount: number
  color: GroupColor
}

export function ActionNode({ data }: NodeProps) {
  const d = data as ActionNodeData
  return (
    <div
      style={{ background: d.color.bg, borderColor: d.color.border }}
      className="w-[220px] rounded-lg border-2 px-3 py-2 shadow-sm"
    >
      <Handle type="target" position={Position.Left} className="!h-2 !w-2 !border-none !bg-slate-400" />
      <p className="truncate text-sm font-semibold" style={{ color: d.color.text }}>
        {d.label}
      </p>
      <p className="mt-0.5 truncate font-mono text-[10px] text-slate-500">{d.actionId}</p>
      <p className="mt-1 text-[10px] text-slate-500">
        {d.stepCount} step(s){d.isSystem ? ' · sistema' : ''}
      </p>
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

