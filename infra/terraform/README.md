# Terraform Foundation

This directory holds the Terraform codebase for TransitOps.

The cloud architecture, naming/tagging rules, environment conventions, and remote-state bootstrap path are documented together in [../../docs/CloudArchitecture.md](../../docs/CloudArchitecture.md).

## Layout

- `bootstrap/remote_state/`: one-time local-state bootstrap root that creates the shared S3 bucket and DynamoDB lock table for Terraform remote state.
- `modules/platform_foundation/`: shared naming, tagging, environment, and configuration conventions reused by environment roots.
- `environments/dev/`: root Terraform configuration for the `dev` environment.
- `environments/prod/`: root Terraform configuration for the future `prod` environment.

## Intent of This First Slice

The current Terraform baseline is split into two responsibilities:

- `bootstrap/remote_state/` creates the remote-state backend resources with local state.
- `environments/dev` and `environments/prod` consume the shared conventions and are prepared to use that remote backend.

At this stage, only the remote-state bootstrap root creates AWS resources. The deployable environment roots still do not create network or runtime resources yet.

It now establishes:

- Terraform repository structure
- provider and Terraform version pinning
- shared locals, variables, and outputs
- environment layout for `dev` and `prod`
- remote Terraform state strategy with S3 backend configuration shape, encryption, versioning, and DynamoDB locking bootstrap

## Expected Next Steps

The next slices will add:

- VPC and subnet resources
- route tables and internet/egress path
- least-privilege security groups

## Basic Commands

From the remote-state bootstrap directory:

```powershell
cd infra\terraform\bootstrap\remote_state
terraform init
terraform fmt -recursive
terraform validate
terraform plan
```

From an environment directory, the intended commands will be:

```powershell
terraform init
terraform fmt -recursive
terraform validate
```

As of April 11, 2026, Terraform CLI is installed locally and `terraform validate` passes for `bootstrap/remote_state`, `environments/dev`, and `environments/prod` with backend initialization disabled.
