import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  ApiClientError, changeUserActivation, changeUserRole, createUser, listUsers,
  type Role, type User, type ValidationDetails,
} from '../../api/client'
import { BackLink, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { ErrorAlert } from '../../components/ErrorAlert'
import { fieldErrors } from '../../components/form-errors'

function message(reason: unknown) {
  return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'
}

export function UserListPage() {
  const [users, setUsers] = useState<User[]>([])
  const [includeInactive, setIncludeInactive] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

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

  return <section className="content-page">
    <PageHeader eyebrow="Administración" title="Usuarios" action={<Link className="button-link" to="/usuarios/nuevo">Nuevo usuario</Link>} />
    <label className="checkbox-field"><input type="checkbox" checked={includeInactive} onChange={event => setIncludeInactive(event.target.checked)} /> Mostrar también los desactivados</label>
    <ErrorAlert message={error} />
    {loading ? <Loading /> : users.length === 0 ? <Empty>No hay usuarios que mostrar.</Empty> :
      <div className="table-wrap"><table><thead><tr><th>Usuario</th><th>Correo</th><th>Rol</th><th>Estado</th><th>Acciones</th></tr></thead>
        <tbody>{users.map(user => <tr className={user.isActive ? '' : 'inactive-row'} key={user.id}>
          <td>{user.username}</td><td>{user.email}</td><td><select aria-label={`Rol de ${user.username}`} value={user.role} onChange={event => void role(user, event.target.value as Role)}><option value="admin">Administrador</option><option value="operator">Operador</option></select></td>
          <td><span className={`role-badge ${user.isActive ? '' : 'role-inactive'}`}>{user.isActive ? 'Activo' : 'Desactivado'}</span></td>
          <td><button className={user.isActive ? 'danger-link' : 'action-link'} type="button" onClick={() => void activation(user)}>{user.isActive ? 'Desactivar' : 'Reactivar'}</button></td>
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
