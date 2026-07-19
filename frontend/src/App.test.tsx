import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from './App'
import { AuthProvider } from './auth/AuthContext'

const session = {
  accessToken: 'test-token',
  tokenType: 'Bearer',
  expiresAt: '2099-01-01T00:00:00Z',
  user: { id: 'user-1', username: 'admin', email: 'admin@test.dev', role: 'admin', isActive: true },
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

describe('authenticated skeleton', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })

  it('redirects an unauthenticated protected route to login', () => {
    renderAt('/')
    expect(screen.getByRole('heading', { name: 'TransitOps' })).toBeInTheDocument()
  })

  it('logs in against the API and reaches the protected home', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ data: session, requestId: 'request-1' }),
    }))
    renderAt('/login')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Usuario'), 'admin')
    await user.type(screen.getByLabelText('Contraseña'), 'SecurePass!123')
    await user.click(screen.getByRole('button', { name: 'Iniciar sesión' }))

    expect(await screen.findByRole('heading', { name: 'Hola, admin' })).toBeInTheDocument()
    expect(localStorage.getItem('transitops.session')).toContain('test-token')
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
    localStorage.setItem('transitops.session', JSON.stringify(session))
    renderAt('/')
    expect(screen.getByText('Usuarios (próximamente)')).toBeInTheDocument()
    expect(screen.getByText('Administrador')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Vehículos' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Conductores' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Clientes' })).toBeInTheDocument()
  })

  it('loads an authenticated vehicle list and sends the bearer token', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session))
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
    expect(headers.get('Authorization')).toBe('Bearer test-token')
  })

  it('creates a vehicle and redirects to its detail', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session))
    const vehicle = { id: 'vehicle-1', licensePlate: '1234 ABC', internalCode: null, brand: null, model: null, loadCapacity: null, isActive: true, createdAt: '2026-07-19', updatedAt: '2026-07-19' }
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce({ ok: true, status: 201, json: async () => ({ data: vehicle, requestId: 'create-1' }) })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ data: vehicle, requestId: 'get-1' }) }))
    renderAt('/vehiculos/nuevo')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Matrícula'), '1234 ABC')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('heading', { name: '1234 ABC' })).toBeInTheDocument()
  })

  it('shows a business conflict while creating a vehicle', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      json: async () => ({ error: { code: 'vehicle_plate_conflict', message: 'Ya existe un vehículo activo con esa matrícula.' }, requestId: 'conflict-1' }),
    }))
    renderAt('/vehiculos/nuevo')
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Matrícula'), '1234 ABC')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe un vehículo activo con esa matrícula.')
  })
})
