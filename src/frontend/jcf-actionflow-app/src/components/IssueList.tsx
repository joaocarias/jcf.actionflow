import type { ValidationIssue } from '../lib/types'

export function IssueList({ issues }: { issues: ValidationIssue[] }) {
  if (issues.length === 0) return null

  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
      <p className="text-sm font-medium text-amber-800">
        {issues.length} ponto(s) de atenção encontrados na validação
      </p>
      <ul className="mt-2 space-y-1.5 text-xs text-amber-700">
        {issues.map((issue, index) => (
          <li key={index}>
            <span
              className={`mr-1.5 rounded px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide ${
                issue.severity === 'Error' ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700'
              }`}
            >
              {issue.severity === 'Error' ? 'erro' : 'aviso'}
            </span>
            {issue.message}
          </li>
        ))}
      </ul>
    </div>
  )
}
