import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'

export function ProtectedRoute() {
  const { session, loading } = useAuth()
  const location = useLocation()
  if (loading) return <p role="status">Comprobando sesión…</p>
  return session ? <Outlet /> : <Navigate to="/login" replace state={{ from: location.pathname }} />
}
