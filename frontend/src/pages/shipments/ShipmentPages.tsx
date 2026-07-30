import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import {
  ApiClientError, assignShipment, changeShipmentStatus, createShipment, getShipment, listCustomers,
  listDrivers, listShipments, listVehicles, unassignShipment, updateShipment, type Customer,
  type Driver, type Page, type Shipment, type ShipmentInput, type ShipmentStatus,
  type ValidationDetails, type Vehicle,
} from '../../api/client'
import { BackLink, DetailList, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { ErrorAlert } from '../../components/ErrorAlert'
import { fieldErrors } from '../../components/form-errors'

function errorMessage(reason: unknown) { return reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.' }
function formatDate(value: string | null) { return value ? new Date(value).toLocaleString() : '—' }
function statusLabel(status: ShipmentStatus) { return { planned: 'Planificado', in_progress: 'En curso', delivered: 'Entregado', cancelled: 'Cancelado' }[status] }
function StatusChip({ status }: { status: ShipmentStatus }) { return <span className={`status-chip status-${status}`}>{statusLabel(status)}</span> }
function dayStart(value: string) { return value ? new Date(`${value}T00:00`).toISOString() : undefined }
function dayEnd(value: string) { if (!value) return undefined; const date = new Date(`${value}T00:00`); date.setHours(23, 59, 59, 999); return date.toISOString() }
function toLocalInput(value: string | null) {
  if (!value) return ''
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

export function ShipmentListPage() {
  const [params, setParams] = useSearchParams()
  const serializedParams = params.toString()
  const [result, setResult] = useState<Page<Shipment> | null>(null)
  const [vehicles, setVehicles] = useState<Vehicle[]>([]); const [drivers, setDrivers] = useState<Driver[]>([])
  const [status, setStatus] = useState(''); const [pickupFrom, setPickupFrom] = useState(''); const [pickupTo, setPickupTo] = useState('')
  const [vehicleId, setVehicleId] = useState(''); const [driverId, setDriverId] = useState('')
  const [loading, setLoading] = useState(true); const [error, setError] = useState('')

  useEffect(() => { Promise.all([listVehicles(), listDrivers()]).then(([v, d]) => { setVehicles(v); setDrivers(d) }).catch(reason => setError(errorMessage(reason))) }, [])
  useEffect(() => {
    const current = new URLSearchParams(serializedParams)
    setStatus(current.get('status') ?? ''); setPickupFrom(current.get('pickupFrom') ?? ''); setPickupTo(current.get('pickupTo') ?? '')
    setVehicleId(current.get('vehicleId') ?? ''); setDriverId(current.get('driverId') ?? '')
  }, [serializedParams])
  useEffect(() => {
    let ignore = false; setLoading(true); setError(''); const current = new URLSearchParams(serializedParams)
    const page = Number(current.get('page') ?? '1')
    listShipments({ status: (current.get('status') || undefined) as ShipmentStatus | undefined,
      pickupFrom: dayStart(current.get('pickupFrom') ?? ''), pickupTo: dayEnd(current.get('pickupTo') ?? ''),
      vehicleId: current.get('vehicleId') || undefined, driverId: current.get('driverId') || undefined, page })
      .then(value => { if (!ignore) setResult(value) }).catch(reason => { if (!ignore) setError(errorMessage(reason)) })
      .finally(() => { if (!ignore) setLoading(false) })
    return () => { ignore = true }
  }, [serializedParams])

  function filter(event: FormEvent) {
    event.preventDefault(); const next = new URLSearchParams()
    if (status) next.set('status', status); if (pickupFrom) next.set('pickupFrom', pickupFrom); if (pickupTo) next.set('pickupTo', pickupTo)
    if (vehicleId) next.set('vehicleId', vehicleId); if (driverId) next.set('driverId', driverId); setParams(next)
  }
  function goToPage(page: number) { const next = new URLSearchParams(params); if (page <= 1) next.delete('page'); else next.set('page', String(page)); setParams(next) }
  const hasFilters = Boolean(params.get('status') || params.get('pickupFrom') || params.get('pickupTo') || params.get('vehicleId') || params.get('driverId'))
  return <section className="content-page">
    <PageHeader eyebrow="Operaciones" title="Envíos" action={<Link className="button-link" to="/envios/nuevo">Nuevo envío</Link>} />
    <form className="catalog-form filter-bar" onSubmit={filter}>
      <FormField id="status" label="Estado"><select id="status" value={status} onChange={event => setStatus(event.target.value)}><option value="">Todos</option><option value="planned">Planificado</option><option value="in_progress">En curso</option><option value="delivered">Entregado</option><option value="cancelled">Cancelado</option></select></FormField>
      <FormField id="pickupFrom" label="Recogida desde"><input id="pickupFrom" type="date" value={pickupFrom} onChange={event => setPickupFrom(event.target.value)} /></FormField>
      <FormField id="pickupTo" label="Recogida hasta"><input id="pickupTo" type="date" value={pickupTo} onChange={event => setPickupTo(event.target.value)} /></FormField>
      <FormField id="vehicleId" label="Vehículo"><select id="vehicleId" value={vehicleId} onChange={event => setVehicleId(event.target.value)}><option value="">Todos</option>{vehicles.map(item => <option key={item.id} value={item.id}>{item.licensePlate}</option>)}</select></FormField>
      <FormField id="driverId" label="Conductor"><select id="driverId" value={driverId} onChange={event => setDriverId(event.target.value)}><option value="">Todos</option>{drivers.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></FormField>
      <div className="form-actions filter-actions"><button type="submit">Filtrar</button><button className="secondary" type="button" onClick={() => setParams({})}>Limpiar</button></div>
    </form>
    <ErrorAlert message={error} />
    {loading ? <Loading /> : !result || result.items.length === 0 ? <Empty>{hasFilters ? 'Ningún envío coincide con los filtros.' : 'Todavía no hay envíos.'}</Empty> : <>
      <div className="table-wrap"><table><thead><tr><th>Referencia</th><th>Estado</th><th>Ruta</th><th>Recogida</th><th>Entrega</th><th>Cliente</th><th>Vehículo</th><th>Conductor</th><th>Acciones</th></tr></thead>
      <tbody>{result.items.map(item => <tr key={item.id}><td><Link to={`/envios/${item.id}`}>{item.reference}</Link></td><td><StatusChip status={item.status} /></td><td>{item.origin} → {item.destination}</td><td>{formatDate(item.plannedPickupAt)}</td><td>{formatDate(item.plannedDeliveryAt)}</td><td>{item.customerName ?? '—'}</td><td>{item.vehiclePlate ?? '—'}</td><td>{item.driverName ?? '—'}</td><td><Link to={`/envios/${item.id}/editar`}>Editar</Link></td></tr>)}</tbody></table></div>
      <div className="pagination"><span>Página {result.page} de {result.totalPages} · {result.totalCount} envíos</span><div className="actions"><button type="button" disabled={result.page <= 1} onClick={() => goToPage(result.page - 1)}>Anterior</button><button type="button" disabled={result.page >= result.totalPages} onClick={() => goToPage(result.page + 1)}>Siguiente</button></div></div>
    </>}
  </section>
}

export function ShipmentFormPage() {
  const { id } = useParams(); const editing = Boolean(id); const navigate = useNavigate()
  const [customers, setCustomers] = useState<Customer[]>([]); const [inactiveCustomer, setInactiveCustomer] = useState<{ id: string; name: string } | null>(null)
  const [reference, setReference] = useState(''); const [origin, setOrigin] = useState(''); const [destination, setDestination] = useState('')
  const [pickup, setPickup] = useState(''); const [delivery, setDelivery] = useState(''); const [customerId, setCustomerId] = useState('')
  const [estimatedLoad, setEstimatedLoad] = useState(''); const [notes, setNotes] = useState('')
  const [loading, setLoading] = useState(true); const [pending, setPending] = useState(false); const [error, setError] = useState(''); const [details, setDetails] = useState<ValidationDetails>(); const [pickupError, setPickupError] = useState(''); const [dateError, setDateError] = useState(''); const [referenceConflict, setReferenceConflict] = useState('')
  useEffect(() => {
    Promise.all([listCustomers(), id ? getShipment(id) : Promise.resolve(null)]).then(([activeCustomers, item]) => {
      setCustomers(activeCustomers)
      if (item) { setReference(item.reference); setOrigin(item.origin); setDestination(item.destination); setPickup(toLocalInput(item.plannedPickupAt)); setDelivery(toLocalInput(item.plannedDeliveryAt)); setCustomerId(item.customerId ?? ''); setEstimatedLoad(item.estimatedLoad?.toString() ?? ''); setNotes(item.notes ?? '')
        if (item.customerId && !activeCustomers.some(customer => customer.id === item.customerId)) setInactiveCustomer({ id: item.customerId, name: item.customerName ?? 'Cliente' }) }
    }).catch(reason => setError(errorMessage(reason))).finally(() => setLoading(false))
  }, [id])
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setDetails(undefined); setPickupError(''); setDateError(''); setReferenceConflict('')
    const pickupDate = new Date(pickup); const deliveryDate = delivery ? new Date(delivery) : null
    if (!pickup || Number.isNaN(pickupDate.getTime())) { setPickupError('Indica una fecha de recogida válida.'); return }
    if (deliveryDate && Number.isNaN(deliveryDate.getTime())) { setDateError('Indica una fecha de entrega válida.'); return }
    if (delivery && delivery < pickup) { setDateError('La entrega no puede ser anterior a la recogida.'); return }
    setPending(true)
    const input: ShipmentInput = { reference, origin, destination, plannedPickupAt: pickupDate.toISOString(), notes,
      ...(deliveryDate ? { plannedDeliveryAt: deliveryDate.toISOString() } : {}), ...(customerId ? { customerId } : {}),
      ...(estimatedLoad ? { estimatedLoad: Number(estimatedLoad) } : {}) }
    try { const saved = id ? await updateShipment(id, input) : await createShipment(input); navigate(`/envios/${saved.id}`) }
    catch (reason) { setError(errorMessage(reason)); if (reason instanceof ApiClientError) { setDetails(reason.details); if (reason.code === 'shipment_reference_conflict') setReferenceConflict('Usa una referencia diferente.') } }
    finally { setPending(false) }
  }
  return <section className="content-page narrow"><BackLink to="/envios" /><PageHeader eyebrow={editing ? 'Edición' : 'Alta'} title={editing ? 'Editar envío' : 'Nuevo envío'} />
    {loading ? <Loading /> : <form className="catalog-form" onSubmit={submit}>
      <FormField id="reference" label="Referencia" error={[...(fieldErrors(details, 'Reference') ?? []), ...(referenceConflict ? [referenceConflict] : [])]}><input id="reference" required maxLength={50} value={reference} onChange={event => setReference(event.target.value)} /></FormField>
      <div className="form-grid"><FormField id="origin" label="Origen" error={fieldErrors(details, 'Origin')}><input id="origin" required maxLength={160} value={origin} onChange={event => setOrigin(event.target.value)} /></FormField><FormField id="destination" label="Destino" error={fieldErrors(details, 'Destination')}><input id="destination" required maxLength={160} value={destination} onChange={event => setDestination(event.target.value)} /></FormField></div>
      <div className="form-grid"><FormField id="pickup" label="Recogida prevista" error={[...(fieldErrors(details, 'PlannedPickupAt') ?? []), ...(pickupError ? [pickupError] : [])]}><input id="pickup" required type="datetime-local" value={pickup} onChange={event => setPickup(event.target.value)} /></FormField><FormField id="delivery" label="Entrega prevista (opcional)" error={[...(fieldErrors(details, 'PlannedDeliveryAt') ?? []), ...(dateError ? [dateError] : [])]}><input id="delivery" type="datetime-local" value={delivery} onChange={event => setDelivery(event.target.value)} /></FormField></div>
      <FormField id="customerId" label="Cliente"><select id="customerId" value={customerId} onChange={event => setCustomerId(event.target.value)}><option value="">Sin cliente</option>{customers.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}{inactiveCustomer && <option value={inactiveCustomer.id}>{inactiveCustomer.name} (dado de baja)</option>}</select></FormField>
      <FormField id="estimatedLoad" label="Carga estimada en kg (opcional)" error={fieldErrors(details, 'EstimatedLoad')}><input id="estimatedLoad" type="number" min="0.01" max="9999999999.99" step="0.01" value={estimatedLoad} onChange={event => setEstimatedLoad(event.target.value)} /></FormField>
      <FormField id="notes" label="Notas (opcional)" error={fieldErrors(details, 'Notes')}><textarea id="notes" maxLength={500} value={notes} onChange={event => setNotes(event.target.value)} /></FormField>
      <ErrorAlert message={error} /><div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Guardando…' : 'Guardar'}</button><Link className="secondary-link" to="/envios">Cancelar</Link></div>
    </form>}
  </section>
}

export function ShipmentDetailPage() {
  const { id } = useParams(); const [item, setItem] = useState<Shipment | null>(null); const [vehicles, setVehicles] = useState<Vehicle[]>([]); const [drivers, setDrivers] = useState<Driver[]>([])
  const [vehicleId, setVehicleId] = useState(''); const [driverId, setDriverId] = useState(''); const [error, setError] = useState(''); const [warning, setWarning] = useState('')
  const [loading, setLoading] = useState(true); const [pending, setPending] = useState(false)
  useEffect(() => {
    if (!id) return
    Promise.all([getShipment(id), listVehicles(), listDrivers()]).then(([shipment, activeVehicles, activeDrivers]) => {
      setItem(shipment); setVehicles(activeVehicles); setDrivers(activeDrivers); setVehicleId(shipment.vehicleId ?? ''); setDriverId(shipment.driverId ?? '')
    }).catch(reason => setError(errorMessage(reason))).finally(() => setLoading(false))
  }, [id])

  async function assign(event: FormEvent) {
    event.preventDefault(); if (!id || !vehicleId || !driverId) return; setPending(true); setError(''); setWarning('')
    try { const saved = await assignShipment(id, { vehicleId, driverId }); setItem(saved); setWarning(saved.capacityWarning ?? '') }
    catch (reason) { setError(errorMessage(reason)) } finally { setPending(false) }
  }

  async function unassign() {
    if (!id) return; setPending(true); setError(''); setWarning('')
    try { const saved = await unassignShipment(id); setItem(saved); setVehicleId(''); setDriverId('') }
    catch (reason) { setError(errorMessage(reason)) } finally { setPending(false) }
  }

  async function transition(status: ShipmentStatus) {
    if (!id) return
    if ((status === 'delivered' || status === 'cancelled') && !window.confirm(status === 'delivered' ? '¿Marcar este envío como entregado? Esta acción no se puede deshacer.' : '¿Cancelar este envío? Esta acción no se puede deshacer.')) return
    setPending(true); setError(''); setWarning('')
    try { setItem(await changeShipmentStatus(id, status)) }
    catch (reason) { setError(errorMessage(reason)) } finally { setPending(false) }
  }

  const inactiveVehicle = item?.vehicleId && !vehicles.some(vehicle => vehicle.id === item.vehicleId)
  const inactiveDriver = item?.driverId && !drivers.some(driver => driver.id === item.driverId)
  const assigned = Boolean(item?.vehicleId && item?.driverId)
  return <section className="content-page"><BackLink to="/envios" />{loading ? <Loading /> : !item ? <ErrorAlert message={error || 'No se pudo cargar el envío.'} /> : <><PageHeader eyebrow="Envío" title={item.reference} action={<Link className="button-link" to={`/envios/${item.id}/editar`}>Editar</Link>} />
    <ErrorAlert message={error} />{warning && <div className="notice" role="status">{warning}</div>}
    <DetailList rows={[["Estado", <StatusChip key="status" status={item.status} />], ["Origen", item.origin], ["Destino", item.destination], ["Recogida prevista", formatDate(item.plannedPickupAt)], ["Entrega prevista", formatDate(item.plannedDeliveryAt)], ["Recogida real", formatDate(item.actualPickupAt)], ["Entrega real", formatDate(item.actualDeliveryAt)], ["Cliente", item.customerName], ["Carga estimada", item.estimatedLoad == null ? null : `${item.estimatedLoad} kg`], ["Notas", item.notes], ["Vehículo", item.vehiclePlate], ["Conductor", item.driverName]]} />
    {item.status === 'planned' && <form className="operation-panel" onSubmit={assign}>
      <div><p className="eyebrow">Recursos</p><h2>{assigned ? 'Reasignar vehículo y conductor' : 'Asignar vehículo y conductor'}</h2><p className="hint">La asignación se confirma de forma conjunta y solo mientras el envío está planificado.</p></div>
      <div className="form-grid">
        <FormField id="assignmentVehicle" label="Vehículo"><select id="assignmentVehicle" value={vehicleId} onChange={event => setVehicleId(event.target.value)}><option value="">Selecciona un vehículo</option>{vehicles.map(vehicle => <option key={vehicle.id} value={vehicle.id}>{vehicle.licensePlate}</option>)}{inactiveVehicle && <option value={item.vehicleId!}>{item.vehiclePlate ?? 'Vehículo'} (dado de baja)</option>}</select></FormField>
        <FormField id="assignmentDriver" label="Conductor"><select id="assignmentDriver" value={driverId} onChange={event => setDriverId(event.target.value)}><option value="">Selecciona un conductor</option>{drivers.map(driver => <option key={driver.id} value={driver.id}>{driver.name}</option>)}{inactiveDriver && <option value={item.driverId!}>{item.driverName ?? 'Conductor'} (dado de baja)</option>}</select></FormField>
      </div><div className="operation-actions"><button type="submit" disabled={pending || !vehicleId || !driverId}>{pending ? 'Guardando…' : assigned ? 'Reasignar' : 'Asignar'}</button>{assigned && <button className="secondary" type="button" disabled={pending} onClick={unassign}>Quitar asignación</button>}</div>
    </form>}
    <section className="operation-panel" aria-labelledby="shipment-lifecycle"><div><p className="eyebrow">Ciclo de vida</p><h2 id="shipment-lifecycle">Actualizar estado</h2></div>
      {item.status === 'planned' && <><div className="operation-actions"><button type="button" disabled={pending || !assigned} onClick={() => transition('in_progress')}>Poner en curso</button><button className="danger" type="button" disabled={pending} onClick={() => transition('cancelled')}>Cancelar envío</button></div>{!assigned && <p className="hint">Asigna primero un vehículo y un conductor para poner el envío en curso.</p>}</>}
      {item.status === 'in_progress' && <div className="operation-actions"><button type="button" disabled={pending} onClick={() => transition('delivered')}>Marcar entregado</button><button className="danger" type="button" disabled={pending} onClick={() => transition('cancelled')}>Cancelar envío</button></div>}
      {(item.status === 'delivered' || item.status === 'cancelled') && <p className="hint">El envío está en un estado final y ya no puede cambiar.</p>}
    </section>
  </>}</section>
}
