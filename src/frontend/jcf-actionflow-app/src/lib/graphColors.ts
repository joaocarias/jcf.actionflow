export interface GroupColor {
  bg: string
  border: string
  text: string
}

const PALETTE: GroupColor[] = [
  { bg: '#eef2ff', border: '#6366f1', text: '#3730a3' }, // indigo
  { bg: '#ecfdf5', border: '#10b981', text: '#065f46' }, // emerald
  { bg: '#fffbeb', border: '#f59e0b', text: '#92400e' }, // amber
  { bg: '#fdf2f8', border: '#ec4899', text: '#9d174d' }, // pink
  { bg: '#eff6ff', border: '#3b82f6', text: '#1e40af' }, // blue
  { bg: '#f5f3ff', border: '#8b5cf6', text: '#5b21b6' }, // violet
  { bg: '#f0fdfa', border: '#14b8a6', text: '#115e59' }, // teal
  { bg: '#fff7ed', border: '#f97316', text: '#9a3412' }, // orange
]

const SYSTEM_COLOR: GroupColor = { bg: '#f1f5f9', border: '#64748b', text: '#334155' }

function hash(value: string): number {
  let h = 0
  for (let i = 0; i < value.length; i++) {
    h = (Math.imul(h, 31) + value.charCodeAt(i)) | 0
  }
  return Math.abs(h)
}

/** Stable color per graph group (collection id, "system", "unassigned", or an actionId). */
export function colorForGroup(group: string | null | undefined): GroupColor {
  if (!group || group === 'system') return SYSTEM_COLOR
  return PALETTE[hash(group) % PALETTE.length]
}
