import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Navbar } from './components/Navbar'
import { ActionDetail } from './pages/ActionDetail'
import { GraphView } from './pages/Graph'
import { NovoFluxo } from './pages/NovoFluxo'
import { Sobre } from './pages/Sobre'
import { Workspace } from './pages/Workspace'

function App() {
  return (
    <BrowserRouter>
      <div className="flex min-h-screen flex-col bg-slate-50">
        <Navbar />
        <main className="flex-1">
          <Routes>
            <Route path="/" element={<NovoFluxo />} />
            <Route path="/workspaces/:sessionId" element={<Workspace />} />
            <Route path="/workspaces/:sessionId/graph" element={<GraphView />} />
            <Route path="/workspaces/:sessionId/actions/:actionId" element={<ActionDetail />} />
            <Route path="/sobre" element={<Sobre />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}

export default App
