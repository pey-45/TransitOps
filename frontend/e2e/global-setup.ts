import { request, type FullConfig } from '@playwright/test'
import { admin, expectApi } from './support'

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0]?.use.baseURL
  if (typeof baseURL !== 'string') throw new Error('Playwright necesita una baseURL para preparar el E2E.')

  const api = await request.newContext({ baseURL })
  try {
    const bootstrap = await api.post('/api/v1/auth/bootstrap-admin', {
      headers: { 'X-Bootstrap-Token': process.env.E2E_BOOTSTRAP_TOKEN ?? 'transitops-bootstrap-local-only' },
      data: admin,
    })
    if (![201, 409].includes(bootstrap.status())) {
      await expectApi(bootstrap, 201)
    }

    const login = await api.post('/api/v1/auth/login', {
      data: { username: admin.username, password: admin.password },
    })
    if (!login.ok()) {
      throw new Error(
        'La base ya contiene otro administrador y no admite las credenciales E2E. ' +
        'Usa una base dedicada o conserva e2e.admin entre ejecuciones.',
      )
    }
  } finally {
    await api.dispose()
  }
}
