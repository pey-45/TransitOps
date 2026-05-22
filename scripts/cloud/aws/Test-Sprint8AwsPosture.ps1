param(
    [string]$Profile = "aws-pey-v1",
    [string]$Region = "eu-west-1"
)

$ErrorActionPreference = "Stop"

function Invoke-AwsJson {
    param([string[]]$Arguments)

    $output = & aws @Arguments --profile $Profile --region $Region --output json 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI failed: aws $($Arguments -join ' ')"
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    return $output | ConvertFrom-Json
}

function Add-Check {
    param(
        [System.Collections.Generic.List[object]]$Checks,
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )

    $Checks.Add([pscustomobject]@{
        Check   = $Name
        Passed  = $Passed
        Details = $Details
    }) | Out-Null
}

$checks = [System.Collections.Generic.List[object]]::new()

$identity = Invoke-AwsJson @("sts", "get-caller-identity")
Add-Check $checks "AWS account" ($identity.Account -eq "661000947340") "Account=$($identity.Account)"

$rds = Invoke-AwsJson @("rds", "describe-db-instances", "--db-instance-identifier", "transitops-dev-db")
$db = $rds.DBInstances[0]
Add-Check $checks "RDS private" (-not [bool]$db.PubliclyAccessible) "PubliclyAccessible=$($db.PubliclyAccessible)"
Add-Check $checks "RDS backups disabled for dev" ($db.BackupRetentionPeriod -eq 0) "BackupRetentionPeriod=$($db.BackupRetentionPeriod)"

$ecs = Invoke-AwsJson @("ecs", "describe-services", "--cluster", "transitops-dev-cluster", "--services", "transitops-dev-api-svc")
$service = $ecs.services[0]
$assignPublicIp = $service.networkConfiguration.awsvpcConfiguration.assignPublicIp
Add-Check $checks "ECS private networking" ($assignPublicIp -eq "DISABLED") "assignPublicIp=$assignPublicIp"

$albSg = Invoke-AwsJson @("ec2", "describe-security-groups", "--filters", "Name=group-name,Values=transitops-dev-alb-sg")
$ecsSg = Invoke-AwsJson @("ec2", "describe-security-groups", "--filters", "Name=group-name,Values=transitops-dev-ecs-sg")
$rdsSg = Invoke-AwsJson @("ec2", "describe-security-groups", "--filters", "Name=group-name,Values=transitops-dev-rds-sg")

$albGroup = $albSg.SecurityGroups[0]
$ecsGroup = $ecsSg.SecurityGroups[0]
$rdsGroup = $rdsSg.SecurityGroups[0]

$albIngressPorts = @($albGroup.IpPermissions | ForEach-Object { $_.FromPort }) | Sort-Object -Unique
Add-Check $checks "ALB public ports" (($albIngressPorts -contains 80) -and ($albIngressPorts -contains 443) -and ($albIngressPorts.Count -eq 2)) "Ports=$($albIngressPorts -join ',')"

$ecsIngressFromAlb = $false
foreach ($permission in $ecsGroup.IpPermissions) {
    foreach ($pair in $permission.UserIdGroupPairs) {
        if ($permission.FromPort -eq 8080 -and $pair.GroupId -eq $albGroup.GroupId) {
            $ecsIngressFromAlb = $true
        }
    }
}
Add-Check $checks "ECS ingress from ALB only" $ecsIngressFromAlb "ECS SG=$($ecsGroup.GroupId), ALB SG=$($albGroup.GroupId)"

$rdsIngressFromEcs = $false
foreach ($permission in $rdsGroup.IpPermissions) {
    foreach ($pair in $permission.UserIdGroupPairs) {
        if ($permission.FromPort -eq 5432 -and $pair.GroupId -eq $ecsGroup.GroupId) {
            $rdsIngressFromEcs = $true
        }
    }
}
Add-Check $checks "RDS ingress from ECS only" $rdsIngressFromEcs "RDS SG=$($rdsGroup.GroupId), ECS SG=$($ecsGroup.GroupId)"

$taskRolePolicies = Invoke-AwsJson @("iam", "list-role-policies", "--role-name", "transitops-dev-api-task-role")
Add-Check $checks "ECS task role has no inline policies" ($taskRolePolicies.PolicyNames.Count -eq 0) "InlinePolicies=$($taskRolePolicies.PolicyNames -join ',')"

$executionPolicy = Invoke-AwsJson @("iam", "get-role-policy", "--role-name", "transitops-dev-api-execution-role", "--policy-name", "transitops-dev-api-runtime-config")
$executionPolicyJson = $executionPolicy.PolicyDocument | ConvertTo-Json -Depth 20
Add-Check $checks "Execution role scoped runtime config" (-not ($executionPolicyJson -match '"Resource"\s*:\s*"\*"')) "Runtime config policy does not use Resource=*"

$deployPolicy = Invoke-AwsJson @("iam", "get-role-policy", "--role-name", "transitops-dev-github-actions-deploy-role", "--policy-name", "transitops-dev-github-actions-deploy")
$deployActions = @($deployPolicy.PolicyDocument.Statement | ForEach-Object { $_.Action } | ForEach-Object { $_ })
$deployHasWildcardActions = @($deployActions | Where-Object { $_ -like "*:*" }).Count -gt 0
Add-Check $checks "GitHub deploy role broad permissions documented" $deployHasWildcardActions "Expected warning: Terraform deploy role uses broad service permissions in dev"

$secrets = Invoke-AwsJson @("secretsmanager", "list-secrets", "--filters", "Key=name,Values=transitops/dev/app/")
$expectedSecrets = @(
    "transitops/dev/app/db-connection-string",
    "transitops/dev/app/jwt-signing-key",
    "transitops/dev/app/bootstrap-first-admin-token"
)
$secretNames = @($secrets.SecretList | ForEach-Object { $_.Name })
$allSecretsPresent = @($expectedSecrets | Where-Object { $secretNames -notcontains $_ }).Count -eq 0
Add-Check $checks "Runtime secrets present" $allSecretsPresent "Secrets=$($secretNames -join ',')"

$checks | Format-Table -AutoSize

$failed = @($checks | Where-Object { -not $_.Passed })
if ($failed.Count -gt 0) {
    throw "Sprint 8 AWS posture check failed: $($failed.Check -join ', ')"
}
