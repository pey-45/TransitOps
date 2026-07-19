# TransitOps frontend

SPA de TransitOps construida con Vite, React y TypeScript. Implementa el login del Sprint 1, persistencia de sesión, rutas protegidas, navegación adaptada al rol y presentación uniforme de errores de API.

## Desarrollo

Desde este directorio:

```bash
npm ci
npm run dev
```

Vite sirve la aplicación en `http://localhost:5173` y reenvía `/api` a la API local en `http://localhost:8080`. Para la ejecución integrada recomendada, consulta el `README.md` de la raíz y usa Docker Compose.

## Validación

```bash
npm run lint
npm run build
npm run test
```

Las pruebas usan Vitest, jsdom, React Testing Library y `user-event`.
