import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiClientError, createCustomer, deactivateCustomer, getCustomer, listCustomers, updateCustomer, type Customer, type ValidationDetails } from '../../api/client'
import { BackLink, DetailList, Empty, FormField, Loading, PageHeader } from '../../components/CatalogUi'
import { fieldErrors } from '../../components/form-errors'
import { ErrorAlert } from '../../components/ErrorAlert'

const message = (reason: unknown) => reason instanceof ApiClientError ? reason.message : 'No se pudo conectar con el servidor.'

export function CustomerListPage() {
  const [items, setItems] = useState<Customer[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('')
  useEffect(() => { listCustomers().then(setItems).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [])
  async function deactivate(item: Customer) { if (!window.confirm(`¿Dar de baja a ${item.name}?`)) return; try { await deactivateCustomer(item.id); setItems(current => current.filter(value => value.id !== item.id)) } catch (reason) { setError(message(reason)) } }
  return <section className="content-page"><PageHeader eyebrow="Catálogo" title="Clientes" action={<Link className="button-link" to="/clientes/nuevo">Nuevo cliente</Link>} /><ErrorAlert message={error} />
    {loading ? <Loading /> : items.length === 0 ? <Empty>No hay clientes activos.</Empty> : <div className="table-wrap"><table><thead><tr><th>Nombre</th><th>Contacto</th><th>Acciones</th></tr></thead><tbody>{items.map(item => <tr key={item.id}><td><Link to={`/clientes/${item.id}`}>{item.name}</Link></td><td>{item.contactDetails ?? '—'}</td><td className="actions"><Link to={`/clientes/${item.id}/editar`}>Editar</Link><button className="danger-link" type="button" onClick={() => void deactivate(item)}>Dar de baja</button></td></tr>)}</tbody></table></div>}
  </section>
}

export function CustomerDetailPage() {
  const { id } = useParams(); const [item, setItem] = useState<Customer | null>(null); const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  useEffect(() => { if (id) getCustomer(id).then(setItem).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [id])
  return <section className="content-page"><BackLink to="/clientes" />{loading ? <Loading /> : error ? <ErrorAlert message={error} /> : item && <><PageHeader eyebrow="Cliente" title={item.name} action={<Link className="button-link" to={`/clientes/${item.id}/editar`}>Editar</Link>} /><DetailList rows={[["Datos de contacto", item.contactDetails]]} /></>}</section>
}

export function CustomerFormPage() {
  const { id } = useParams(); const navigate = useNavigate(); const [name, setName] = useState(''); const [contactDetails, setContactDetails] = useState(''); const [loading, setLoading] = useState(Boolean(id)); const [pending, setPending] = useState(false); const [error, setError] = useState(''); const [details, setDetails] = useState<ValidationDetails>()
  useEffect(() => { if (!id) return; getCustomer(id).then(item => { setName(item.name); setContactDetails(item.contactDetails ?? '') }).catch(reason => setError(message(reason))).finally(() => setLoading(false)) }, [id])
  async function submit(event: FormEvent) { event.preventDefault(); setPending(true); setError(''); setDetails(undefined); try { const saved = id ? await updateCustomer(id, { name, contactDetails }) : await createCustomer({ name, contactDetails }); navigate(`/clientes/${saved.id}`) } catch (reason) { setError(message(reason)); if (reason instanceof ApiClientError) setDetails(reason.details) } finally { setPending(false) } }
  return <section className="content-page narrow"><BackLink to="/clientes" /><PageHeader eyebrow={id ? 'Edición' : 'Alta'} title={id ? 'Editar cliente' : 'Nuevo cliente'} />{loading ? <Loading /> : <form className="catalog-form" onSubmit={submit}>
    <FormField id="name" label="Nombre" error={fieldErrors(details, 'Name')}><input id="name" required maxLength={160} value={name} onChange={event => setName(event.target.value)} /></FormField>
    <FormField id="contactDetails" label="Datos de contacto (opcional)" error={fieldErrors(details, 'ContactDetails')}><textarea id="contactDetails" maxLength={500} value={contactDetails} onChange={event => setContactDetails(event.target.value)} /></FormField>
    <ErrorAlert message={error} /><div className="form-actions"><button type="submit" disabled={pending}>{pending ? 'Guardando…' : 'Guardar'}</button><Link className="secondary-link" to="/clientes">Cancelar</Link></div>
  </form>}</section>
}
