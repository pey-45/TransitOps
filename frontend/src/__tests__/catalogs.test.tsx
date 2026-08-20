import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderAt, session, setSession, setupHarness } from '../test/harness'

setupHarness()

describe('catálogos', () => {
  it('creates a vehicle and redirects to its detail', async () => {
    setSession(session)
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
    setSession(session)
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
