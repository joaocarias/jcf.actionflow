import { NavLink } from 'react-router-dom'

const navItems = [
  { to: '/', label: 'Novo fluxo', end: true },
  { to: '/sobre', label: 'Sobre', end: false },
]

export function Navbar() {
  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-6">
        <span className="text-lg font-semibold tracking-tight text-slate-900">
          Action<span className="text-indigo-600">Flow</span>
        </span>
        <nav className="flex gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-indigo-50 text-indigo-700'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </div>
    </header>
  )
}
