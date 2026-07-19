import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiClientError, createDriver, deactivateDriver, getDriver, listDrivers, updateDriver, type Driver, type ValidationDetails } from '../../api/client'
import { BackLink, DetailList, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { fieldErrors } from '../../components/form-errors'
import { ErrorAlert } from '../../components/ErrorAlert'

const message = (reason: unknown) => reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'

export function DriverListPage() {
  const [items, setItems] = useState<Driver[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  useEffect(() => { listDrivers().then(setItems).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [])
  async function deactivate(item: Driver) { if (!window.confirm(`¿Dar de baja a ${item.name}?`)) return; try { await deactivateDriver(item.id); setItems(current => current.filter(value => value.id !== item.id)) } catch (reason) { setError(message(reason)) } }
  return <section className="content-page"><PageHeader eyebrow="Catálogo" title="Conductores" action={<Link className="button-link" to="/conductores/nuevo">Nuevo conductor</Link>} /><ErrorAlert message={error} />
    {loading ? <Loading /> : items.length === 0 ? <Empty>No hay conductores activos.</Empty> : <div className="table-wrap"><table><thead><tr><th>Nombre</th><th>N.º de carné</th><th>Código</th><th>Contacto</th><th>Acciones</th></tr></thead><tbody>{items.map(item => <tr key={item.id}><td><Link to={`/conductores/${item.id}`}>{item.name}</Link></td><td>{item.licenseNumber}</td><td>{item.employeeCode ?? '—'}</td><td>{item.contactDetails ?? '—'}</td><td className="actions"><Link to={`/conductores/${item.id}/editar`}>Editar</Link><button className="danger-link" type="button" onClick={() => void deactivate(item)}>Dar de baja</button></td></tr>)}</tbody></table></div>}
  </section>
}

export function DriverDetailPage() {
  const { id } = useParams(); const [item, setItem] = useState<Driver | null>(null); const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  useEffect(() => { if (id) getDriver(id).then(setItem).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [id])
  return <section className="content-page"><BackLink to="/conductores" />{loading ? <Loading /> : error ? <ErrorAlert message={error} /> : item && <><PageHeader eyebrow="Conductor" title={item.name} action={<Link className="button-link" to={`/conductores/${item.id}/editar`}>Editar</Link>} /><DetailList rows={[["Número de carné", item.licenseNumber], ["Código de empleado", item.employeeCode], ["Contacto", item.contactDetails]]} /></>}</section>
}

export function DriverFormPage() {
  const { id } = useParams(); const navigate = useNavigate(); const [name, setName] = useState(''); const [licenseNumber, setLicenseNumber] = useState(''); const [employeeCode, setEmployeeCode] = useState(''); const [contactDetails, setContactDetails] = useState(''); const [loading, setLoading] = useState(Boolean(id)); const [pending, setPending] = useState(false); const [error, setError] = useState(''); const [details, setDetails] = useState<ValidationDetails>()
  useEffect(() => { if (!id) return; getDriver(id).then(item => { setName(item.name); setLicenseNumber(item.licenseNumber); setEmployeeCode(item.employeeCode ?? ''); setContactDetails(item.contactDetails ?? '') }).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [id])
  async function submit(event: FormEvent) { event.preventDefault(); setPending(true); setError(''); setDetails(undefined); try { const saved = id ? await updateDriver(id, { name, licenseNumber, employeeCode, contactDetails }) : await createDriver({ name, licenseNumber, employeeCode, contactDetails }); navigate(`/conductores/${saved.id}`) } catch (reason) { setError(message(reason)); if (reason instanceof ApiClientError) setDetails(reason.details) } finally { setPending(false) } }
  return <section className="content-page narrow"><BackLink to="/conductores" /><PageHeader eyebrow={id ? 'Edición' : 'Alta'} title={id ? 'Editar conductor' : 'Nuevo conductor'} />{loading ? <Loading /> : <form className="catalog-form" onSubmit={submit}>
    <FormField id="name" label="Nombre" error={fieldErrors(details, 'Name')}><input id="name" required maxLength={160} value={name} onChange={event => setName(event.target.value)} /></FormField>
    <FormField id="licenseNumber" label="Número de carné" error={fieldErrors(details, 'LicenseNumber')}><input id="licenseNumber" required maxLength={50} value={licenseNumber} onChange={event => setLicenseNumber(event.target.value)} /></FormField>
    <FormField id="employeeCode" label="Código de empleado (opcional)" error={fieldErrors(details, 'EmployeeCode')}><input id="employeeCode" maxLength={50} value={employeeCode} onChange={event => setEmployeeCode(event.target.value)} /></FormField>
    <FormField id="contactDetails" label="Datos de contacto (opcional)" error={fieldErrors(details, 'ContactDetails')}><textarea id="contactDetails" maxLength={500} value={contactDetails} onChange={event => setContactDetails(event.target.value)} /></FormField>
    <ErrorAlert message={error} /><div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Guardando…' : 'Guardar'}</button><Link className="secondary-link" to="/conductores">Cancelar</Link></div>
  </form>}</section>
}
