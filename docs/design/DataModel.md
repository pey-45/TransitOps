# Modelo de datos de TransitOps

## Alcance

Este diseño cubre el dominio completo definido en `docs/Requirements.md`. El Sprint 1 implementó y migró `AppUser`; el Sprint 2 incorporó `Vehicle`, `Driver` y `Customer`; el Sprint 3 incorporó `Shipment`, y el Sprint 4 completó su operación con asignación, estados y fechas reales. `ShipmentEvent` se incorporará mediante una migración incremental en su sprint.

```mermaid
erDiagram
    APP_USER ||--o{ SHIPMENT_EVENT : registra
    CUSTOMER o|--o{ SHIPMENT : solicita
    VEHICLE o|--o{ SHIPMENT : realiza
    DRIVER o|--o{ SHIPMENT : conduce
    SHIPMENT ||--o{ SHIPMENT_EVENT : contiene

    APP_USER {
      uuid id PK
      string username UK
      string email UK
      string password_hash
      enum role
      boolean is_active
      datetime created_at
      datetime updated_at
    }
    VEHICLE {
      uuid id PK
      string license_plate
      string internal_code
      string brand
      string model
      decimal load_capacity
      boolean is_active
      datetime created_at
      datetime updated_at
    }
    DRIVER {
      uuid id PK
      string name
      string license_number
      string employee_code
      string contact_details
      boolean is_active
      datetime created_at
      datetime updated_at
    }
    CUSTOMER {
      uuid id PK
      string name
      string contact_details
      boolean is_active
      datetime created_at
      datetime updated_at
    }
    SHIPMENT {
      uuid id PK
      string reference UK
      string origin
      string destination
      datetime planned_pickup_at
      datetime planned_delivery_at
      datetime actual_pickup_at
      datetime actual_delivery_at
      uuid customer_id FK
      decimal estimated_load
      string notes
      enum status
      uuid vehicle_id FK
      uuid driver_id FK
      datetime created_at
      datetime updated_at
    }
    SHIPMENT_EVENT {
      uuid id PK
      uuid shipment_id FK
      enum event_type
      datetime occurred_at
      string location
      string notes
      uuid recorded_by_user_id FK
      datetime created_at
    }
```

## Entidades y decisiones

- `AppUser`: usuario interno con rol `Admin` u `Operator`. La contraseña solo se guarda como hash. `IsActive` aplica la baja lógica exigida por RNF-03.
- `Vehicle`, `Driver` y `Customer`: catálogos con baja lógica. Las unicidades funcionales solo afectan a registros activos y se reforzarán en servicio y base de datos al implementar cada catálogo.
- `Shipment`: agregado operativo con referencia única global y estado `Planned`, `InProgress`, `Delivered` o `Cancelled`. No usa `IsActive`: su ciclo se expresa mediante el estado. Cliente, carga estimada, vehículo, conductor y entrega prevista son opcionales; sus claves foráneas usan `RESTRICT` para conservar relaciones históricas aunque el catálogo se desactive. Las fechas de negocio se normalizan y persisten en UTC; la recogida y entrega reales se sellan automáticamente al entrar en `InProgress` y `Delivered`.
- `ShipmentEvent`: historial inmutable del envío, ordenado por `OccurredAt`, siempre vinculado al usuario que lo registró. Tipos previstos: creación, asignación, salida, punto de control, incidencia, entrega y cancelación.

## Restricciones principales

- La entrega prevista no puede preceder a la recogida (RN-06).
- Solo recursos activos pueden participar en nuevas asignaciones (RN-03), sin doble reserva en envíos no terminados (RN-04).
- RN-04 se comprueba en el servicio mediante una consulta sobre otros envíos `Planned` o `InProgress`, excluyendo el envío actual. No se usa un índice único filtrado: la regla es operativa y puede evolucionar, y el volumen previsto no justifica convertirla todavía en una restricción PostgreSQL. La ventana entre comprobación y escritura bajo concurrencia simultánea queda registrada para revisión en el Sprint 7.
- La capacidad insuficiente genera aviso, no bloqueo (RN-05).
- El envío solo pasa a curso con vehículo y conductor, y un estado terminal no se revierte (RN-07/RN-08).
- Las bajas son lógicas: no se aplican borrados en cascada sobre el historial (RN-15/RNF-03).
