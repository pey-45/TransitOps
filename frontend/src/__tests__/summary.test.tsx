import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderAt, response, session, setSession, setupHarness, summary } from '../test/harness'

setupHarness()

describe('resumen operativo', () => {
  it('shows the operational summary and links status counters to filtered shipments', async () => {
    setSession(session)
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
    setSession(session)
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
})
