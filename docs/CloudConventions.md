# TransitOps · Cloud Conventions

## Purpose

Define the shared naming, tagging, environment, and configuration conventions that all Terraform, GitHub Actions, and AWS runtime work must follow.

## Fixed Project Constants

- Project slug: `transitops`
- Service slug: `api`
- Default AWS region: `eu-west-1`
- Mandatory first deployed environment: `dev`
- Reserved future long-lived environment: `prod`

No extra long-lived environments such as `qa`, `uat`, or `stage` should be introduced unless there is a concrete reason and the documentation is updated.

## Naming Rules

### General Rules

- Use lowercase only.
- Use hyphen-separated names for AWS resources where the service allows it.
- Do not use spaces, camelCase, or underscores in resource names.
- Put the project slug first and the environment second.
- Keep names short, predictable, and reusable across Terraform modules.
- Use the same canonical name in the AWS `Name` tag whenever the resource supports tags.

### Canonical Pattern

The default resource pattern is:

`<project>-<environment>-<component>`

Examples:

- `transitops-dev-vpc`
- `transitops-dev-alb`
- `transitops-dev-api-svc`
- `transitops-prod-db`

### Approved Component Abbreviations

Use these abbreviations consistently instead of inventing new ones:

- `alb`
- `api`
- `app`
- `cluster`
- `db`
- `ecr`
- `ecs`
- `igw`
- `nat`
- `rds`
- `rt`
- `sg`
- `subnet`
- `tg`
- `vpc`

### Resource Naming Reference

| Resource | Convention | Example |
| --- | --- | --- |
| VPC | `<project>-<env>-vpc` | `transitops-dev-vpc` |
| Public subnet | `<project>-<env>-public-<az>` | `transitops-dev-public-a` |
| Private app subnet | `<project>-<env>-app-<az>` | `transitops-dev-app-a` |
| Private data subnet | `<project>-<env>-data-<az>` | `transitops-dev-data-a` |
| ALB | `<project>-<env>-alb` | `transitops-dev-alb` |
| ALB security group | `<project>-<env>-alb-sg` | `transitops-dev-alb-sg` |
| ECS security group | `<project>-<env>-ecs-sg` | `transitops-dev-ecs-sg` |
| RDS security group | `<project>-<env>-rds-sg` | `transitops-dev-rds-sg` |
| ECS cluster | `<project>-<env>-cluster` | `transitops-dev-cluster` |
| ECS service | `<project>-<env>-api-svc` | `transitops-dev-api-svc` |
| Task definition family | `<project>-<env>-api` | `transitops-dev-api` |
| Target group | `<project>-<env>-api-tg` | `transitops-dev-api-tg` |
| RDS instance identifier | `<project>-<env>-db` | `transitops-dev-db` |
| DB subnet group | `<project>-<env>-db-subnets` | `transitops-dev-db-subnets` |
| DB parameter group | `<project>-<env>-postgres16` | `transitops-dev-postgres16` |
| ECR repository | `<project>/api` | `transitops/api` |
| CloudWatch log group | `/aws/ecs/<project>/<env>/api` | `/aws/ecs/transitops/dev/api` |
| Secrets Manager app secret | `<project>/<env>/app/<name>` | `transitops/dev/app/jwt-signing-key` |
| Parameter Store app parameter | `/<project>/<env>/app/<name>` | `/transitops/dev/app/jwt-issuer` |
| Terraform state bucket | `<project>-tfstate-<account-id>-<region>` | `transitops-tfstate-123456789012-eu-west-1` |
| Terraform lock table | `<project>-tfstate-locks` | `transitops-tfstate-locks` |

Global resources that must be globally unique, such as the Terraform state bucket, may include account and region suffixes even when the normal pattern does not.

## Tagging Rules

### Mandatory Tags

Every taggable AWS resource created for TransitOps must carry these tags:

| Tag | Example | Purpose |
| --- | --- | --- |
| `Name` | `transitops-dev-alb` | Human-readable primary identifier |
| `Project` | `TransitOps` | Group resources by project |
| `Environment` | `dev` | Distinguish long-lived environments |
| `ManagedBy` | `Terraform` | Make ownership and update path explicit |
| `Owner` | `pey` | Human owner reference |
| `Repository` | `pey-45/TransitOps` | Tie cloud resources back to source control |
| `Service` | `api` or `platform` | Distinguish application vs platform resources |

