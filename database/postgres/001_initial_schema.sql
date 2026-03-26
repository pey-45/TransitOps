-- =========================================================
-- PostgreSQL schema for revised model:
-- AppUser, Driver, Vehicle, Transport, ShipmentEvent
-- =========================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =========================================================
-- ENUMS
-- =========================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'transport_status') THEN
        CREATE TYPE transport_status AS ENUM (
            'planned',
            'in_transit',
            'delivered',
            'cancelled'
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'shipment_event_type') THEN
        CREATE TYPE shipment_event_type AS ENUM (
            'created',
            'assigned',
            'departed',
            'checkpoint',
            'incident',
            'delivered',
            'cancelled'
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'user_role') THEN
        CREATE TYPE user_role AS ENUM (
            'admin',
            'operator'
        );
    END IF;
END
$$;

-- =========================================================
-- TABLE: app_user
-- =========================================================

CREATE TABLE IF NOT EXISTS app_user
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    password_hash TEXT NOT NULL,
    user_role user_role NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP NULL
);

-- =========================================================
-- TABLE: driver
-- =========================================================

CREATE TABLE IF NOT EXISTS driver
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_code VARCHAR(100) NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(150) NOT NULL,
    license_number VARCHAR(100) NOT NULL,
    license_expiry_date DATE NULL,
    phone VARCHAR(50) NULL,
    email VARCHAR(255) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP NULL
);

-- =========================================================
-- TABLE: vehicle
-- =========================================================

CREATE TABLE IF NOT EXISTS vehicle
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plate_number VARCHAR(50) NOT NULL,
    internal_code VARCHAR(100) NULL,
    brand VARCHAR(100) NULL,
    model VARCHAR(100) NULL,
    capacity_kg NUMERIC(12,2) NULL,
    capacity_m3 NUMERIC(12,2) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP NULL,

    CONSTRAINT ck_vehicle_capacity_kg_non_negative
        CHECK (capacity_kg IS NULL OR capacity_kg >= 0),
    CONSTRAINT ck_vehicle_capacity_m3_non_negative
        CHECK (capacity_m3 IS NULL OR capacity_m3 >= 0)
);

-- =========================================================
-- TABLE: transport
-- =========================================================

CREATE TABLE IF NOT EXISTS transport
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference VARCHAR(100) NOT NULL,
    description TEXT NULL,
    origin VARCHAR(255) NOT NULL,
    destination VARCHAR(255) NOT NULL,
    planned_pickup_at TIMESTAMP NOT NULL,
    planned_delivery_at TIMESTAMP NULL,
    actual_pickup_at TIMESTAMP NULL,
    actual_delivery_at TIMESTAMP NULL,
    status transport_status NOT NULL,
    vehicle_id UUID NULL,
    driver_id UUID NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP NULL,

    CONSTRAINT fk_transport_vehicle
        FOREIGN KEY (vehicle_id)
        REFERENCES vehicle(id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT fk_transport_driver
        FOREIGN KEY (driver_id)
        REFERENCES driver(id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,

    CONSTRAINT ck_transport_planned_dates
        CHECK (
            planned_delivery_at IS NULL
            OR planned_delivery_at >= planned_pickup_at
        ),

    CONSTRAINT ck_transport_actual_dates
        CHECK (
            actual_pickup_at IS NULL
            OR actual_delivery_at IS NULL
            OR actual_delivery_at >= actual_pickup_at
        )
);

-- =========================================================
-- TABLE: shipment_event
-- =========================================================

CREATE TABLE IF NOT EXISTS shipment_event
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transport_id UUID NOT NULL,
    created_by_user_id UUID NOT NULL,
    event_type shipment_event_type NOT NULL,
    event_date TIMESTAMP NOT NULL,
    location VARCHAR(255) NULL,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP NULL,

    CONSTRAINT fk_shipment_event_transport
        FOREIGN KEY (transport_id)
        REFERENCES transport(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_shipment_event_created_by_user
        FOREIGN KEY (created_by_user_id)
        REFERENCES app_user(id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- =========================================================
-- NAMED CONSTRAINTS SAFETY
-- =========================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_vehicle_capacity_kg_non_negative'
          AND conrelid = 'vehicle'::regclass
    ) THEN
        ALTER TABLE vehicle
            ADD CONSTRAINT ck_vehicle_capacity_kg_non_negative
            CHECK (capacity_kg IS NULL OR capacity_kg >= 0);
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_vehicle_capacity_m3_non_negative'
          AND conrelid = 'vehicle'::regclass
    ) THEN
        ALTER TABLE vehicle
            ADD CONSTRAINT ck_vehicle_capacity_m3_non_negative
            CHECK (capacity_m3 IS NULL OR capacity_m3 >= 0);
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_transport_vehicle'
          AND conrelid = 'transport'::regclass
    ) THEN
        ALTER TABLE transport
            ADD CONSTRAINT fk_transport_vehicle
            FOREIGN KEY (vehicle_id)
            REFERENCES vehicle(id)
            ON UPDATE CASCADE
            ON DELETE SET NULL;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_transport_driver'
          AND conrelid = 'transport'::regclass
    ) THEN
        ALTER TABLE transport
            ADD CONSTRAINT fk_transport_driver
            FOREIGN KEY (driver_id)
            REFERENCES driver(id)
            ON UPDATE CASCADE
            ON DELETE SET NULL;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_transport_planned_dates'
          AND conrelid = 'transport'::regclass
    ) THEN
        ALTER TABLE transport
            ADD CONSTRAINT ck_transport_planned_dates
            CHECK (
                planned_delivery_at IS NULL
                OR planned_delivery_at >= planned_pickup_at
            );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_transport_actual_dates'
          AND conrelid = 'transport'::regclass
    ) THEN
        ALTER TABLE transport
            ADD CONSTRAINT ck_transport_actual_dates
            CHECK (
                actual_pickup_at IS NULL
                OR actual_delivery_at IS NULL
                OR actual_delivery_at >= actual_pickup_at
            );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_shipment_event_transport'
          AND conrelid = 'shipment_event'::regclass
    ) THEN
        ALTER TABLE shipment_event
            ADD CONSTRAINT fk_shipment_event_transport
            FOREIGN KEY (transport_id)
            REFERENCES transport(id)
            ON UPDATE CASCADE
            ON DELETE RESTRICT;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_shipment_event_created_by_user'
          AND conrelid = 'shipment_event'::regclass
    ) THEN
        ALTER TABLE shipment_event
            ADD CONSTRAINT fk_shipment_event_created_by_user
            FOREIGN KEY (created_by_user_id)
            REFERENCES app_user(id)
            ON UPDATE CASCADE
            ON DELETE RESTRICT;
    END IF;
