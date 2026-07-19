import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'

export function AppLayout() {
  const { session, logout } = useAuth()
  return (
    <div className="app-shell">
      <header>
        <Link className="brand" to="/">TransitOps</Link>
        <nav aria-label="Navegación principal">
          <NavLink to="/" end>Inicio</NavLink>
          <NavLink to="/vehiculos">Vehículos</NavLink>
          <NavLink to="/conductores">Conductores</NavLink>
          <NavLink to="/clientes">Clientes</NavLink>
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