### Tagging Rules of Use

- `Environment` values are limited to `dev` and `prod` unless a new environment is explicitly introduced.
- `ManagedBy` should remain `Terraform` for all infrastructure created by the repository.
- `Service=api` is used for application-serving resources.
- `Service=platform` is used for shared platform resources such as the VPC or Terraform state components.
- The `Name` tag should match the canonical resource name exactly whenever the AWS service supports it.

Terraform should define these tags once as shared locals and merge them into all resources by default.

## Environment Conventions

### Supported Environments

- `dev` is the first and only mandatory deployed environment in the current phase.
- `prod` is reserved for later use, but the naming and state conventions must already support it.

### Isolation Rules

Each long-lived environment must have its own:

- Terraform state key;
- VPC;
- ECS service;
- RDS instance;
- runtime secrets and configuration namespace;
- DNS record;
- CloudWatch log group namespace.

Do not share mutable runtime resources such as databases or secret namespaces across environments.

### AWS Account and Region Posture

- A single AWS account is acceptable for the first real deployment if the environment boundaries above are respected.
- The default region for all work is `eu-west-1`.
- If later work introduces a second account or second region, the naming and tagging rules above remain in force.

### DNS Conventions

Use these host naming patterns:

- `api.dev.<root-domain>` for the dev API
- `api.<root-domain>` for the prod API

### Deployment Conventions

- Until `prod` exists, GitHub Actions should only deploy to `dev`.
- Deployed AWS environments should run with `ASPNETCORE_ENVIRONMENT=Production`, even for `dev`.
- Environment identity is expressed through AWS naming, tags, DNS, and runtime configuration, not by switching the app into ASP.NET `Development` mode.

## Configuration Conventions

### General Rules

- Every runtime setting must be externalized.
- No secret may be committed to the repository.
- Non-secret values may be injected directly from Terraform or read from Parameter Store.
- Secret values must come from Secrets Manager unless there is a strong reason to use another secure store.
- The container receives .NET configuration keys through environment variables with double underscores.

### Runtime Key Naming

Keep the .NET configuration keys exactly aligned with application code. Examples:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Jwt__ExpirationMinutes`
- `Bootstrap__FirstAdminToken`

Do not invent AWS-specific app key names when the existing .NET key already expresses the setting clearly.

### Recommended Source of Truth by Setting Type

| Setting Type | Preferred Store | Example |
| --- | --- | --- |
| Database connection string | Secrets Manager | `ConnectionStrings__DefaultConnection` |
| JWT signing key | Secrets Manager | `Jwt__SigningKey` |
| One-time bootstrap token | Secrets Manager | `Bootstrap__FirstAdminToken` |
| JWT issuer/audience | Parameter Store or Terraform env vars | `Jwt__Issuer`, `Jwt__Audience` |
| Non-secret operational toggles | Parameter Store or Terraform env vars | health/deployment settings |

### Secret and Parameter Paths

Use these path shapes:

- Secrets Manager:
  - `transitops/<env>/app/jwt-signing-key`
  - `transitops/<env>/app/bootstrap-first-admin-token`
  - `transitops/<env>/app/db-connection-string`
- Parameter Store:
  - `/transitops/<env>/app/jwt-issuer`
  - `/transitops/<env>/app/jwt-audience`
  - `/transitops/<env>/app/jwt-expiration-minutes`

Terraform is responsible for mapping those values into ECS task-definition secrets or environment variables with the exact .NET key names expected by the application.

### Migration and Bootstrap Conventions

- In AWS, EF Core migrations should not depend on application startup side effects.
- The later deployment path should run migrations explicitly as a deployment step or dedicated task.
- The first-admin bootstrap token should be stored as a secret, used only for the bootstrap phase, and then rotated or removed.

### CI/CD Credential Convention

- GitHub Actions must authenticate to AWS using OIDC role assumption.
- Long-lived AWS access keys must not be stored as repository secrets for the intended steady-state deployment path.

## What These Conventions Intentionally Forbid

- environment-specific ad hoc names chosen per resource;
- storing secrets in `appsettings.*.json`, Terraform variables files committed to git, or GitHub plaintext variables;
- mixing `dev` and `prod` resources behind shared mutable state without explicit isolation;
- using ASP.NET `Development` mode in deployed AWS environments;
- building Terraform modules that hardcode one-off names outside these conventions.
