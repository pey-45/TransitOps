# platform_foundation

Shared Terraform baseline for TransitOps environments.

This module creates the environment network foundation.

It centralizes:

- naming conventions
- mandatory tags
- environment-specific host naming
- secret and parameter path prefixes
- VPC, public/app/data subnets, routing, NAT, and security groups
- shared outputs that runtime Terraform modules consume
