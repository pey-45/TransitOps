import { cleanup, render, screen, waitFor } from '@testing-library/react'
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

beforeEach(() => localStorage.clear())
afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

describe('authenticated skeleton', () => {
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

describe('envíos', () => {
  const shipment = {
    id: 'shipment-1', reference: 'ENV-001', origin: 'Madrid', destination: 'Barcelona',
    plannedPickupAt: '2026-08-01T08:00:00Z', plannedDeliveryAt: null, customerId: null,
    customerName: null, estimatedLoad: null, notes: null, status: 'planned', vehicleId: null,
    driverId: null, createdAt: '2026-07-26T00:00:00Z', updatedAt: '2026-07-26T00:00:00Z',
  }

  function response(data: unknown, ok = true, status = 200) { return Promise.resolve({ ok, status, json: async () => data }) }
  function listMock(totalPages = 2) {
    return vi.fn((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/api/v1/vehicles')) return response({ data: [], requestId: 'vehicles' })
      if (url.includes('/api/v1/drivers')) return response({ data: [], requestId: 'drivers' })
      if (url.includes('/api/v1/shipments')) return response({ data: { items: [shipment], page: url.includes('page=2') ? 2 : 1, pageSize: 20, totalCount: 21, totalPages }, requestId: 'shipments' })
      throw new Error(`Unexpected URL ${url}`)
    })
  }

  it('lists paginated shipments and moves to the next page', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); const fetchMock = listMock(); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    expect(await screen.findByRole('link', { name: 'ENV-001' })).toBeInTheDocument()
    await userEvent.setup().click(screen.getByRole('button', { name: 'Siguiente' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('page=2'))
  })

  it('writes the status filter to the query and does not reload catalogs', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    await screen.findByRole('link', { name: 'ENV-001' }); const user = userEvent.setup(); await user.selectOptions(screen.getByLabelText('Estado'), 'planned'); await user.click(screen.getByRole('button', { name: 'Filtrar' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('status=planned'))
    expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/vehicles'))).toHaveLength(1); expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/drivers'))).toHaveLength(1)
  })

  it('recovers the filtered view from the URL', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios?status=delivered&pickupFrom=2026-08-01')
    expect(await screen.findByLabelText('Estado')).toHaveValue('delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('status=delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('pickupFrom=')
  })

  it('creates a shipment with ISO pickup and omits status and empty customer', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input); if (url.endsWith('/customers')) return response({ data: [], requestId: 'customers' })
      if (url.endsWith('/shipments') && init?.method === 'POST') return response({ data: shipment, requestId: 'created' }, true, 201)
      if (url.endsWith('/shipments/shipment-1')) return response({ data: shipment, requestId: 'detail' })
      throw new Error(`Unexpected URL ${url}`)
    }); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/nuevo'); const user = userEvent.setup()
    await screen.findByLabelText('Referencia'); await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    await screen.findByRole('heading', { name: 'ENV-001' }); const post = fetchMock.mock.calls.find(call => call[1]?.method === 'POST'); const body = JSON.parse(String(post?.[1]?.body))
    expect(body.plannedPickupAt).toBe(new Date('2026-08-01T10:00').toISOString()); expect(body).not.toHaveProperty('status'); expect(body).not.toHaveProperty('customerId')
  })

  it('shows a reference conflict globally and under the field', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/customers')) return response({ data: [], requestId: 'customers' })
      if (init?.method === 'POST') return response({ error: { code: 'shipment_reference_conflict', message: 'Ya existe un envío con esa referencia.' } }, false, 409)
      throw new Error('Unexpected request')
    })); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe un envío con esa referencia.'); expect(screen.getByText('Usa una referencia diferente.')).toBeInTheDocument()
  })

  it('blocks an earlier delivery without calling the server', async () => {
    localStorage.setItem('transitops.session', JSON.stringify(session)); const fetchMock = vi.fn((input: RequestInfo | URL) => String(input).endsWith('/customers') ? response({ data: [], requestId: 'customers' }) : Promise.reject(new Error('Unexpected request'))); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-02T10:00'); await user.type(screen.getByLabelText('Entrega prevista (opcional)'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(screen.getByText('La entrega no puede ser anterior a la recogida.')).toBeInTheDocument(); expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
