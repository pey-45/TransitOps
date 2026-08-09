import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from './App'
import type { Session, Shipment, ShipmentEvent, SummaryResponse } from './api/client'
import { AuthProvider } from './auth/AuthContext'

const session: Session = {
  expiresAt: '2099-01-01T00:00:00Z',
  user: { id: 'user-1', username: 'admin', email: 'admin@test.dev', role: 'admin', isActive: true },
}
let currentSession: Session | null | undefined = null
const summary: SummaryResponse = {
  shipments: { planned: 2, inProgress: 1, delivered: 4, cancelled: 1, total: 8 },
  vehicles: [{ id: 'vehicle-1', label: '1234 ABC', shipmentCount: 3 }],
  drivers: [{ id: 'driver-1', label: 'Ana', shipmentCount: 3 }],
  incidents: 2,
  from: '2026-07-01T00:00:00Z',
  to: '2026-07-31T23:59:59Z',
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider initialSession={currentSession}><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

beforeEach(() => { currentSession = null })
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
    currentSession = undefined
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
    currentSession = session
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
    currentSession = session
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
    currentSession = session
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

  it('creates a vehicle and redirects to its detail', async () => {
    currentSession = session
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
    currentSession = session
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

describe('administración e indicadores', () => {
  function response(data: unknown, ok = true, status = 200) {
    return Promise.resolve({ ok, status, json: async () => data })
  }

  it('shows the operational summary and links status counters to filtered shipments', async () => {
    currentSession = session
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() =>
      response({ data: summary, requestId: 'summary-1' })))

    renderAt('/')

    expect(await screen.findByRole('heading', { name: 'Envíos por estado' })).toBeInTheDocument()
    expect(screen.getByText('Situación actual; estos contadores no dependen del periodo.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Actividad en el periodo' })).toBeInTheDocument()
    expect(screen.getByText('1234 ABC')).toBeInTheDocument()
    expect(screen.getByText('Ana')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /En curso/ })).toHaveAttribute('href', '/envios?status=in_progress')
  })

  it('applies an explicit summary period with local day limits', async () => {
    currentSession = session
    const fetchMock = vi.fn().mockImplementation(() =>
      response({ data: summary, requestId: 'summary-1' }))
    vi.stubGlobal('fetch', fetchMock)
    renderAt('/')
    const user = userEvent.setup()
    await screen.findByRole('heading', { name: 'Actividad en el periodo' })
    fireEvent.change(screen.getByLabelText('Desde'), { target: { value: '2026-07-10' } })
    fireEvent.change(screen.getByLabelText('Hasta'), { target: { value: '2026-07-20' } })
    await user.click(screen.getByRole('button', { name: 'Aplicar periodo' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
    const url = String(fetchMock.mock.calls[1][0])
    expect(url).toContain('from=')
    expect(url).toContain('to=')
  })

  it('hides administration from operators and redirects direct navigation', async () => {
    const operatorSession = { ...session, user: { ...session.user, username: 'operator', role: 'operator' as const } }
    currentSession = operatorSession
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() =>
      response({ data: summary, requestId: 'summary-1' })))

    renderAt('/usuarios')

    expect(await screen.findByRole('heading', { name: 'Hola, operator' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Usuarios' })).toBeNull()
    expect(screen.queryByRole('heading', { name: 'Usuarios' })).toBeNull()
  })

  it('shows administration to admins and lists inactive users on request', async () => {
    currentSession = session
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
    currentSession = session
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

  it('checks repeated password locally and submits a valid password change', async () => {
    currentSession = session
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

describe('envíos', () => {
  const shipment: Shipment = {
    id: 'shipment-1', reference: 'ENV-001', origin: 'Madrid', destination: 'Barcelona',
    plannedPickupAt: '2026-08-01T08:00:00Z', plannedDeliveryAt: null, customerId: null,
    customerName: null, estimatedLoad: null, notes: null, status: 'planned', vehicleId: null,
    driverId: null, vehiclePlate: null, driverName: null, actualPickupAt: null, actualDeliveryAt: null,
    capacityWarning: null, createdAt: '2026-07-26T00:00:00Z', updatedAt: '2026-07-26T00:00:00Z',
  }
  const vehicles = [{ id: 'vehicle-1', licensePlate: '1234 ABC', internalCode: null, brand: null, model: null, loadCapacity: 10000, isActive: true, createdAt: '2026-07-26', updatedAt: '2026-07-26' }]
  const drivers = [{ id: 'driver-1', name: 'Ana', licenseNumber: 'L-1', employeeCode: null, contactDetails: null, isActive: true, createdAt: '2026-07-26', updatedAt: '2026-07-26' }]
  const createdEvent: ShipmentEvent = { id: 'event-created', shipmentId: 'shipment-1', eventType: 'created', occurredAt: '2026-07-26T00:00:00Z', location: null, notes: null, recordedByUserId: null, recordedByUsername: null, createdAt: '2026-07-26T00:00:00Z' }

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
  function operationMock(current = shipment, saved = current, history: ShipmentEvent[] = [], manualCreated?: ShipmentEvent) {
    return vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input)
      if (url.endsWith('/api/v1/shipments/shipment-1/events') && init?.method === 'POST') return response({ data: manualCreated ?? createdEvent, requestId: 'event-created' }, true, 201)
      if (url.endsWith('/api/v1/shipments/shipment-1/events') && !init?.method) return response({ data: history, requestId: 'events' })
      if (url.endsWith('/api/v1/shipments/shipment-1/assignment')) return response({ data: saved, requestId: 'assignment' })
      if (url.endsWith('/api/v1/shipments/shipment-1/status')) return response({ data: saved, requestId: 'status' })
      if (url.endsWith('/api/v1/shipments/shipment-1') && !init?.method) return response({ data: current, requestId: 'detail' })
      if (url.endsWith('/api/v1/vehicles')) return response({ data: vehicles, requestId: 'vehicles' })
      if (url.endsWith('/api/v1/drivers')) return response({ data: drivers, requestId: 'drivers' })
      throw new Error(`Unexpected URL ${url}`)
    })
  }

  it('lists paginated shipments and moves to the next page', async () => {
    currentSession = session; const fetchMock = listMock(); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    expect(await screen.findByRole('link', { name: 'ENV-001' })).toBeInTheDocument()
    await userEvent.setup().click(screen.getByRole('button', { name: 'Siguiente' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('page=2'))
  })

  it('writes the status filter to the query and does not reload catalogs', async () => {
    currentSession = session; const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    await screen.findByRole('link', { name: 'ENV-001' }); const user = userEvent.setup(); await user.selectOptions(screen.getByLabelText('Estado'), 'planned'); await user.click(screen.getByRole('button', { name: 'Filtrar' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('status=planned'))
    expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/vehicles'))).toHaveLength(1); expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/drivers'))).toHaveLength(1)
  })

  it('recovers the filtered view from the URL', async () => {
    currentSession = session; const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios?status=delivered&pickupFrom=2026-08-01')
    expect(await screen.findByLabelText('Estado')).toHaveValue('delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('status=delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('pickupFrom=')
  })

  it('creates a shipment with ISO pickup and omits status and empty customer', async () => {
    currentSession = session; const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input); if (url.endsWith('/customers')) return response({ data: [], requestId: 'customers' })
      if (url.endsWith('/shipments') && init?.method === 'POST') return response({ data: shipment, requestId: 'created' }, true, 201)
      if (url.endsWith('/shipments/shipment-1/events')) return response({ data: [], requestId: 'events' })
      if (url.endsWith('/shipments/shipment-1')) return response({ data: shipment, requestId: 'detail' })
      if (url.endsWith('/vehicles')) return response({ data: [], requestId: 'vehicles' })
      if (url.endsWith('/drivers')) return response({ data: [], requestId: 'drivers' })
      throw new Error(`Unexpected URL ${url}`)
    }); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/nuevo'); const user = userEvent.setup()
    await screen.findByLabelText('Referencia'); await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    await screen.findByRole('heading', { name: 'ENV-001' }); const post = fetchMock.mock.calls.find(call => call[1]?.method === 'POST'); const body = JSON.parse(String(post?.[1]?.body))
    expect(body.plannedPickupAt).toBe(new Date('2026-08-01T10:00').toISOString()); expect(body).not.toHaveProperty('status'); expect(body).not.toHaveProperty('customerId')
  })

  it('shows a reference conflict globally and under the field', async () => {
    currentSession = session; vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/customers')) return response({ data: [], requestId: 'customers' })
      if (init?.method === 'POST') return response({ error: { code: 'shipment_reference_conflict', message: 'Ya existe un envío con esa referencia.' } }, false, 409)
      throw new Error('Unexpected request')
    })); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe un envío con esa referencia.'); expect(screen.getByText('Usa una referencia diferente.')).toBeInTheDocument()
  })

  it('blocks an earlier delivery without calling the server', async () => {
    currentSession = session; const fetchMock = vi.fn((input: RequestInfo | URL) => String(input).endsWith('/customers') ? response({ data: [], requestId: 'customers' }) : Promise.reject(new Error('Unexpected request'))); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-02T10:00'); await user.type(screen.getByLabelText('Entrega prevista (opcional)'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(screen.getByText('La entrega no puede ser anterior a la recogida.')).toBeInTheDocument(); expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('rejects an invalid pickup without leaving the form pending', async () => {
    currentSession = session; const fetchMock = vi.fn((input: RequestInfo | URL) => String(input).endsWith('/customers') ? response({ data: [], requestId: 'customers' }) : Promise.reject(new Error('Unexpected request'))); vi.stubGlobal('fetch', fetchMock); const rendered = renderAt('/envios/nuevo'); await screen.findByLabelText('Referencia')
    fireEvent.submit(rendered.container.querySelector('form')!); expect(await screen.findByText('Indica una fecha de recogida válida.')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Guardar' })).toBeEnabled(); expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('shows assignment controls and blocks starting an unassigned planned shipment', async () => {
    currentSession = session; vi.stubGlobal('fetch', operationMock()); renderAt('/envios/shipment-1')
    expect(await screen.findByLabelText('Vehículo')).toBeInTheDocument(); expect(screen.getByLabelText('Conductor')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Poner en curso' })).toBeDisabled(); expect(screen.getByText(/Asigna primero un vehículo y un conductor/)).toBeInTheDocument()
  })

  it('assigns both resources and repaints the detail without reloading the shipment', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana' }
    currentSession = session; const fetchMock = operationMock(shipment, assigned); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    expect(await screen.findByText('1234 ABC', { selector: 'dd' })).toBeInTheDocument(); expect(screen.getByText('Ana', { selector: 'dd' })).toBeInTheDocument()
    const assignment = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/assignment')); expect(assignment?.[1]?.method).toBe('PUT'); expect(JSON.parse(String(assignment?.[1]?.body))).toEqual({ vehicleId: 'vehicle-1', driverId: 'driver-1' })
    expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/shipments/shipment-1') && !call[1]?.method)).toHaveLength(1)
  })

  it('shows a successful capacity warning as a notice rather than an error', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', capacityWarning: 'La capacidad es inferior a la carga estimada.' }
    currentSession = session; vi.stubGlobal('fetch', operationMock(shipment, assigned)); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    const notice = await screen.findByRole('status'); expect(notice).toHaveTextContent('La capacidad es inferior'); expect(notice).toHaveClass('notice'); expect(screen.queryByRole('alert')).toBeNull()
  })

  it('hides every operation for a delivered shipment', async () => {
    const delivered: Shipment = { ...shipment, status: 'delivered', vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', actualPickupAt: '2026-08-01T08:00:00Z', actualDeliveryAt: '2026-08-01T12:00:00Z' }
    currentSession = session; vi.stubGlobal('fetch', operationMock(delivered)); renderAt('/envios/shipment-1')
    expect(await screen.findByText('El envío está en un estado final y ya no puede cambiar.')).toBeInTheDocument(); expect(screen.queryByRole('button', { name: 'Marcar entregado' })).toBeNull(); expect(screen.queryByRole('button', { name: 'Cancelar envío' })).toBeNull(); expect(screen.queryByLabelText('Vehículo')).toBeNull()
  })

  it('requires confirmation before delivering and sends the status only when accepted', async () => {
    const inProgress: Shipment = { ...shipment, status: 'in_progress', vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', actualPickupAt: '2026-08-01T08:00:00Z' }
    const delivered: Shipment = { ...inProgress, status: 'delivered', actualDeliveryAt: '2026-08-01T12:00:00Z' }; const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true)
    currentSession = session; const fetchMock = operationMock(inProgress, delivered); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup(); const button = await screen.findByRole('button', { name: 'Marcar entregado' })
    await user.click(button); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/status'))).toHaveLength(0)
    await user.click(button); await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/status'))).toHaveLength(1)); const statusCall = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/status')); expect(statusCall?.[1]?.method).toBe('POST'); expect(JSON.parse(String(statusCall?.[1]?.body))).toEqual({ status: 'delivered' }); expect(confirm).toHaveBeenCalledTimes(2)
  })

  it('renders the chronological event timeline with user and system actors', async () => {
    const incident: ShipmentEvent = { ...createdEvent, id: 'event-incident', eventType: 'incident', occurredAt: '2026-07-27T10:00:00Z', notes: 'Retraso por avería', recordedByUserId: 'user-1', recordedByUsername: 'ana', createdAt: '2026-07-27T10:05:00Z' }
    currentSession = session; vi.stubGlobal('fetch', operationMock(shipment, shipment, [createdEvent, incident])); renderAt('/envios/shipment-1')
    expect(await screen.findByRole('heading', { name: 'Historial de eventos' })).toBeInTheDocument(); expect(screen.getByText('Creación')).toBeInTheDocument(); expect(screen.getByText('Incidencia')).toBeInTheDocument()
    expect(screen.getByText(/Sistema · Automático/)).toBeInTheDocument(); expect(screen.getByText(/ana · Manual/)).toBeInTheDocument(); expect(screen.getByText('Retraso por avería')).toBeInTheDocument()
  })

  it('registers a manual event with ISO time and omitted empty fields without reloading history', async () => {
    const manual: ShipmentEvent = { ...createdEvent, id: 'event-manual', eventType: 'incident', occurredAt: new Date().toISOString(), recordedByUserId: 'user-1', recordedByUsername: 'admin', createdAt: new Date().toISOString() }
    currentSession = session; const fetchMock = operationMock(shipment, shipment, [createdEvent], manual); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); await user.selectOptions(screen.getByLabelText('Tipo de evento'), 'incident'); const localTime = (screen.getByLabelText('Fecha y hora') as HTMLInputElement).value; await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    expect(await screen.findByText('Incidencia')).toBeInTheDocument(); const post = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/events') && call[1]?.method === 'POST'); const body = JSON.parse(String(post?.[1]?.body))
    expect(body).toEqual({ eventType: 'incident', occurredAt: new Date(localTime).toISOString() }); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && !call[1]?.method)).toHaveLength(1)
  })

  it('inserts a backdated manual event in its chronological position', async () => {
    const earlier: ShipmentEvent = { ...createdEvent, id: 'event-earlier', eventType: 'checkpoint', occurredAt: '2026-07-25T10:00:00Z', location: 'León', recordedByUserId: 'user-1', recordedByUsername: 'admin', createdAt: '2026-07-30T10:00:00Z' }
    currentSession = session; vi.stubGlobal('fetch', operationMock(shipment, shipment, [createdEvent], earlier)); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); fireEvent.change(screen.getByLabelText('Fecha y hora'), { target: { value: '2026-07-25T10:00' } }); await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    await screen.findByText('León'); const entries = screen.getAllByRole('listitem'); expect(entries[0]).toHaveTextContent('Punto de control'); expect(entries[1]).toHaveTextContent('Creación')
  })

  it('blocks a future event locally without calling the API', async () => {
    currentSession = session; const fetchMock = operationMock(); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); const future = new Date(Date.now() + 24 * 60 * 60_000); const localFuture = new Date(future.getTime() - future.getTimezoneOffset() * 60_000).toISOString().slice(0, 16); fireEvent.change(screen.getByLabelText('Fecha y hora'), { target: { value: localFuture } }); await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    expect(screen.getByText('La fecha del evento no puede estar en el futuro.')).toBeInTheDocument(); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && call[1]?.method === 'POST')).toHaveLength(0)
  })

  it('refreshes automatic events after an operation action', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana' }
    currentSession = session; const fetchMock = operationMock(shipment, assigned, [createdEvent]); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && !call[1]?.method)).toHaveLength(2))
  })
})
