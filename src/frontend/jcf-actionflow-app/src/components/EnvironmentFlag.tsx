import type { EnvironmentStepCount } from '../lib/types'

export function EnvironmentFlag({ missingInEnvironments }: { missingInEnvironments: string[] }) {
  if (missingInEnvironments.length === 0) return null

  return (
    <span
      title={`Não existe em: ${missingInEnvironments.join(', ')}`}
      className="inline-flex shrink-0 items-center gap-1 rounded-full bg-red-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-red-700"
    >
      ⚠ falta em {missingInEnvironments.join(', ')}
    </span>
  )
}

export function UnusedVariablesFlag({ unusedVariables }: { unusedVariables: string[] }) {
  if (unusedVariables.length === 0) return null

  return (
    <span
      title={`Atribuídas mas nunca lidas: ${unusedVariables.join(', ')}`}
      className="inline-flex shrink-0 items-center gap-1 rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-purple-700"
    >
      ⚠ {unusedVariables.length} variável(is) não usada(s)
    </span>
  )
}

export function StepCountMismatchFlag({ stepCount, mismatches }: { stepCount: number; mismatches: EnvironmentStepCount[] }) {
  if (mismatches.length === 0) return null

  const detail = mismatches.map((m) => `${m.env}: ${m.stepCount} step(s)`).join(', ')

  return (
    <span
      title={`Esta action tem ${stepCount} step(s) aqui, mas ${detail}`}
      className="inline-flex shrink-0 items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-700"
    >
      ⚠ steps diferentes ({detail})
    </span>
  )
}
