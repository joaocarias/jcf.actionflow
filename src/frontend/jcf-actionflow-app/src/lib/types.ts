export type IssueSeverity = 'Warning' | 'Error'

export interface ValidationIssue {
  severity: IssueSeverity
  code: string
  message: string
  actionId?: string | null
  stepId?: string | null
}

export interface WorkspaceSummary {
  sessionId: string
  name: string | null
  actionCount: number
  businessActionCount: number
  systemActionCount: number
  intentCount: number
  variableCount: number
  collectionCount: number
}

export interface ImportResponse {
  sessionId: string
  summary: WorkspaceSummary
  issues: ValidationIssue[]
}

export interface CollectionActionSummary {
  actionId: string
  title: string | null
  stepCount: number
}

export interface CollectionSummary {
  collectionId: string
  title: string | null
  actions: CollectionActionSummary[]
}

export interface ActionSummary {
  actionId: string
  title: string | null
  isSystemAction: boolean
  isOrphan: boolean
  collectionId: string | null
  stepCount: number
}

export interface GraphNode {
  id: string
  label: string
  group: string | null
  kind: string
  data: Record<string, unknown>
}

export interface GraphEdge {
  id: string
  source: string
  target: string
  kind: string
  label?: string | null
  weak: boolean
}

export interface FlowGraph {
  nodes: GraphNode[]
  edges: GraphEdge[]
}

export type CopyMode = 'copy' | 'move'
export type ReferenceStrategy = 'keep' | 'remap'

export interface CopyActionRequest {
  targetCollection: string
  mode: CopyMode
  titlePrefix?: string | null
  referenceStrategy: ReferenceStrategy
}

/**
 * Raw Watson objects (snake_case, passed through as-is by the backend). Only the fields
 * the UI actually reads are typed; everything else still comes through via the index
 * signature — the backend guarantees round-trip, not a fully modeled schema.
 */
export interface WatsonInvokeAction {
  action: string
  result_variable?: string | null
  policy?: string | null
  [key: string]: unknown
}

export interface WatsonResolver {
  type: string
  invoke_action?: WatsonInvokeAction | null
  [key: string]: unknown
}

export interface WatsonStep {
  step: string
  type?: string | null
  title?: string | null
  variable?: string | null
  condition?: unknown
  context?: unknown
  question?: unknown
  output?: unknown
  resolver: WatsonResolver
  next_step?: string | null
  handlers?: unknown[] | null
  max_hits?: number | null
  [key: string]: unknown
}

export interface WatsonAction {
  action: string
  title?: string | null
  type?: string | null
  condition?: unknown
  steps: WatsonStep[]
  next_action?: string | null
  launch_mode?: string | null
  disambiguation_opt_out?: boolean | null
  [key: string]: unknown
}

export interface CopyActionResponse {
  action: WatsonAction
  warnings: string[]
  issues: ValidationIssue[]
}

export interface ActionWriteResponse {
  action: WatsonAction
  issues: ValidationIssue[]
}

export interface DeleteActionResponse {
  actionId: string
  orphanedReferences: string[]
  issues: ValidationIssue[]
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  referencedBy?: string[]
  [key: string]: unknown
}
