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
  })
})
