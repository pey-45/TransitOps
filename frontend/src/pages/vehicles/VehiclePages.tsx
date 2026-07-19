import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiClientError, createVehicle, deactivateVehicle, getVehicle, listVehicles, updateVehicle, type ValidationDetails, type Vehicle } from '../../api/client'
import { BackLink, DetailList, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { fieldErrors } from '../../components/form-errors'
import { ErrorAlert } from '../../components/ErrorAlert'

function errorMessage(reason: unknown) { return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.' }

export function VehicleListPage() {
  const [items, setItems] = useState<Vehicle[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  async function load() {
    setLoading(true); setError('')
    try { setItems(await listVehicles()) } catch (reason) { setError(errorMessage(reason)) } finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])
  async function deactivate(item: Vehicle) {
    if (!window.confirm(`¿Dar de baja el vehículo ${item.licensePlate}?`)) return
    try { await deactivateVehicle(item.id); setItems(current => current.filter(value => value.id !== item.id)) }
    catch (reason) { setError(errorMessage(reason)) }
  }
  return <section className="content-page">
    <PageHeader eyebrow="Catálogo" title="Vehículos" action={<Link className="button-link" to="/vehiculos/nuevo">Nuevo vehículo</Link>} />
    <ErrorAlert message={error} />
    {loading ? <Loading /> : items.length === 0 ? <Empty>No hay vehículos activos.</Empty> : <div className="table-wrap"><table>
      <thead><tr><th>Matrícula</th><th>Código</th><th>Vehículo</th><th>Capacidad</th><th>Acciones</th></tr></thead>
      <tbody>{items.map(item => <tr key={item.id}><td><Link to={`/vehiculos/${item.id}`}>{item.licensePlate}</Link></td><td>{item.internalCode ?? '—'}</td><td>{[item.brand, item.model].filter(Boolean).join(' ') || '—'}</td><td>{item.loadCapacity == null ? '—' : `${item.loadCapacity} kg`}</td><td className="actions"><Link to={`/vehiculos/${item.id}/editar`}>Editar</Link><button className="danger-link" type="button" onClick={() => void deactivate(item)}>Dar de baja</button></td></tr>)}</tbody>
    </table></div>}
  </section>
}

export function VehicleDetailPage() {
  const { id } = useParams(); const [item, setItem] = useState<Vehicle | null>(null); const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  useEffect(() => { if (!id) return; getVehicle(id).then(setItem).catch(reason => setError(errorMessage(reason))).finally(() => setLoading(false)) }, [id])
  return <section className="content-page"><BackLink to="/vehiculos" />{loading ? <Loading /> : error ? <ErrorAlert message={error} /> : item && <>
    <PageHeader eyebrow="Vehículo" title={item.licensePlate} action={<Link className="button-link" to={`/vehiculos/${item.id}/editar`}>Editar</Link>} />
    <DetailList rows={[["Código interno", item.internalCode], ["Marca", item.brand], ["Modelo", item.model], ["Capacidad", item.loadCapacity == null ? null : `${item.loadCapacity} kg`]]} />
  </>}</section>
}

export function VehicleFormPage() {
  const { id } = useParams(); const editing = Boolean(id); const navigate = useNavigate()
  const [licensePlate, setLicensePlate] = useState(''); const [internalCode, setInternalCode] = useState(''); const [brand, setBrand] = useState(''); const [model, setModel] = useState(''); const [loadCapacity, setLoadCapacity] = useState('')
  const [loading, setLoading] = useState(editing); const [pending, setPending] = useState(false); const [error, setError] = useState(''); const [details, setDetails] = useState<ValidationDetails>()
  useEffect(() => { if (!id) return; getVehicle(id).then(item => { setLicensePlate(item.licensePlate); setInternalCode(item.internalCode ?? ''); setBrand(item.brand ?? ''); setModel(item.model ?? ''); setLoadCapacity(item.loadCapacity?.toString() ?? '') }).catch(reason => setError(errorMessage(reason))).finally(() => setLoading(false)) }, [id])
  async function submit(event: FormEvent) {
    event.preventDefault(); setPending(true); setError(''); setDetails(undefined)
    const input = { licensePlate, internalCode, brand, model, ...(loadCapacity ? { loadCapacity: Number(loadCapacity) } : {}) }
    try { const saved = id ? await updateVehicle(id, input) : await createVehicle(input); navigate(`/vehiculos/${saved.id}`) }
    catch (reason) { setError(errorMessage(reason)); if (reason instanceof ApiClientError) setDetails(reason.details) } finally { setPending(false) }
  }
  return <section className="content-page narrow"><BackLink to="/vehiculos" /><PageHeader eyebrow={editing ? 'Edición' : 'Alta'} title={editing ? 'Editar vehículo' : 'Nuevo vehículo'} />
    {loading ? <Loading /> : <form className="catalog-form" onSubmit={submit}>
      <FormField id="licensePlate" label="Matrícula" error={fieldErrors(details, 'LicensePlate')}><input id="licensePlate" required maxLength={20} value={licensePlate} onChange={event => setLicensePlate(event.target.value)} /></FormField>
      <FormField id="internalCode" label="Código interno (opcional)" error={fieldErrors(details, 'InternalCode')}><input id="internalCode" maxLength={50} value={internalCode} onChange={event => setInternalCode(event.target.value)} /></FormField>
      <div className="form-grid"><FormField id="brand" label="Marca (opcional)" error={fieldErrors(details, 'Brand')}><input id="brand" maxLength={80} value={brand} onChange={event => setBrand(event.target.value)} /></FormField><FormField id="model" label="Modelo (opcional)" error={fieldErrors(details, 'Model')}><input id="model" maxLength={80} value={model} onChange={event => setModel(event.target.value)} /></FormField></div>
      <FormField id="loadCapacity" label="Capacidad de carga en kg (opcional)" error={fieldErrors(details, 'LoadCapacity')}><input id="loadCapacity" type="number" min="0.01" max="9999999999.99" step="0.01" value={loadCapacity} onChange={event => setLoadCapacity(event.target.value)} /></FormField>
      <ErrorAlert message={error} /><div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Guardando…' : 'Guardar'}</button><Link className="secondary-link" to="/vehiculos">Cancelar</Link></div>
    </form>}
  </section>
}
