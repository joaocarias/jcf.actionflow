import ELK from 'elkjs/lib/elk.bundled.js'
import type { GraphEdge, GraphNode } from './types'

const elk = new ELK()

export type NodePosition = { x: number; y: number }

export async function layoutGraph(
  nodes: GraphNode[],
  edges: GraphEdge[],
  nodeSize: { width: number; height: number },
): Promise<Map<string, NodePosition>> {
  const elkGraph = {
    id: 'root',
    layoutOptions: {
      'elk.algorithm': 'layered',
      'elk.direction': 'RIGHT',
      'elk.spacing.nodeNode': '32',
      'elk.layered.spacing.nodeNodeBetweenLayers': '96',
      'elk.layered.spacing.edgeNodeBetweenLayers': '40',
    },
    children: nodes.map((node) => ({ id: node.id, width: nodeSize.width, height: nodeSize.height })),
    edges: edges.map((edge) => ({ id: edge.id, sources: [edge.source], targets: [edge.target] })),
  }

  const layouted = await elk.layout(elkGraph)

  const positions = new Map<string, NodePosition>()
  for (const child of layouted.children ?? []) {
    positions.set(child.id, { x: child.x ?? 0, y: child.y ?? 0 })
  }
  return positions
}
