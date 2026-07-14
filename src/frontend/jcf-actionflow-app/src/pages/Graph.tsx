import { Background, Controls, MarkerType, MiniMap, ReactFlow, ReactFlowProvider, type Edge, type Node } from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ActionNode, StepNode, type ActionNodeData, type StepNodeData } from '../components/graph/GraphNodes'
import { ApiError, getGraph } from '../lib/api'
import { colorForGroup } from '../lib/graphColors'
import { layoutGraph } from '../lib/layout'
import type { EnvironmentStepCount, FlowGraph } from '../lib/types'

type GraphLevel = 'actions' | 'steps'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; nodes: Node[]; edges: Edge[] }
  | { status: 'error'; message: string }

const ACTION_NODE_SIZE = { width: 220, height: 72 }
const STEP_NODE_SIZE = { width: 200, height: 66 }
const nodeTypes = { action: ActionNode, step: StepNode }

export function GraphView() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const navigate = useNavigate()
  const [level, setLevel] = useState<GraphLevel>('actions')
  const [state, setState] = useState<LoadState>({ status: 'loading' })

  useEffect(() => {
    if (!sessionId) return
    let cancelled = false
    setState({ status: 'loading' })

    getGraph(sessionId, level)
      .then(async (graph: FlowGraph) => {
        const size = level === 'actions' ? ACTION_NODE_SIZE : STEP_NODE_SIZE
        const positions = await layoutGraph(graph.nodes, graph.edges, size)
        if (cancelled) return

        const nodes: Node[] = graph.nodes.map((node) => {
          const position = positions.get(node.id) ?? { x: 0, y: 0 }
          const color = colorForGroup(node.group)

          if (level === 'actions') {
            const data: ActionNodeData = {
              label: node.label,
              actionId: node.id,
              isSystem: Boolean(node.data.isSystem),
              stepCount: Number(node.data.stepCount ?? 0),
              color,
              missingInEnvironments: Array.isArray(node.data.missingInEnvironments)
                ? (node.data.missingInEnvironments as string[])
                : [],
              stepCountMismatches: Array.isArray(node.data.stepCountMismatches)
                ? (node.data.stepCountMismatches as EnvironmentStepCount[])
                : [],
              unusedVariables: Array.isArray(node.data.unusedVariables) ? (node.data.unusedVariables as string[]) : [],
            }
            return { id: node.id, type: 'action', position, data }
          }

          const data: StepNodeData = {
            label: node.label,
            stepId: String(node.data.stepId ?? node.id),
            actionId: String(node.data.actionId ?? node.group ?? ''),
            resolverType: String(node.data.resolverType ?? ''),
            color: colorForGroup(node.group),
          }
          return { id: node.id, type: 'step', position, data }
        })

        const edges: Edge[] = graph.edges.map((edge) => {
          const strokeColor = edge.weak ? '#cbd5e1' : edge.kind === 'invoke' ? '#6366f1' : '#94a3b8'
          return {
            id: edge.id,
            source: edge.source,
            target: edge.target,
            label: edge.label ?? undefined,
            animated: edge.kind === 'invoke',
            style: { stroke: strokeColor, strokeWidth: edge.weak ? 1 : 1.5, strokeDasharray: edge.weak ? '4 4' : undefined },
            markerEnd: { type: MarkerType.ArrowClosed, color: strokeColor },
            labelStyle: { fontSize: 10, fill: '#475569' },
            labelBgStyle: { fill: '#ffffff', fillOpacity: 0.85 },
          }
        })

        setState({ status: 'ready', nodes, edges })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setState({
          status: 'error',
          message: error instanceof ApiError ? error.message : 'Falha ao carregar o grafo.',
        })
      })

    return () => {
      cancelled = true
    }
  }, [sessionId, level])

  const levelToggle = useMemo(
    () => (
      <div className="inline-flex rounded-md border border-slate-300 bg-white p-0.5 text-sm">
        {(['actions', 'steps'] as const).map((option) => (
          <button
            key={option}
            type="button"
            onClick={() => setLevel(option)}
            className={`rounded px-3 py-1 font-medium transition-colors ${
              level === option ? 'bg-indigo-600 text-white' : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            {option === 'actions' ? 'Actions' : 'Steps'}
          </button>
        ))}
      </div>
    ),
    [level],
  )

  return (
    <div className="flex h-[calc(100vh-56px)] flex-col">
      <div className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
        <Link to={`/workspaces/${sessionId}`} className="text-sm font-medium text-indigo-600 hover:text-indigo-700">
          ← Voltar para o workspace
        </Link>
        {levelToggle}
      </div>

      <div className="relative flex-1">
        {state.status === 'loading' && (
          <p className="absolute left-1/2 top-8 -translate-x-1/2 text-sm text-slate-500">Carregando grafo...</p>
        )}

        {state.status === 'error' && (
          <div className="absolute left-1/2 top-8 -translate-x-1/2 rounded-lg border border-red-200 bg-red-50 px-4 py-3">
            <p className="text-sm font-medium text-red-800">{state.message}</p>
          </div>
        )}

        {state.status === 'ready' && (
          <ReactFlowProvider>
            <ReactFlow
              nodes={state.nodes}
              edges={state.edges}
              nodeTypes={nodeTypes}
              fitView
              onNodeClick={(_, node) => {
                const actionId = level === 'actions' ? node.id : ((node.data as { actionId?: string }).actionId ?? node.id)
                navigate(`/workspaces/${sessionId}/actions/${actionId}`)
              }}
            >
              <Background />
              <Controls />
              <MiniMap pannable zoomable className="!bg-white" />
            </ReactFlow>
          </ReactFlowProvider>
        )}
      </div>
    </div>
  )
}
