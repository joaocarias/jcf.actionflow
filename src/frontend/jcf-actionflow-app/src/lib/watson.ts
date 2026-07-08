/** Watson Actions reserves these ids for lifecycle hooks; mirrors Core.Domain.SystemActions. */
export const SYSTEM_ACTION_IDS = new Set(['welcome', 'fallback', 'anything_else', 'run_always'])

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
}

function formatOperand(operand: unknown): string {
  if (!isPlainObject(operand)) return JSON.stringify(operand)

  const entry = Object.entries(operand)[0]
  if (!entry) return JSON.stringify(operand)
  const [key, value] = entry
  const text = typeof value === 'string' ? value : JSON.stringify(value)

  switch (key) {
    case 'scalar':
      return text
    case 'skill_variable':
      return `$${text}`
    case 'system_variable':
      return `$sys.${text}`
    default:
      return text
  }
}

/**
 * Renders a step/action condition into a short label — mirrors the backend's
 * ConditionFormatter so the raw JSON (`{"intent": ...}`, `{"neq": [...]}`, ...) reads the
 * same way in both places.
 */
export function formatCondition(condition: unknown): string | null {
  if (!isPlainObject(condition)) return null

  if (typeof condition.intent === 'string') return `intent: ${condition.intent}`
  if (typeof condition.expression === 'string') return condition.expression
  if (typeof condition.entity === 'string') return `entity: ${condition.entity}`

  const entry = Object.entries(condition)[0]
  if (!entry) return null
  const [operator, operands] = entry

  return Array.isArray(operands)
    ? `${operator}(${operands.map(formatOperand).join(', ')})`
    : `${operator}(${formatOperand(operands)})`
}

/** Best-effort extraction of the user-facing text out of a step's `output.generic[]`. */
export function extractOutputText(output: unknown): string | null {
  if (!isPlainObject(output) || !Array.isArray(output.generic)) return null

  const texts: string[] = []
  for (const item of output.generic) {
    if (!isPlainObject(item) || !Array.isArray(item.values)) continue

    for (const value of item.values) {
      if (!isPlainObject(value)) continue

      if (typeof value.text === 'string' && value.text.length > 0) {
        texts.push(value.text)
        continue
      }

      const concat = isPlainObject(value.text_expression) ? value.text_expression.concat : undefined
      if (Array.isArray(concat)) {
        texts.push(concat.map(formatOperand).join(''))
      }
    }
  }

  return texts.length > 0 ? texts.join(' / ') : null
}

export function isFreeTextQuestion(question: unknown): boolean {
  return isPlainObject(question) && question.free_text === true
}
