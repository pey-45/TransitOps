import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ApiClientError, changeUserActivation, changeUserRole, createUser, listUsers, resetUserPassword,
  type Role, type User, type ValidationDetails,
} from '../../api/client'
import { BackLink, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { ErrorAlert } from '../../components/ErrorAlert'
import { fieldErrors } from '../../components/form-errors'
import { useAuth } from '../../auth/auth-state'

function message(reason: unknown) {
  return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'
}

export function UserListPage() {
  const { session } = useAuth()
  const [users, setUsers] = useState<User[]>([])
  const [includeInactive, setIncludeInactive] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [resetTarget, setResetTarget] = useState<User | null>(null)
  const [resetPassword, setResetPassword] = useState('')
  const [resetRepeat, setResetRepeat] = useState('')
  const [resetRepeatError, setResetRepeatError] = useState('')
  const [resetDetails, setResetDetails] = useState<ValidationDetails>()
  const [resetPending, setResetPending] = useState(false)
  const [resetSuccess, setResetSuccess] = useState('')

  useEffect(() => {
    let ignore = false
    setLoading(true)
    setError('')
    listUsers(includeInactive).then(value => { if (!ignore) setUsers(value) })
      .catch(reason => { if (!ignore) setError(message(reason)) })
      .finally(() => { if (!ignore) setLoading(false) })
    return () => { ignore = true }
  }, [includeInactive])

  async function role(user: User, nextRole: Role) {
    if (nextRole === user.role || !window.confirm(`¿Cambiar el rol de ${user.username}?`)) return
    setError('')
    try {
      const updated = await changeUserRole(user.id, nextRole)
      setUsers(current => current.map(item => item.id === updated.id ? updated : item))
    } catch (reason) {
      setError(message(reason))
    }
  }

  async function activation(user: User) {
    const action = user.isActive ? 'desactivar' : 'reactivar'
    if (!window.confirm(`¿${action[0].toUpperCase()}${action.slice(1)} a ${user.username}?`)) return
    setError('')
    try {
      const updated = await changeUserActivation(user.id, !user.isActive)
      setUsers(current => includeInactive
        ? current.map(item => item.id === updated.id ? updated : item)
        : current.filter(item => item.id !== updated.id))
    } catch (reason) {
      setError(message(reason))
    }
  }

  function openReset(user: User) {
    setResetTarget(user)
    setResetPassword('')
    setResetRepeat('')
    setResetRepeatError('')
    setResetDetails(undefined)
    setResetSuccess('')
    setError('')
  }

  async function submitReset(event: FormEvent) {
    event.preventDefault()
    if (!resetTarget) return
    setResetRepeatError('')
    setResetDetails(undefined)
    setError('')
    if (resetPassword !== resetRepeat) {
      setResetRepeatError('Las contraseñas no coinciden.')
      return
    }
    setResetPending(true)
    try {
      await resetUserPassword(resetTarget.id, resetPassword)
      setResetSuccess(`Se ha asignado una contraseña nueva a ${resetTarget.username} y se han cerrado sus sesiones abiertas.`)
      setResetTarget(null)
      setResetPassword('')
      setResetRepeat('')
    } catch (reason) {
      setError(message(reason))
      if (reason instanceof ApiClientError) setResetDetails(reason.details)
    } finally {
      setResetPending(false)
    }
  }

  return <section className="content-page">
    <PageHeader eyebrow="Administración" title="Usuarios" action={<Link className="button-link" to="/usuarios/nuevo">Nuevo usuario</Link>} />
    <label className="checkbox-field"><input type="checkbox" checked={includeInactive} onChange={event => setIncludeInactive(event.target.checked)} /> Mostrar también los desactivados</label>
    <ErrorAlert message={error} />
    {resetSuccess && <div className="success-notice" role="status">{resetSuccess}</div>}
    {resetTarget && <form className="operation-panel" onSubmit={submitReset}>
      <div><p className="eyebrow">Cuenta</p><h2>Restablecer la contraseña de {resetTarget.username}</h2><p className="hint">Comunica la contraseña nueva a la persona por un canal aparte. Al guardarla se cerrarán sus sesiones abiertas y deberá cambiarla al entrar.</p></div>
      <div className="form-grid">
        <FormField id="resetPassword" label="Contraseña nueva" error={fieldErrors(resetDetails, 'Password')}><input id="resetPassword" required type="password" minLength={10} maxLength={128} value={resetPassword} onChange={event => setResetPassword(event.target.value)} /></FormField>
        <FormField id="resetRepeat" label="Repetir contraseña nueva" error={resetRepeatError ? [resetRepeatError] : undefined}><input id="resetRepeat" required type="password" minLength={10} maxLength={128} value={resetRepeat} onChange={event => setResetRepeat(event.target.value)} /></FormField>
      </div>
      <div className="operation-actions"><button type="submit" disabled={resetPending}>{resetPending ? 'Guardando…' : 'Guardar contraseña'}</button><button className="secondary" type="button" disabled={resetPending} onClick={() => setResetTarget(null)}>Cancelar</button></div>
    </form>}
    {loading ? <Loading /> : users.length === 0 ? <Empty>No hay usuarios que mostrar.</Empty> :
      <div className="table-wrap"><table><thead><tr><th>Usuario</th><th>Correo</th><th>Rol</th><th>Estado</th><th>Acciones</th></tr></thead>
        <tbody>{users.map(user => <tr className={user.isActive ? '' : 'inactive-row'} key={user.id}>
          <td>{user.username}</td><td>{user.email}</td><td><select aria-label={`Rol de ${user.username}`} value={user.role} onChange={event => void role(user, event.target.value as Role)}><option value="admin">Administrador</option><option value="operator">Operador</option></select></td>
          <td><span className={`role-badge ${user.isActive ? '' : 'role-inactive'}`}>{user.isActive ? 'Activo' : 'Desactivado'}</span></td>
          <td className="actions">
            {user.id === session?.user.id
              ? <Link to="/cambiar-contrasena">Cambiar mi contraseña</Link>
              : <button className="action-link" type="button" onClick={() => openReset(user)}>Restablecer contraseña</button>}
            <button className={user.isActive ? 'danger-link' : 'action-link'} type="button" onClick={() => void activation(user)}>{user.isActive ? 'Desactivar' : 'Reactivar'}</button>
          </td>
        </tr>)}</tbody></table></div>}
  </section>
}

export function UserFormPage() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<Role>('operator')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const [details, setDetails] = useState<ValidationDetails>()

  async function submit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError('')
    setDetails(undefined)
    try {
      await createUser({ username, email, password, role })
      navigate('/usuarios')
    } catch (reason) {
      setError(message(reason))
      if (reason instanceof ApiClientError) setDetails(reason.details)
    } finally {
      setPending(false)
    }
  }

  return <section className="content-page narrow"><BackLink to="/usuarios" />
    <PageHeader eyebrow="Administración" title="Nuevo usuario" />
    <form className="catalog-form" onSubmit={submit}>
      <FormField id="username" label="Usuario" error={fieldErrors(details, 'Username')}><input id="username" required minLength={3} maxLength={80} value={username} onChange={event => setUsername(event.target.value)} /></FormField>
      <FormField id="email" label="Correo" error={fieldErrors(details, 'Email')}><input id="email" required type="email" maxLength={254} value={email} onChange={event => setEmail(event.target.value)} /></FormField>
      <FormField id="password" label="Contraseña inicial" error={fieldErrors(details, 'Password')}><input id="password" required type="password" minLength={10} maxLength={128} value={password} onChange={event => setPassword(event.target.value)} /></FormField>
      <FormField id="role" label="Rol" error={fieldErrors(details, 'Role')}><select id="role" value={role} onChange={event => setRole(event.target.value as Role)}><option value="operator">Operador</option><option value="admin">Administrador</option></select></FormField>
      <ErrorAlert message={error} /><div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Guardando…' : 'Crear usuario'}</button><Link className="secondary-link" to="/usuarios">Cancelar</Link></div>
    </form>
  </section>
}
