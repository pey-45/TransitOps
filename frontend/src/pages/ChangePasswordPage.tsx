import { useState, type FormEvent } from 'react'
import { ApiClientError, changePassword, type ValidationDetails } from '../api/client'
import { FormField, PageHeader } from '../components/CatalogUi'
import { ErrorAlert } from '../components/ErrorAlert'
import { fieldErrors } from '../components/form-errors'

function message(reason: unknown) {
  return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'
}

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [repeatPassword, setRepeatPassword] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const [currentError, setCurrentError] = useState('')
  const [repeatError, setRepeatError] = useState('')
  const [details, setDetails] = useState<ValidationDetails>()
  const [success, setSuccess] = useState('')

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setCurrentError('')
    setRepeatError('')
    setDetails(undefined)
    setSuccess('')
    if (newPassword !== repeatPassword) {
      setRepeatError('Las contraseñas nuevas no coinciden.')
      return
    }
    setPending(true)
    try {
      await changePassword(currentPassword, newPassword)
      setCurrentPassword('')
      setNewPassword('')
      setRepeatPassword('')
      setSuccess('Contraseña cambiada correctamente.')
    } catch (reason) {
      setError(message(reason))
      if (reason instanceof ApiClientError) {
        setDetails(reason.details)
        if (reason.code === 'invalid_credentials') setCurrentError('La contraseña actual no es correcta.')
      }
    } finally {
      setPending(false)
    }
  }

  return <section className="content-page narrow"><PageHeader eyebrow="Cuenta" title="Cambiar contraseña" />
    <form className="catalog-form" onSubmit={submit}>
      <FormField id="currentPassword" label="Contraseña actual" error={[...(fieldErrors(details, 'CurrentPassword') ?? []), ...(currentError ? [currentError] : [])]}><input id="currentPassword" required type="password" maxLength={128} value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} /></FormField>
      <FormField id="newPassword" label="Nueva contraseña" error={fieldErrors(details, 'NewPassword')}><input id="newPassword" required type="password" minLength={10} maxLength={128} value={newPassword} onChange={event => setNewPassword(event.target.value)} /></FormField>
      <FormField id="repeatPassword" label="Repetir nueva contraseña" error={repeatError ? [repeatError] : undefined}><input id="repeatPassword" required type="password" minLength={10} maxLength={128} value={repeatPassword} onChange={event => setRepeatPassword(event.target.value)} /></FormField>
      <ErrorAlert message={error} />{success && <div className="success-notice" role="status">{success}</div>}
      <div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Cambiando…' : 'Cambiar contraseña'}</button></div>
    </form>
  </section>
}
