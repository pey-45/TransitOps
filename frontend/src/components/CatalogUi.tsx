import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export function PageHeader({ eyebrow, title, action }: { eyebrow: string; title: string; action?: ReactNode }) {
  return <div className="page-header"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1></div>{action}</div>
}

export function FormField({ id, label, error, children }: { id: string; label: string; error?: string[]; children: ReactNode }) {
  return <div className="form-field"><label htmlFor={id}>{label}</label>{children}{error?.map(message => <span className="field-error" key={message}>{message}</span>)}</div>
}

export function Loading() { return <p className="status-message">Cargando…</p> }
export function Empty({ children }: { children: ReactNode }) { return <p className="empty-state">{children}</p> }
export function BackLink({ to, children = 'Volver al listado' }: { to: string; children?: ReactNode }) { return <Link className="back-link" to={to}>← {children}</Link> }

export function DetailList({ rows }: { rows: [string, ReactNode][] }) {
  return <dl className="detail-list">{rows.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || '—'}</dd></div>)}</dl>
}
