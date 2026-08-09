import { expect, test } from '@playwright/test'
import {
  admin,
  createOperatorApi,
  expectApi,
  loginApi,
  loginThroughUi,
  unique,
} from './support'

const initialPassword = 'OperatorPass!2026'

test('Flujo 1 · arranca el primer administrador y bloquea un segundo bootstrap', async ({ page, request }) => {
  const response = await request.post('/api/v1/auth/bootstrap-admin', {
    headers: { 'X-Bootstrap-Token': process.env.E2E_BOOTSTRAP_TOKEN ?? 'transitops-bootstrap-local-only' },
    data: {
      username: unique('second-admin'),
      email: `${unique('second-admin')}@transitops.test`,
      password: 'SecondAdminPass!2026',
    },
  })
  await expectApi(response, 409)
  await expect(await response.json()).toMatchObject({ error: { code: 'first_admin_already_exists' } })

  await loginThroughUi(page, admin.username, admin.password)
  await expect(page.getByRole('link', { name: 'Usuarios' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => document.cookie)).not.toContain('transitops_session')
  await page.reload()
  await expect(page.getByRole('heading', { name: `Hola, ${admin.username}` })).toBeVisible()
})

test('Flujo 2 · un administrador crea un operador que accede y cambia su contraseña', async ({ page }) => {
  const username = unique('operator')
  const nextPassword = 'ChangedPass!2026'

  await loginThroughUi(page, admin.username, admin.password)
  await page.getByRole('link', { name: 'Usuarios' }).click()
  await page.getByRole('link', { name: 'Nuevo usuario' }).click()
  await page.getByLabel('Usuario').fill(username)
  await page.getByLabel('Correo').fill(`${username}@transitops.test`)
  await page.getByLabel('Contraseña inicial').fill(initialPassword)
  await page.getByLabel('Rol').selectOption('operator')
  await page.getByRole('button', { name: 'Crear usuario' }).click()
  await expect(page.getByRole('row').filter({ hasText: username })).toBeVisible()

  await page.getByRole('button', { name: 'Cerrar sesión' }).click()
  await loginThroughUi(page, username, initialPassword)
  await page.getByRole('link', { name: 'Cambiar contraseña' }).click()
  await page.getByLabel('Contraseña actual').fill(initialPassword)
  await page.getByLabel('Nueva contraseña', { exact: true }).fill(nextPassword)
  await page.getByLabel('Repetir nueva contraseña').fill(nextPassword)
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click()
  await expect(page.getByRole('status')).toHaveText('Contraseña cambiada correctamente.')
})

test('Flujo 3 · un operador ejecuta un envío de principio a fin', async ({ page, request }) => {
  const username = unique('operator')
  const suffix = unique('flow3').slice(-12).toUpperCase()
  const plate = `E2E-${suffix}`.slice(0, 20)
  const driver = `Conductor ${suffix}`
  const customer = `Cliente ${suffix}`
  const reference = `E2E-${suffix}`
  await createOperatorApi(request, username, initialPassword)

  await loginThroughUi(page, username, initialPassword)

  await page.getByRole('link', { name: 'Vehículos' }).click()
  await page.getByRole('link', { name: 'Nuevo vehículo' }).click()
  await page.getByLabel('Matrícula').fill(plate)
  await page.getByLabel('Capacidad de carga en kg (opcional)').fill('3000')
  await page.getByRole('button', { name: 'Guardar' }).click()
  await expect(page.getByRole('heading', { name: plate })).toBeVisible()

  await page.getByRole('link', { name: 'Conductores' }).click()
  await page.getByRole('link', { name: 'Nuevo conductor' }).click()
  await page.getByLabel('Nombre').fill(driver)
  await page.getByLabel('Número de carné').fill(`LIC-${suffix}`)
  await page.getByRole('button', { name: 'Guardar' }).click()
  await expect(page.getByRole('heading', { name: driver })).toBeVisible()

  await page.getByRole('link', { name: 'Clientes' }).click()
  await page.getByRole('link', { name: 'Nuevo cliente' }).click()
  await page.getByLabel('Nombre').fill(customer)
  await page.getByRole('button', { name: 'Guardar' }).click()
  await expect(page.getByRole('heading', { name: customer })).toBeVisible()

  await page.getByRole('link', { name: 'Envíos' }).click()
  await page.getByRole('link', { name: 'Nuevo envío' }).click()
  await page.getByLabel('Referencia').fill(reference)
  await page.getByLabel('Origen').fill('Madrid')
  await page.getByLabel('Destino').fill('Barcelona')
  await page.getByLabel('Recogida prevista').fill('2026-08-10T09:00')
  await page.getByLabel('Entrega prevista (opcional)').fill('2026-08-10T17:00')
  await page.getByLabel('Cliente').selectOption({ label: customer })
  await page.getByLabel('Carga estimada en kg (opcional)').fill('5000')
  await page.getByRole('button', { name: 'Guardar' }).click()
  await expect(page.getByRole('heading', { name: reference })).toBeVisible()

  await page.getByLabel('Vehículo').selectOption({ label: plate })
  await page.getByLabel('Conductor').selectOption({ label: driver })
  await page.getByRole('button', { name: 'Asignar' }).click()
  await expect(page.getByRole('status')).toContainText('capacidad del vehículo')

  await page.getByRole('button', { name: 'Poner en curso' }).click()
  await expect(page.getByText('En curso', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Registrar evento' }).click()
  await page.getByLabel('Tipo de evento').selectOption('incident')
  await page.getByLabel('Ubicación (opcional)').fill('Zaragoza')
  await page.getByLabel('Notas (opcional)').fill('Incidencia E2E resuelta')
  await page.getByRole('button', { name: 'Guardar evento' }).click()
  await expect(page.getByText('Incidencia E2E resuelta')).toBeVisible()

  page.once('dialog', dialog => dialog.accept())
  await page.getByRole('button', { name: 'Marcar entregado' }).click()
  await expect(page.getByText('El envío está en un estado final y ya no puede cambiar.')).toBeVisible()
  await expect(page.getByText('Entrega', { exact: true })).toBeVisible()
})

test('Flujo 4 · la baja impide el acceso y conserva el historial', async ({ page, request }) => {
  const username = unique('inactive')
  await createOperatorApi(request, username, initialPassword)
  await loginApi(request, username, initialPassword)
  const reference = unique('HISTORY').toUpperCase()
  const shipmentResponse = await request.post('/api/v1/shipments', {
    data: {
      reference,
      origin: 'Bilbao',
      destination: 'Valencia',
      plannedPickupAt: '2026-08-10T10:00:00Z',
    },
  })
  await expectApi(shipmentResponse, 201)
  const shipment = (await shipmentResponse.json() as { data: { id: string } }).data

  await loginThroughUi(page, admin.username, admin.password)
  await page.getByRole('link', { name: 'Usuarios' }).click()
  const row = page.getByRole('row').filter({ hasText: username })
  page.once('dialog', dialog => dialog.accept())
  await row.getByRole('button', { name: 'Desactivar' }).click()
  await expect(row).not.toBeVisible()

  await page.getByRole('button', { name: 'Cerrar sesión' }).click()
  await page.getByLabel('Usuario').fill(username)
  await page.getByLabel('Contraseña').fill(initialPassword)
  await page.getByRole('button', { name: 'Iniciar sesión' }).click()
  await expect(page.getByRole('alert')).toContainText('El usuario o la contraseña no son válidos.')

  await loginThroughUi(page, admin.username, admin.password)
  await page.goto(`/envios/${shipment.id}`)
  await expect(page.getByRole('heading', { name: reference })).toBeVisible()
  await expect(page.getByText(username, { exact: false })).toBeVisible()
})
