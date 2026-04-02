-- =========================================================
-- Manual seed data for the current TransitOps schema
-- This script inserts a deterministic sample dataset for
-- local manual testing. It is safe to rerun because it first
-- removes the same seeded rows by fixed UUID.
-- =========================================================
-- Enum mappings used by the current schema:
-- user_role: admin=0, operator=1
-- transport.status: planned=0, in_transit=1, delivered=2, cancelled=3
-- shipment_event.event_type: created=0, assigned=1, departed=2,
-- checkpoint=3, incident=4, delivered=5, cancelled=6

BEGIN;

-- Remove a previous version of this same seed set.
DELETE FROM shipment_event
WHERE id IN (
    '50000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000002',
    '50000000-0000-0000-0000-000000000003',
    '50000000-0000-0000-0000-000000000004',
    '50000000-0000-0000-0000-000000000005',
    '50000000-0000-0000-0000-000000000006',
    '50000000-0000-0000-0000-000000000007',
    '50000000-0000-0000-0000-000000000008'
);

DELETE FROM transport
WHERE id IN (
    '40000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000002',
    '40000000-0000-0000-0000-000000000003',
    '40000000-0000-0000-0000-000000000004'
);

DELETE FROM vehicle
WHERE id IN (
    '30000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000002'
);

DELETE FROM driver
WHERE id IN (
    '20000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000002'
);

DELETE FROM app_user
WHERE id IN (
    '10000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002'
);

INSERT INTO app_user (
    id,
    username,
    email,
    password_hash,
    user_role,
    is_active,
    created_at,
    updated_at,
    deleted_at
)
VALUES
(
    '10000000-0000-0000-0000-000000000001',
    'seed.admin',
    'seed.admin@transitops.local',
    '$2b$12$JYk7rJfJQ6l6C9M0u0PjMOby7Y0xjD3l4J3mFQ7nDkQnVhQ5GmN3K',
    0,
    TRUE,
    '2026-03-20 09:00:00',
    '2026-03-20 09:00:00',
    NULL
),
(
    '10000000-0000-0000-0000-000000000002',
    'seed.operator',
    'seed.operator@transitops.local',
    '$2b$12$6qM5QJQ2n2E6Fv6gV0K3mO7p4M8zX1xN4V3qM6hW7cR2tB9pL0yFe',
    1,
    TRUE,
    '2026-03-20 09:05:00',
    '2026-03-20 09:05:00',
    NULL
);

INSERT INTO driver (
    id,
    employee_code,
    first_name,
    last_name,
    license_number,
    license_expiry_date,
    phone,
    email,
    is_active,
    created_at,
    updated_at,
    deleted_at
)
VALUES
(
    '20000000-0000-0000-0000-000000000001',
    'SEED-DRV-001',
    'Lucia',
    'Martin',
    'LIC-SEED-001',
    '2028-06-30',
    '+34-600-000-001',
    'lucia.martin@transitops.local',
    TRUE,
    '2026-03-20 10:00:00',
    '2026-03-20 10:00:00',
    NULL
),
(
    '20000000-0000-0000-0000-000000000002',
    'SEED-DRV-002',
    'Carlos',
    'Ruiz',
    'LIC-SEED-002',
    '2027-11-15',
    '+34-600-000-002',
    'carlos.ruiz@transitops.local',
    TRUE,
    '2026-03-20 10:05:00',
    '2026-03-20 10:05:00',
    NULL
);

INSERT INTO vehicle (
    id,
    plate_number,
    internal_code,
    brand,
    model,
    capacity_kg,
    capacity_m3,
    is_active,
    created_at,
    updated_at,
    deleted_at
)
VALUES
(
    '30000000-0000-0000-0000-000000000001',
    'SEED-PLATE-001',
    'SEED-VEH-001',
    'Iveco',
    'Daily',
    3500.00,
    18.50,
    TRUE,
    '2026-03-20 11:00:00',
    '2026-03-20 11:00:00',
    NULL
),
(
    '30000000-0000-0000-0000-000000000002',
    'SEED-PLATE-002',
    'SEED-VEH-002',
    'Mercedes-Benz',
    'Actros',
    18000.00,
    52.00,
    TRUE,
    '2026-03-20 11:05:00',
    '2026-03-20 11:05:00',
    NULL
);

