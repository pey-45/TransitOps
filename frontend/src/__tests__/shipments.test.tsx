import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Shipment, ShipmentEvent } from '../api/client'
import { renderAt, response, session, setSession, setupHarness } from '../test/harness'

setupHarness()

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
    setSession(session); const fetchMock = listMock(); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    expect(await screen.findByRole('link', { name: 'ENV-001' })).toBeInTheDocument()
    await userEvent.setup().click(screen.getByRole('button', { name: 'Siguiente' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('page=2'))
  })

  it('writes the status filter to the query and does not reload catalogs', async () => {
    setSession(session); const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios')
    await screen.findByRole('link', { name: 'ENV-001' }); const user = userEvent.setup(); await user.selectOptions(screen.getByLabelText('Estado'), 'planned'); await user.click(screen.getByRole('button', { name: 'Filtrar' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/shipments')).at(-1)?.[0]).toContain('status=planned'))
    expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/vehicles'))).toHaveLength(1); expect(fetchMock.mock.calls.filter(call => String(call[0]).includes('/drivers'))).toHaveLength(1)
  })

  it('recovers the filtered view from the URL', async () => {
    setSession(session); const fetchMock = listMock(1); vi.stubGlobal('fetch', fetchMock); renderAt('/envios?status=delivered&pickupFrom=2026-08-01')
    expect(await screen.findByLabelText('Estado')).toHaveValue('delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('status=delivered')
    expect(fetchMock.mock.calls.find(call => String(call[0]).includes('/shipments'))?.[0]).toContain('pickupFrom=')
  })

  it('creates a shipment with ISO pickup and omits status and empty customer', async () => {
    setSession(session); const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
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
    setSession(session); vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/customers')) return response({ data: [], requestId: 'customers' })
      if (init?.method === 'POST') return response({ error: { code: 'shipment_reference_conflict', message: 'Ya existe un envío con esa referencia.' } }, false, 409)
      throw new Error('Unexpected request')
    })); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe un envío con esa referencia.'); expect(screen.getByText('Usa una referencia diferente.')).toBeInTheDocument()
  })

  it('blocks an earlier delivery without calling the server', async () => {
    setSession(session); const fetchMock = vi.fn((input: RequestInfo | URL) => String(input).endsWith('/customers') ? response({ data: [], requestId: 'customers' }) : Promise.reject(new Error('Unexpected request'))); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/nuevo'); const user = userEvent.setup(); await screen.findByLabelText('Referencia')
    await user.type(screen.getByLabelText('Referencia'), 'ENV-001'); await user.type(screen.getByLabelText('Origen'), 'Madrid'); await user.type(screen.getByLabelText('Destino'), 'Barcelona'); await user.type(screen.getByLabelText('Recogida prevista'), '2026-08-02T10:00'); await user.type(screen.getByLabelText('Entrega prevista (opcional)'), '2026-08-01T10:00'); await user.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(screen.getByText('La entrega no puede ser anterior a la recogida.')).toBeInTheDocument(); expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('rejects an invalid pickup without leaving the form pending', async () => {
    setSession(session); const fetchMock = vi.fn((input: RequestInfo | URL) => String(input).endsWith('/customers') ? response({ data: [], requestId: 'customers' }) : Promise.reject(new Error('Unexpected request'))); vi.stubGlobal('fetch', fetchMock); const rendered = renderAt('/envios/nuevo'); await screen.findByLabelText('Referencia')
    fireEvent.submit(rendered.container.querySelector('form')!); expect(await screen.findByText('Indica una fecha de recogida válida.')).toBeInTheDocument(); expect(screen.getByRole('button', { name: 'Guardar' })).toBeEnabled(); expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('shows assignment controls and blocks starting an unassigned planned shipment', async () => {
    setSession(session); vi.stubGlobal('fetch', operationMock()); renderAt('/envios/shipment-1')
    expect(await screen.findByLabelText('Vehículo')).toBeInTheDocument(); expect(screen.getByLabelText('Conductor')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Poner en curso' })).toBeDisabled(); expect(screen.getByText(/Asigna primero un vehículo y un conductor/)).toBeInTheDocument()
  })

  it('assigns both resources and repaints the detail without reloading the shipment', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana' }
    setSession(session); const fetchMock = operationMock(shipment, assigned); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    expect(await screen.findByText('1234 ABC', { selector: 'dd' })).toBeInTheDocument(); expect(screen.getByText('Ana', { selector: 'dd' })).toBeInTheDocument()
    const assignment = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/assignment')); expect(assignment?.[1]?.method).toBe('PUT'); expect(JSON.parse(String(assignment?.[1]?.body))).toEqual({ vehicleId: 'vehicle-1', driverId: 'driver-1' })
    expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/shipments/shipment-1') && !call[1]?.method)).toHaveLength(1)
  })

  it('shows a successful capacity warning as a notice rather than an error', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', capacityWarning: 'La capacidad es inferior a la carga estimada.' }
    setSession(session); vi.stubGlobal('fetch', operationMock(shipment, assigned)); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    const notice = await screen.findByRole('status'); expect(notice).toHaveTextContent('La capacidad es inferior'); expect(notice).toHaveClass('notice'); expect(screen.queryByRole('alert')).toBeNull()
  })

  it('hides every operation for a delivered shipment', async () => {
    const delivered: Shipment = { ...shipment, status: 'delivered', vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', actualPickupAt: '2026-08-01T08:00:00Z', actualDeliveryAt: '2026-08-01T12:00:00Z' }
    setSession(session); vi.stubGlobal('fetch', operationMock(delivered)); renderAt('/envios/shipment-1')
    expect(await screen.findByText('El envío está en un estado final y ya no puede cambiar.')).toBeInTheDocument(); expect(screen.queryByRole('button', { name: 'Marcar entregado' })).toBeNull(); expect(screen.queryByRole('button', { name: 'Cancelar envío' })).toBeNull(); expect(screen.queryByLabelText('Vehículo')).toBeNull()
  })

  it('requires confirmation before delivering and sends the status only when accepted', async () => {
    const inProgress: Shipment = { ...shipment, status: 'in_progress', vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana', actualPickupAt: '2026-08-01T08:00:00Z' }
    const delivered: Shipment = { ...inProgress, status: 'delivered', actualDeliveryAt: '2026-08-01T12:00:00Z' }; const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true)
    setSession(session); const fetchMock = operationMock(inProgress, delivered); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup(); const button = await screen.findByRole('button', { name: 'Marcar entregado' })
    await user.click(button); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/status'))).toHaveLength(0)
    await user.click(button); await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/status'))).toHaveLength(1)); const statusCall = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/status')); expect(statusCall?.[1]?.method).toBe('POST'); expect(JSON.parse(String(statusCall?.[1]?.body))).toEqual({ status: 'delivered' }); expect(confirm).toHaveBeenCalledTimes(2)
  })

  it('renders the chronological event timeline with user and system actors', async () => {
    const incident: ShipmentEvent = { ...createdEvent, id: 'event-incident', eventType: 'incident', occurredAt: '2026-07-27T10:00:00Z', notes: 'Retraso por avería', recordedByUserId: 'user-1', recordedByUsername: 'ana', createdAt: '2026-07-27T10:05:00Z' }
    setSession(session); vi.stubGlobal('fetch', operationMock(shipment, shipment, [createdEvent, incident])); renderAt('/envios/shipment-1')
    expect(await screen.findByRole('heading', { name: 'Historial de eventos' })).toBeInTheDocument(); expect(screen.getByText('Creación')).toBeInTheDocument(); expect(screen.getByText('Incidencia')).toBeInTheDocument()
    expect(screen.getByText(/Sistema · Automático/)).toBeInTheDocument(); expect(screen.getByText(/ana · Manual/)).toBeInTheDocument(); expect(screen.getByText('Retraso por avería')).toBeInTheDocument()
  })

  it('registers a manual event with ISO time and omitted empty fields without reloading history', async () => {
    const manual: ShipmentEvent = { ...createdEvent, id: 'event-manual', eventType: 'incident', occurredAt: new Date().toISOString(), recordedByUserId: 'user-1', recordedByUsername: 'admin', createdAt: new Date().toISOString() }
    setSession(session); const fetchMock = operationMock(shipment, shipment, [createdEvent], manual); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); await user.selectOptions(screen.getByLabelText('Tipo de evento'), 'incident'); const localTime = (screen.getByLabelText('Fecha y hora') as HTMLInputElement).value; await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    expect(await screen.findByText('Incidencia')).toBeInTheDocument(); const post = fetchMock.mock.calls.find(call => String(call[0]).endsWith('/events') && call[1]?.method === 'POST'); const body = JSON.parse(String(post?.[1]?.body))
    expect(body).toEqual({ eventType: 'incident', occurredAt: new Date(localTime).toISOString() }); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && !call[1]?.method)).toHaveLength(1)
  })

  it('inserts a backdated manual event in its chronological position', async () => {
    const earlier: ShipmentEvent = { ...createdEvent, id: 'event-earlier', eventType: 'checkpoint', occurredAt: '2026-07-25T10:00:00Z', location: 'León', recordedByUserId: 'user-1', recordedByUsername: 'admin', createdAt: '2026-07-30T10:00:00Z' }
    setSession(session); vi.stubGlobal('fetch', operationMock(shipment, shipment, [createdEvent], earlier)); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); fireEvent.change(screen.getByLabelText('Fecha y hora'), { target: { value: '2026-07-25T10:00' } }); await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    await screen.findByText('León'); const entries = screen.getAllByRole('listitem'); expect(entries[0]).toHaveTextContent('Punto de control'); expect(entries[1]).toHaveTextContent('Creación')
  })

  it('blocks a future event locally without calling the API', async () => {
    setSession(session); const fetchMock = operationMock(); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.click(await screen.findByRole('button', { name: 'Registrar evento' })); const future = new Date(Date.now() + 24 * 60 * 60_000); const localFuture = new Date(future.getTime() - future.getTimezoneOffset() * 60_000).toISOString().slice(0, 16); fireEvent.change(screen.getByLabelText('Fecha y hora'), { target: { value: localFuture } }); await user.click(screen.getByRole('button', { name: 'Guardar evento' }))
    expect(screen.getByText('La fecha del evento no puede estar en el futuro.')).toBeInTheDocument(); expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && call[1]?.method === 'POST')).toHaveLength(0)
  })

  it('refreshes automatic events after an operation action', async () => {
    const assigned: Shipment = { ...shipment, vehicleId: 'vehicle-1', driverId: 'driver-1', vehiclePlate: '1234 ABC', driverName: 'Ana' }
    setSession(session); const fetchMock = operationMock(shipment, assigned, [createdEvent]); vi.stubGlobal('fetch', fetchMock); renderAt('/envios/shipment-1'); const user = userEvent.setup()
    await user.selectOptions(await screen.findByLabelText('Vehículo'), 'vehicle-1'); await user.selectOptions(screen.getByLabelText('Conductor'), 'driver-1'); await user.click(screen.getByRole('button', { name: 'Asignar' }))
    await waitFor(() => expect(fetchMock.mock.calls.filter(call => String(call[0]).endsWith('/events') && !call[1]?.method)).toHaveLength(2))
  })
})
