import { useAuth } from '../auth/auth-state'

export function HomePage() {
  const { session } = useAuth()
  return (
    <section className="welcome">
      <p className="eyebrow">Zona autenticada</p>
      <h1>Hola, {session?.user.username}</h1>
      <p>El esqueleto de TransitOps está listo. Los módulos operativos se incorporarán en los siguientes sprints.</p>
      <div className="role-card"><strong>Rol activo</strong><span>{session?.user.role === 'admin' ? 'Administrador' : 'Operador'}</span></div>
    </section>
  )
}
