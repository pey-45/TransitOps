import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderAt, response, session, setSession, setupHarness, summary } from '../test/harness'

setupHarness()

describe('administración de usuarios', () => {
  it('hides administration from operators and redirects direct navigation', async () => {
    const operatorSession = { ...session, user: { ...session.user, username: 'operator', role: 'operator' as const } }
    setSession(operatorSession)
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() =>
      response({ data: summary, requestId: 'summary-1' })))

    renderAt('/usuarios')

    expect(await screen.findByRole('heading', { name: 'Hola, operator' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Usuarios' })).toBeNull()
    expect(screen.queryByRole('heading', { name: 'Usuarios' })).toBeNull()
  })

  it('shows administration to admins and lists inactive users on request', async () => {
    setSession(session)
    const users = [
      session.user,
      { ...session.user, id: 'user-2', username: 'inactive', role: 'operator' as const, isActive: false },
    ]
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      return response({ data: url.includes('includeInactive=true') ? users : [session.user], requestId: 'users-1' })
    })
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/usuarios')
    const user = userEvent.setup()

    expect(await screen.findByRole('heading', { name: 'Usuarios' })).toBeInTheDocument()
    expect(screen.queryByText('inactive')).toBeNull()
    await user.click(screen.getByLabelText('Mostrar también los desactivados'))
    expect(await screen.findByText('inactive')).toBeInTheDocument()
    expect(fetchMock.mock.calls.at(-1)?.[0]).toContain('includeInactive=true')
  })

  it('sends a user creation contract and shows backend conflicts', async () => {
    setSession(session)
    const fetchMock = vi.fn().mockImplementation((_input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'POST') return response({
        error: { code: 'user_credentials_conflict', message: 'El nombre de usuario o correo ya está en uso.' },
        requestId: 'conflict-1',
      }, false, 409)
      throw new Error('Unexpected request')
    })
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/usuarios/nuevo')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Usuario'), 'operator')
    await user.type(screen.getByLabelText('Correo'), 'operator@test.dev')
    await user.type(screen.getByLabelText('Contraseña inicial'), 'OperatorPass!123')
    await user.click(screen.getByRole('button', { name: 'Crear usuario' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('El nombre de usuario o correo ya está en uso.')
    const body = JSON.parse(String(fetchMock.mock.calls[0][1]?.body))
    expect(body).toEqual({
      username: 'operator',
      email: 'operator@test.dev',
      password: 'OperatorPass!123',
      role: 'operator',
    })
  })

  it('resets another user password and offers self-service on the own row', async () => {
    setSession(session)
    const target = { ...session.user, id: 'user-2', username: 'operator', role: 'operator' as const }
    const fetchMock = vi.fn().mockImplementation((_input: RequestInfo | URL, init?: RequestInit) =>
      init?.method === 'PUT'
        ? response({ data: target, requestId: 'reset-1' })
        : response({ data: [session.user, target], requestId: 'users-1' }))
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/usuarios')
    const user = userEvent.setup()

    await screen.findByText('operator')
    expect(screen.getByRole('link', { name: 'Cambiar mi contraseña' })).toHaveAttribute('href', '/cambiar-contrasena')
    expect(screen.getAllByRole('button', { name: 'Restablecer contraseña' })).toHaveLength(1)

    await user.click(screen.getByRole('button', { name: 'Restablecer contraseña' }))
    expect(await screen.findByRole('heading', { name: 'Restablecer la contraseña de operator' })).toBeInTheDocument()
    await user.type(screen.getByLabelText('Contraseña nueva'), 'ResetPass!2026')
    await user.type(screen.getByLabelText('Repetir contraseña nueva'), 'OtherPass!2026')
    await user.click(screen.getByRole('button', { name: 'Guardar contraseña' }))

    expect(screen.getByText('Las contraseñas no coinciden.')).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(call => call[1]?.method === 'PUT')).toBe(false)

    await user.clear(screen.getByLabelText('Repetir contraseña nueva'))
    await user.type(screen.getByLabelText('Repetir contraseña nueva'), 'ResetPass!2026')
    await user.click(screen.getByRole('button', { name: 'Guardar contraseña' }))

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Se ha asignado una contraseña nueva a operator y se han cerrado sus sesiones abiertas.')
    const call = fetchMock.mock.calls.find(item => item[1]?.method === 'PUT')!
    expect(String(call[0])).toContain('/api/v1/users/user-2/password')
    expect(JSON.parse(String(call[1]?.body))).toEqual({ password: 'ResetPass!2026' })
  })

  it('checks repeated password locally and submits a valid password change', async () => {
    setSession(session)
    const fetchMock = vi.fn().mockImplementation(() =>
      response({ data: { changed: true }, requestId: 'password-1' }))
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/cambiar-contrasena')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Contraseña actual'), 'SecurePass!123')
    await user.type(screen.getByLabelText('Nueva contraseña'), 'NewSecurePass!456')
    await user.type(screen.getByLabelText('Repetir nueva contraseña'), 'DifferentPass!789')
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))

    expect(screen.getByText('Las contraseñas nuevas no coinciden.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()

    await user.clear(screen.getByLabelText('Repetir nueva contraseña'))
    await user.type(screen.getByLabelText('Repetir nueva contraseña'), 'NewSecurePass!456')
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }))
    expect(await screen.findByRole('status')).toHaveTextContent('Contraseña cambiada correctamente.')
    expect(JSON.parse(String(fetchMock.mock.calls[0][1]?.body))).toEqual({
      currentPassword: 'SecurePass!123',
      newPassword: 'NewSecurePass!456',
    })
  })
})