END
$$;

-- =========================================================
-- INDEXES
-- =========================================================

CREATE UNIQUE INDEX IF NOT EXISTS ux_app_user_username_active
    ON app_user(username)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_app_user_email_active
    ON app_user(email)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_driver_license_number_active
    ON driver(license_number)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_driver_employee_code_active
    ON driver(employee_code)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_driver_email_active
    ON driver(email)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicle_plate_number_active
    ON vehicle(plate_number)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicle_internal_code_active
    ON vehicle(internal_code)
    WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_transport_reference_active
    ON transport(reference)
    WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_transport_vehicle_id
    ON transport(vehicle_id);

CREATE INDEX IF NOT EXISTS ix_transport_driver_id
    ON transport(driver_id);

CREATE INDEX IF NOT EXISTS ix_transport_status
    ON transport(status);

CREATE INDEX IF NOT EXISTS ix_transport_deleted_at
    ON transport(deleted_at);

CREATE INDEX IF NOT EXISTS ix_shipment_event_transport_id
    ON shipment_event(transport_id);

CREATE INDEX IF NOT EXISTS ix_shipment_event_created_by_user_id
    ON shipment_event(created_by_user_id);

CREATE INDEX IF NOT EXISTS ix_shipment_event_date
    ON shipment_event(event_date);

CREATE INDEX IF NOT EXISTS ix_shipment_event_deleted_at
    ON shipment_event(deleted_at);

CREATE INDEX IF NOT EXISTS ix_vehicle_deleted_at
    ON vehicle(deleted_at);

CREATE INDEX IF NOT EXISTS ix_driver_deleted_at
    ON driver(deleted_at);

CREATE INDEX IF NOT EXISTS ix_app_user_deleted_at
    ON app_user(deleted_at);

-- =========================================================
-- updated_at trigger
-- =========================================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_set_updated_at_app_user ON app_user;
CREATE TRIGGER trg_set_updated_at_app_user
BEFORE UPDATE ON app_user
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_set_updated_at_driver ON driver;
CREATE TRIGGER trg_set_updated_at_driver
BEFORE UPDATE ON driver
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_set_updated_at_vehicle ON vehicle;
CREATE TRIGGER trg_set_updated_at_vehicle
BEFORE UPDATE ON vehicle
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_set_updated_at_transport ON transport;
CREATE TRIGGER trg_set_updated_at_transport
BEFORE UPDATE ON transport
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

COMMIT;
