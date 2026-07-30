import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'

export function AdminRoute() {
  const { session } = useAuth()
  return session?.user.role === 'admin' ? <Outlet /> : <Navigate to="/" replace />
}
