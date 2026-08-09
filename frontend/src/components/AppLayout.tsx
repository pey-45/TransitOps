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
          <NavLink to="/envios">Envíos</NavLink>
          <NavLink to="/vehiculos">Vehículos</NavLink>
          <NavLink to="/conductores">Conductores</NavLink>
          <NavLink to="/clientes">Clientes</NavLink>
          {session?.user.role === 'admin' && <NavLink to="/usuarios">Usuarios</NavLink>}
        </nav>
        <div className="account">
          <span>{session?.user.username} · {session?.user.role === 'admin' ? 'Administrador' : 'Operador'}</span>
          <Link className="account-link" to="/cambiar-contrasena">Cambiar contraseña</Link>
          <button className="secondary" type="button" onClick={() => void logout()}>Cerrar sesión</button>
        </div>
      </header>
      <main><Outlet /></main>
    </div>
  )
}