INSERT INTO transport (
    id,
    reference,
    description,
    origin,
    destination,
    planned_pickup_at,
    planned_delivery_at,
    actual_pickup_at,
    actual_delivery_at,
    status,
    vehicle_id,
    driver_id,
    created_at,
    updated_at,
    deleted_at
)
VALUES
(
    '40000000-0000-0000-0000-000000000001',
    'SEED-TR-001',
    'Planned transport for transport list testing.',
    'Madrid',
    'Barcelona',
    '2026-03-29 08:00:00',
    '2026-03-29 14:00:00',
    NULL,
    NULL,
    0,
    '30000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '2026-03-21 09:00:00',
    '2026-03-21 09:00:00',
    NULL
),
(
    '40000000-0000-0000-0000-000000000002',
    'SEED-TR-002',
    'In-transit transport with pickup already started.',
    'Valencia',
    'Sevilla',
    '2026-03-29 10:00:00',
    '2026-03-29 18:00:00',
    '2026-03-29 10:15:00',
    NULL,
    1,
    '30000000-0000-0000-0000-000000000002',
    '20000000-0000-0000-0000-000000000002',
    '2026-03-21 09:15:00',
    '2026-03-29 10:15:00',
    NULL
),
(
    '40000000-0000-0000-0000-000000000003',
    'SEED-TR-003',
    'Delivered transport with completed timestamps.',
    'Bilbao',
    'Zaragoza',
    '2026-03-28 06:30:00',
    '2026-03-28 12:30:00',
    '2026-03-28 06:35:00',
    '2026-03-28 12:10:00',
    2,
    '30000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    '2026-03-21 09:30:00',
    '2026-03-28 12:10:00',
    NULL
),
(
    '40000000-0000-0000-0000-000000000004',
    'SEED-TR-004',
    'Soft-deleted cancelled transport to validate filtering behavior.',
    'Malaga',
    'Granada',
    '2026-03-27 07:00:00',
    '2026-03-27 09:00:00',
    NULL,
    NULL,
    3,
    '30000000-0000-0000-0000-000000000002',
    '20000000-0000-0000-0000-000000000002',
    '2026-03-21 09:45:00',
    '2026-03-27 08:30:00',
    '2026-03-27 08:30:00'
);

INSERT INTO shipment_event (
    id,
    transport_id,
    created_by_user_id,
    event_type,
    event_date,
    location,
    notes,
    created_at,
    deleted_at
)
VALUES
(
    '50000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    0,
    '2026-03-21 09:00:00',
    'Madrid',
    'Seed dataset: transport created.',
    '2026-03-21 09:00:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000002',
    '40000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002',
    1,
    '2026-03-21 09:10:00',
    'Madrid',
    'Seed dataset: driver and vehicle assigned.',
    '2026-03-21 09:10:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000003',
    '40000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000001',
    0,
    '2026-03-21 09:15:00',
    'Valencia',
    'Seed dataset: transport created.',
    '2026-03-21 09:15:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000004',
    '40000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000002',
    2,
    '2026-03-29 10:15:00',
    'Valencia',
    'Seed dataset: transport departed from origin.',
    '2026-03-29 10:15:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000005',
    '40000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000002',
    3,
    '2026-03-29 13:30:00',
    'Cordoba',
    'Seed dataset: reached intermediate checkpoint.',
    '2026-03-29 13:30:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000006',
    '40000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000001',
    0,
    '2026-03-21 09:30:00',
    'Bilbao',
    'Seed dataset: transport created.',
    '2026-03-21 09:30:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000007',
    '40000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000002',
    2,
    '2026-03-28 06:35:00',
    'Bilbao',
    'Seed dataset: departed from origin.',
    '2026-03-28 06:35:00',
    NULL
),
(
    '50000000-0000-0000-0000-000000000008',
    '40000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000002',
    5,
    '2026-03-28 12:10:00',
    'Zaragoza',
    'Seed dataset: transport delivered successfully.',
    '2026-03-28 12:10:00',
    NULL
);

COMMIT;
