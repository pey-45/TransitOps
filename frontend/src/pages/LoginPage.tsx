import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiClientError } from '../api/client'
import { useAuth } from '../auth/auth-state'
import { ErrorAlert } from '../components/ErrorAlert'

export function LoginPage() {
  const { session, loading, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  if (loading) return <main className="login-page"><p role="status">Comprobando sesión…</p></main>
  if (session) return <Navigate to="/" replace />

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setPending(true)
    try {
      await login(username, password)
      const destination = (location.state as { from?: string } | null)?.from ?? '/'
      navigate(destination, { replace: true })
    } catch (reason) {
      setError(reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.')
    } finally {
      setPending(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <p className="eyebrow">Gestión de transportes</p>
        <h1>TransitOps</h1>
        <p className="intro">Accede a la zona operativa con tus credenciales.</p>
        <form onSubmit={submit}>
          <label htmlFor="username">Usuario</label>
          <input id="username" autoComplete="username" required value={username} onChange={event => setUsername(event.target.value)} />
          <label htmlFor="password">Contraseña</label>
          <input id="password" type="password" autoComplete="current-password" required value={password} onChange={event => setPassword(event.target.value)} />
          <ErrorAlert message={error} />
          <button type="submit" disabled={pending}>{pending ? 'Accediendo…' : 'Iniciar sesión'}</button>
        </form>
      </section>
    </main>
  )
}
