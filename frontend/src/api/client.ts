export type Role = 'admin' | 'operator'
export interface User { id: string; username: string; email: string; role: Role; isActive: boolean }
export interface Session { accessToken: string; tokenType: string; expiresAt: string; user: User }
interface ApiResponse<T> { data: T; requestId: string }
interface ErrorBody { error?: { code?: string; message?: string }; requestId?: string }

export class ApiClientError extends Error {
  readonly code: string
  readonly requestId?: string

  constructor(message: string, code = 'request_failed', requestId?: string) {
    super(message)
    this.code = code
    this.requestId = requestId
  }
}

const API_URL = import.meta.env.VITE_API_URL ?? ''

export async function login(username: string, password: string): Promise<Session> {
  const response = await fetch(`${API_URL}/api/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  })
  const body = await response.json() as ApiResponse<Session> & ErrorBody
  if (!response.ok) {
    throw new ApiClientError(body.error?.message ?? 'No se pudo iniciar sesión.', body.error?.code, body.requestId)
  }
  return body.data
}
