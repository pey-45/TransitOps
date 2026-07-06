# Database Module

Creates the RDS PostgreSQL baseline for TransitOps.

Current scope:

- DB subnet group using private data subnets.
- PostgreSQL parameter group.
- Private RDS instance with encrypted storage.
- Backup and deletion-protection settings controlled by environment variables.

Database credentials are sensitive Terraform variables and must not be committed.
