import { cleanup, render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, vi } from 'vitest'
import { AppRoutes } from '../App'
import type { Session, SummaryResponse } from '../api/client'
import { AuthProvider } from '../auth/AuthContext'

export const session: Session = {
  expiresAt: '2099-01-01T00:00:00Z',
  user: { id: 'user-1', username: 'admin', email: 'admin@test.dev', role: 'admin', isActive: true },
}

export const summary: SummaryResponse = {
  shipments: { planned: 2, inProgress: 1, delivered: 4, cancelled: 1, total: 8 },
  vehicles: [{ id: 'vehicle-1', label: '1234 ABC', shipmentCount: 3 }],
  drivers: [{ id: 'driver-1', label: 'Ana', shipmentCount: 3 }],
  incidents: 2,
  from: '2026-07-01T00:00:00Z',
  to: '2026-07-31T23:59:59Z',
}

// `null` renderiza sin sesión; `undefined` deja que AuthProvider rehidrate contra /auth/me.
let currentSession: Session | null | undefined = null

export function setSession(next: Session | null | undefined) {
  currentSession = next
}

export function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider initialSession={currentSession}><AppRoutes /></AuthProvider>
    </MemoryRouter>,
  )
}

export function response(data: unknown, ok = true, status = 200) {
  return Promise.resolve({ ok, status, json: async () => data })
}

// Aislamiento entre pruebas. Cada fichero de pruebas lo invoca una vez, en su raíz.
export function setupHarness() {
  beforeEach(() => { setSession(null) })
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
    vi.unstubAllGlobals()
  })
}
