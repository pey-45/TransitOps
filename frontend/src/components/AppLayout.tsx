import { Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'

export function AppLayout() {
  const { session, logout } = useAuth()
  return (
    <div className="app-shell">
      <header>
        <a className="brand" href="/">TransitOps</a>
        <nav aria-label="Navegación principal">
          <a href="/">Inicio</a>
          {session?.user.role === 'admin' && <span className="future-nav">Usuarios (próximamente)</span>}
        </nav>
        <div className="account">
          <span>{session?.user.username} · {session?.user.role === 'admin' ? 'Administrador' : 'Operador'}</span>
          <button className="secondary" type="button" onClick={logout}>Cerrar sesión</button>
        </div>
      </header>
      <main><Outlet /></main>
    </div>
  )
}
