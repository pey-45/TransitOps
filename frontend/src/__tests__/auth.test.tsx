import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderAt, session, setSession, setupHarness, summary } from '../test/harness'

setupHarness()

describe('sesión autenticada', () => {
  it('redirects an unauthenticated protected route to login', () => {
    renderAt('/')
    expect(screen.getByRole('heading', { name: 'TransitOps' })).toBeInTheDocument()
  })

  it('logs in against the API and reaches the protected home', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ data: session, requestId: 'request-1' }) })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ data: summary, requestId: 'summary-1' }) }))
    renderAt('/login')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Usuario'), 'admin')
    await user.type(screen.getByLabelText('Contraseña'), 'SecurePass!123')
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }))

    expect(await screen.findByRole('heading', { name: 'Hola, admin' })).toBeInTheDocument()
    expect(localStorage.getItem('transitops.session')).toBeNull()
  })

  it('rehydrates an existing cookie session through auth me', async () => {
    setSession(undefined)
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) =>
      String(input).endsWith('/api/v1/auth/me')
        ? Promise.resolve({ ok: true, status: 200, json: async () => ({ data: session, requestId: 'me-1' }) })
        : Promise.resolve({ ok: true, status: 200, json: async () => ({ data: summary, requestId: 'summary-1' }) }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/')

    expect(await screen.findByRole('heading', { name: 'Hola, admin' })).toBeInTheDocument()
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/auth/me')
  })

  it('shows a clear API error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      json: async () => ({ error: { code: 'invalid_credentials', message: 'Credenciales incorrectas.' } }),
    }))
    renderAt('/login')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Usuario'), 'admin')
    await user.type(screen.getByLabelText('Contraseña'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Credenciales incorrectas.')
  })

  it('adapts navigation to the admin role', () => {
    setSession(session)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true, status: 200, json: async () => ({ data: summary, requestId: 'summary-1' }),
    }))
    renderAt('/')
    expect(screen.getByRole('link', { name: 'Usuarios' })).toBeInTheDocument()
    expect(screen.getByText((_, element) =>
      element?.tagName === 'SPAN' && element.textContent === 'admin · Administrador')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Vehículos' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Conductores' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Clientes' })).toBeInTheDocument()
  })

  it('loads an authenticated vehicle list through same-origin cookies', async () => {
    setSession(session)
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ data: [{
        id: 'vehicle-1', licensePlate: '1234 ABC', internalCode: 'V-1', brand: 'Volvo', model: 'FH',
        loadCapacity: 12000, isActive: true, createdAt: '2026-07-19', updatedAt: '2026-07-19',
      }], requestId: 'request-vehicles' }),
    })
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/vehiculos')

    expect(await screen.findByRole('link', { name: '1234 ABC' })).toBeInTheDocument()
    const headers = new Headers(fetchMock.mock.calls[0][1].headers)
    expect(headers.get('Authorization')).toBeNull()
    expect(fetchMock.mock.calls[0][1].credentials).toBe('same-origin')
  })

  it('logs out on the server and clears the in-memory session', async () => {
    setSession(session)
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) =>
      String(input).endsWith('/api/v1/auth/logout')
        ? Promise.resolve({ ok: true, status: 200, json: async () => ({ data: { loggedOut: true }, requestId: 'logout-1' }) })
        : Promise.resolve({ ok: true, status: 200, json: async () => ({ data: summary, requestId: 'summary-1' }) }))
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/')

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Cerrar sesión' }))

    expect(await screen.findByRole('heading', { name: 'TransitOps' })).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(call => String(call[0]).endsWith('/api/v1/auth/logout'))).toBe(true)
  })
})
