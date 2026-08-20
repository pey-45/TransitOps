import { expect, type APIRequestContext, type APIResponse, type Page } from '@playwright/test'

export const admin = {
  username: 'e2e.admin',
  email: 'e2e.admin@transitops.test',
  password: 'E2eAdminPass!2026',
}

interface Envelope<T> {
  data: T
}

interface UserData {
  id: string
  username: string
}

export function unique(prefix: string) {
  return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`
}

export async function loginThroughUi(page: Page, username: string, password: string) {
  await page.goto('/login')
  await page.getByLabel('Usuario').fill(username)
  await page.getByLabel('Contraseña').fill(password)
  await page.getByRole('button', { name: 'Iniciar sesión' }).click()
  await expect(page.getByRole('heading', { name: `Hola, ${username}` })).toBeVisible()
}

// La sesión viaja en la cookie HttpOnly que emite el login. El APIRequestContext de Playwright
// la conserva en su propio jar y la reenvía en las peticiones siguientes, así que no hay
// ninguna cabecera de autorización que construir ni propagar.
export async function loginApi(request: APIRequestContext, username: string, password: string) {
  const response = await request.post('/api/v1/auth/login', { data: { username, password } })
  await expectApi(response, 200)
}

export async function createOperatorApi(
  request: APIRequestContext,
  username: string,
  password: string,
) {
  await loginApi(request, admin.username, admin.password)
  const response = await request.post('/api/v1/users', {
    data: {
      username,
      email: `${username}@transitops.test`,
      password,
      role: 'operator',
    },
  })
  await expectApi(response, 201)
  return (await response.json() as Envelope<UserData>).data
}

export async function expectApi(response: APIResponse, status: number) {
  if (response.status() !== status) {
    throw new Error(`API ${response.url()} devolvió ${response.status()}: ${await response.text()}`)
  }
}
