import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiClientError, getSummary, type ResourceActivity, type ShipmentStatus, type SummaryResponse } from '../api/client'
import { Empty, Loading, StatusChip } from '../components/CatalogUi'
import { dayEnd, dayStart } from '../components/dates'
import { ErrorAlert } from '../components/ErrorAlert'
import { useAuth } from '../auth/auth-state'

const statusCards: { key: keyof SummaryResponse['shipments']; status?: ShipmentStatus; label: string }[] = [
  { key: 'planned', status: 'planned', label: 'Planificados' },
  { key: 'inProgress', status: 'in_progress', label: 'En curso' },
  { key: 'delivered', status: 'delivered', label: 'Entregados' },
  { key: 'cancelled', status: 'cancelled', label: 'Cancelados' },
  { key: 'total', label: 'Total' },
]

function message(reason: unknown) {
  return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'
}

function inputDate(value: string) {
  const date = new Date(value)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

function ActivityTable({ title, items }: { title: string; items: ResourceActivity[] }) {
  return <section className="activity-card"><h3>{title}</h3>{items.length === 0
    ? <Empty>Sin actividad en el periodo.</Empty>
    : <div className="table-wrap"><table><thead><tr><th>Recurso</th><th>Envíos</th></tr></thead>
      <tbody>{items.map(item => <tr key={item.id}><td>{item.label}</td><td>{item.shipmentCount}</td></tr>)}</tbody></table></div>}</section>
}

export function HomePage() {
  const { session } = useAuth()
  const [summary, setSummary] = useState<SummaryResponse | null>(null)
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let ignore = false
    getSummary().then(value => {
      if (ignore) return
      setSummary(value)
      setFrom(inputDate(value.from))
      setTo(inputDate(value.to))
    }).catch(reason => { if (!ignore) setError(message(reason)) })
      .finally(() => { if (!ignore) setLoading(false) })
    return () => { ignore = true }
  }, [])

  async function filter(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      setSummary(await getSummary(dayStart(from), dayEnd(to)))
    } catch (reason) {
      setError(message(reason))
    } finally {
      setLoading(false)
    }
  }

  return <section className="content-page summary-page">
    <p className="eyebrow">Resumen operativo</p>
    <h1>Hola, {session?.user.username}</h1>
    <ErrorAlert message={error} />
    {loading && !summary ? <Loading /> : summary && <>
      <section className="summary-section" aria-labelledby="current-status">
        <div><h2 id="current-status">Envíos por estado</h2><p>Situación actual; estos contadores no dependen del periodo.</p></div>
        <div className="summary-cards">{statusCards.map(card => {
          const content = <><strong>{summary.shipments[card.key]}</strong>
            <span>{card.status ? <StatusChip status={card.status} /> : card.label}</span></>
          return card.status
            ? <Link className="summary-card" key={card.key} to={`/envios?status=${card.status}`}>{content}</Link>
            : <div className="summary-card" key={card.key}>{content}</div>
        })}</div>
      </section>
      <section className="summary-section" aria-labelledby="period-activity">
        <div><h2 id="period-activity">Actividad en el periodo</h2><p>Envíos por fecha prevista de recogida e incidencias por fecha del suceso.</p></div>
        <form className="catalog-form summary-filter" onSubmit={filter}>
          <label htmlFor="summaryFrom">Desde</label><input id="summaryFrom" type="date" value={from} onChange={event => setFrom(event.target.value)} />
          <label htmlFor="summaryTo">Hasta</label><input id="summaryTo" type="date" value={to} onChange={event => setTo(event.target.value)} />
          <button type="submit" disabled={loading}>{loading ? 'Actualizando…' : 'Aplicar periodo'}</button>
        </form>
        <div className="incident-card"><span>Incidencias registradas</span><strong>{summary.incidents}</strong></div>
        <div className="activity-grid">
          <ActivityTable title="Actividad por vehículo" items={summary.vehicles} />
          <ActivityTable title="Actividad por conductor" items={summary.drivers} />
        </div>
      </section>
    </>}
  </section>
}
