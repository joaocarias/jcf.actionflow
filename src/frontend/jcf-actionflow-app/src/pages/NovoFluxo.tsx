import { useRef, useState, type DragEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, importWorkspace } from '../lib/api'

type UploadState = { status: 'idle' } | { status: 'uploading' } | { status: 'error'; message: string }

export function NovoFluxo() {
  const [state, setState] = useState<UploadState>({ status: 'idle' })
  const [isDragging, setIsDragging] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()
  const isBusy = state.status === 'uploading'

  async function handleFile(file: File) {
    if (!file.name.toLowerCase().endsWith('.json')) {
      setState({ status: 'error', message: 'O arquivo precisa ter a extensão .json.' })
      return
    }

    setState({ status: 'uploading' })
    try {
      const result = await importWorkspace(file)
      navigate(`/workspaces/${result.sessionId}`)
    } catch (error) {
      setState({
        status: 'error',
        message: error instanceof ApiError ? error.message : 'Falha ao enviar o arquivo.',
      })
    }
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragging(false)
    if (isBusy) return
    const file = event.dataTransfer.files[0]
    if (file) void handleFile(file)
  }

  return (
    <div className="mx-auto max-w-2xl px-6 py-16">
      <h1 className="text-2xl font-semibold text-slate-900">Novo fluxo</h1>
      <p className="mt-2 text-sm text-slate-600">
        Envie o arquivo JSON exportado de uma skill de Actions do IBM Watson Assistant para
        importar o fluxo.
      </p>

      <div
        onDragOver={(event) => {
          event.preventDefault()
          if (!isBusy) setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => !isBusy && inputRef.current?.click()}
        role="button"
        tabIndex={0}
        aria-disabled={isBusy}
        onKeyDown={(event) => {
          if (!isBusy && (event.key === 'Enter' || event.key === ' ')) inputRef.current?.click()
        }}
        className={`mt-8 flex flex-col items-center justify-center rounded-xl border-2 border-dashed px-6 py-14 text-center transition-colors ${
          isBusy ? 'cursor-wait border-slate-200 bg-slate-50' : 'cursor-pointer'
        } ${
          !isBusy && isDragging
            ? 'border-indigo-400 bg-indigo-50'
            : !isBusy
              ? 'border-slate-300 bg-white hover:border-indigo-300 hover:bg-slate-50'
              : ''
        }`}
      >
        {isBusy ? (
          <svg className="h-10 w-10 animate-spin text-indigo-500" fill="none" viewBox="0 0 24 24" aria-hidden="true">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8V0C5.373 0 0 5.373 0 12h4Z" />
          </svg>
        ) : (
          <svg
            className="h-10 w-10 text-slate-400"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1.5}
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 16.5V9.75m0 0 3 3m-3-3-3 3M6.75 19.5a4.5 4.5 0 0 1-1.41-8.775 5.25 5.25 0 0 1 10.233-2.33 3.75 3.75 0 0 1 4.177 4.03A4.5 4.5 0 0 1 17.25 19.5H6.75Z"
            />
          </svg>
        )}
        <p className="mt-4 text-sm font-medium text-slate-700">
          {isBusy ? 'Enviando para a API...' : 'Arraste o arquivo .json aqui ou clique para selecionar'}
        </p>
        <p className="mt-1 text-xs text-slate-400">Somente arquivos .json</p>
        <input
          ref={inputRef}
          type="file"
          accept=".json,application/json"
          className="hidden"
          disabled={isBusy}
          onChange={(event) => {
            const file = event.target.files?.[0]
            if (file) void handleFile(file)
            event.target.value = ''
          }}
        />
      </div>

      {state.status === 'error' && (
        <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="text-sm font-medium text-red-800">{state.message}</p>
        </div>
      )}
    </div>
  )
}
