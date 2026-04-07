# Terraform Foundation

This directory holds the Terraform codebase for TransitOps.

## Layout

- `modules/platform_foundation/`: shared naming, tagging, environment, and configuration conventions reused by environment roots.
- `environments/dev/`: root Terraform configuration for the `dev` environment.
- `environments/prod/`: root Terraform configuration for the future `prod` environment.

## Intent of This First Slice

This first Sprint 2 slice does not create AWS resources yet.

It establishes:

- Terraform repository structure
- provider and Terraform version pinning
- shared locals, variables, and outputs
- environment layout for `dev` and `prod`

## Expected Next Steps

The next slices will add:

- remote Terraform state
- VPC and subnet resources
- route tables and internet/egress path
- least-privilege security groups

## Basic Commands

From an environment directory, the intended commands will be:

```powershell
terraform init
terraform fmt -recursive
terraform validate
```

Terraform CLI is not yet installed in this workspace, so those commands are not validated from this repository yet.
