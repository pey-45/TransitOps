# Remote State Bootstrap

This root configuration bootstraps the shared Terraform remote-state resources for TransitOps.

## Why This Root Exists

Terraform cannot store state in an S3 backend until the S3 bucket and DynamoDB lock table already exist.

Because of that, this root is intentionally executed with local state first. Its only job is to create:

- the S3 bucket for Terraform state
- the DynamoDB table for Terraform state locking

After those resources exist, `infra/terraform/environments/dev/` and `infra/terraform/environments/prod/` can be initialized against the remote backend.

## Managed Resources

- S3 bucket with:
  - encryption enabled
  - versioning enabled
  - public access blocked
- DynamoDB table with:
  - `LockID` hash key
  - server-side encryption enabled
  - `PAY_PER_REQUEST` billing

## Execution Model

Run this root first with local state:

```powershell
cd infra\terraform\bootstrap\remote_state
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply
```

Then use the outputs to fill:

- `infra/terraform/environments/dev/backend.hcl`
- `infra/terraform/environments/prod/backend.hcl`

and reinitialize the environment roots with the S3 backend.
