export type Role = 'admin' | 'operator'
export interface User { id: string; username: string; email: string; role: Role; isActive: boolean }
export interface Session { accessToken: string; tokenType: string; expiresAt: string; user: User }
interface ApiResponse<T> { data: T; requestId: string }
export type ValidationDetails = Record<string, string[]>
interface ErrorBody { error?: { code?: string; message?: string; details?: ValidationDetails }; requestId?: string }

export interface Vehicle {
  id: string; licensePlate: string; internalCode: string | null; brand: string | null; model: string | null
  loadCapacity: number | null; isActive: boolean; createdAt: string; updatedAt: string
}
export interface VehicleInput { licensePlate: string; internalCode?: string; brand?: string; model?: string; loadCapacity?: number }
export interface Driver {
  id: string; name: string; licenseNumber: string; employeeCode: string | null; contactDetails: string | null
  isActive: boolean; createdAt: string; updatedAt: string
}
export interface DriverInput { name: string; licenseNumber: string; employeeCode?: string; contactDetails?: string }
export interface Customer {
  id: string; name: string; contactDetails: string | null; isActive: boolean; createdAt: string; updatedAt: string
}
export interface CustomerInput { name: string; contactDetails?: string }
export type ShipmentStatus = 'planned' | 'in_progress' | 'delivered' | 'cancelled'
export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
export interface Shipment {
  id: string; reference: string; origin: string; destination: string; plannedPickupAt: string
  plannedDeliveryAt: string | null; customerId: string | null; customerName: string | null
  estimatedLoad: number | null; notes: string | null; status: ShipmentStatus
  vehicleId: string | null; driverId: string | null; vehiclePlate: string | null; driverName: string | null
  actualPickupAt: string | null; actualDeliveryAt: string | null; capacityWarning: string | null
  createdAt: string; updatedAt: string
}
export interface ShipmentInput {
  reference: string; origin: string; destination: string; plannedPickupAt: string
  plannedDeliveryAt?: string; customerId?: string; estimatedLoad?: number; notes?: string
}
export interface ShipmentFilters {
  status?: ShipmentStatus; pickupFrom?: string; pickupTo?: string; customerId?: string
  vehicleId?: string; driverId?: string; page?: number; pageSize?: number
}

export class ApiClientError extends Error {
  readonly code: string
  readonly requestId?: string
  readonly details?: ValidationDetails

  constructor(message: string, code = 'request_failed', requestId?: string, details?: ValidationDetails) {
    super(message)
    this.code = code
    this.requestId = requestId
    this.details = details
  }
}

const API_URL = import.meta.env.VITE_API_URL ?? ''
const STORAGE_KEY = 'transitops.session'

function getAccessToken(): string | null {
  try {
    return (JSON.parse(localStorage.getItem(STORAGE_KEY) ?? 'null') as Session | null)?.accessToken ?? null
  } catch {
    return null
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getAccessToken()
  const headers = new Headers(options.headers)
  if (options.body) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)
  let response: Response
  try {
    response = await fetch(`${API_URL}${path}`, { ...options, headers })
  } catch {
    throw new ApiClientError('No se pudo conectar con el servidor.')
  }
  if (response.status === 204) return undefined as T
  const body = await response.json() as ApiResponse<T> & ErrorBody
  if (!response.ok) {
    throw new ApiClientError(body.error?.message ?? 'La operación no se pudo completar.', body.error?.code, body.requestId, body.error?.details)
  }
  return body.data
}

export function login(username: string, password: string): Promise<Session> {
  return request('/api/v1/auth/login', { method: 'POST', body: JSON.stringify({ username, password }) })
}

export const listVehicles = () => request<Vehicle[]>('/api/v1/vehicles')
export const getVehicle = (id: string) => request<Vehicle>(`/api/v1/vehicles/${id}`)
export const createVehicle = (input: VehicleInput) => request<Vehicle>('/api/v1/vehicles', { method: 'POST', body: JSON.stringify(input) })
export const updateVehicle = (id: string, input: VehicleInput) => request<Vehicle>(`/api/v1/vehicles/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const deactivateVehicle = (id: string) => request<void>(`/api/v1/vehicles/${id}`, { method: 'DELETE' })

export const listDrivers = () => request<Driver[]>('/api/v1/drivers')
export const getDriver = (id: string) => request<Driver>(`/api/v1/drivers/${id}`)
export const createDriver = (input: DriverInput) => request<Driver>('/api/v1/drivers', { method: 'POST', body: JSON.stringify(input) })
export const updateDriver = (id: string, input: DriverInput) => request<Driver>(`/api/v1/drivers/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const deactivateDriver = (id: string) => request<void>(`/api/v1/drivers/${id}`, { method: 'DELETE' })

export const listCustomers = () => request<Customer[]>('/api/v1/customers')
export const getCustomer = (id: string) => request<Customer>(`/api/v1/customers/${id}`)
export const createCustomer = (input: CustomerInput) => request<Customer>('/api/v1/customers', { method: 'POST', body: JSON.stringify(input) })
export const updateCustomer = (id: string, input: CustomerInput) => request<Customer>(`/api/v1/customers/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const deactivateCustomer = (id: string) => request<void>(`/api/v1/customers/${id}`, { method: 'DELETE' })

function query(params: ShipmentFilters) {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => { if (value !== undefined && value !== '') search.set(key, String(value)) })
  const value = search.toString()
  return value ? `?${value}` : ''
}

export const listShipments = (filters: ShipmentFilters = {}) => request<Page<Shipment>>(`/api/v1/shipments${query(filters)}`)
export const getShipment = (id: string) => request<Shipment>(`/api/v1/shipments/${id}`)
export const createShipment = (input: ShipmentInput) => request<Shipment>('/api/v1/shipments', { method: 'POST', body: JSON.stringify(input) })
export const updateShipment = (id: string, input: ShipmentInput) => request<Shipment>(`/api/v1/shipments/${id}`, { method: 'PUT', body: JSON.stringify(input) })
export const assignShipment = (id: string, input: { vehicleId: string; driverId: string }) => request<Shipment>(`/api/v1/shipments/${id}/assignment`, { method: 'PUT', body: JSON.stringify(input) })
export const unassignShipment = (id: string) => request<Shipment>(`/api/v1/shipments/${id}/assignment`, { method: 'DELETE' })
export const changeShipmentStatus = (id: string, status: ShipmentStatus) => request<Shipment>(`/api/v1/shipments/${id}/status`, { method: 'POST', body: JSON.stringify({ status }) })
