BEGIN;

DELETE FROM transport
WHERE reference LIKE 'TR-SMK-%';

DELETE FROM vehicle
WHERE internal_code LIKE 'VEH-SMK-%';

DELETE FROM driver
WHERE employee_code LIKE 'DRV-SMK-%'
   OR email LIKE 'smoke.%@transitops.local';

COMMIT;
