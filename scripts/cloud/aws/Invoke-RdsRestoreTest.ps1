param(
    [string]$Profile = "aws-pey-v1",
    [string]$Region = "eu-west-1",
    [string]$TerraformDirectory = "infra/terraform/environments/dev",
    [string]$SourceDbIdentifier = "transitops-dev-db",
    [string]$RestoredDbIdentifier = "transitops-dev-db-restore-sprint7",
    [string]$SnapshotIdentifier = "transitops-dev-db-sprint7-restore-test",
    [string]$TemporarySecretName = "transitops/dev/app/db-connection-string-restore-sprint7",
    [string]$TemporaryTaskFamily = "transitops-dev-api-restore-sprint7",
    [string]$ContainerName = "api",
    [string]$DatabaseUsername = $env:DATABASE_USERNAME,
    [string]$DatabasePassword = $env:DATABASE_PASSWORD,
    [switch]$KeepEvidence
)

$ErrorActionPreference = "Stop"

function Invoke-AwsJson {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    $output = & aws @Arguments --region $Region --profile $Profile --output json
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed: aws $($Arguments -join ' ')"
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    return $output | ConvertFrom-Json
}

function Invoke-AwsText {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    $output = & aws @Arguments --region $Region --profile $Profile --output text
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed: aws $($Arguments -join ' ')"
    }

    return $output
}

function Wait-Aws {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & aws @Arguments --region $Region --profile $Profile
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI wait failed: aws $($Arguments -join ' ')"
    }
}

if ([string]::IsNullOrWhiteSpace($DatabaseUsername) -or [string]::IsNullOrWhiteSpace($DatabasePassword)) {
    throw "Set DATABASE_USERNAME and DATABASE_PASSWORD environment variables, or pass -DatabaseUsername/-DatabasePassword."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$tfPath = Resolve-Path (Join-Path $repoRoot $TerraformDirectory)

Push-Location $tfPath
try {
    $tfOutput = terraform output -json | ConvertFrom-Json
}
finally {
    Pop-Location
}

$clusterName = $tfOutput.container_runtime.value.cluster_name
$taskDefinitionArn = $tfOutput.container_runtime.value.task_definition_arn
$appSubnets = @($tfOutput.network.value.app_subnet_ids)
$ecsSecurityGroup = $tfOutput.security_groups.value.ecs_id
$dbName = $tfOutput.database.value.db_name
$dbSubnetGroupName = $tfOutput.database.value.db_subnet_group_name
$rdsSecurityGroup = $tfOutput.security_groups.value.rds_id

$temporarySecretArn = $null
$temporaryTaskDefinitionArn = $null
$temporaryExecutionPolicyName = "$TemporaryTaskFamily-secret-read"
$temporaryExecutionRoleName = $null
$startedTaskArn = $null
$createdSnapshot = $false
$createdRestore = $false

try {
    Write-Host "Creating manual snapshot $SnapshotIdentifier from $SourceDbIdentifier..."
    Invoke-AwsJson @(
        "rds", "create-db-snapshot",
        "--db-instance-identifier", $SourceDbIdentifier,
        "--db-snapshot-identifier", $SnapshotIdentifier
    ) | Out-Null
    $createdSnapshot = $true

    Wait-Aws @(
        "rds", "wait", "db-snapshot-available",
        "--db-snapshot-identifier", $SnapshotIdentifier
    )

    Write-Host "Restoring temporary DB instance $RestoredDbIdentifier..."
    Invoke-AwsJson @(
        "rds", "restore-db-instance-from-db-snapshot",
        "--db-instance-identifier", $RestoredDbIdentifier,
        "--db-snapshot-identifier", $SnapshotIdentifier,
        "--db-subnet-group-name", $dbSubnetGroupName,
        "--vpc-security-group-ids", $rdsSecurityGroup,
        "--no-publicly-accessible",
        "--no-multi-az",
        "--db-instance-class", "db.t4g.micro"
    ) | Out-Null
    $createdRestore = $true

    Wait-Aws @(
        "rds", "wait", "db-instance-available",
        "--db-instance-identifier", $RestoredDbIdentifier
    )

    $restoredDb = Invoke-AwsJson @(
        "rds", "describe-db-instances",
        "--db-instance-identifier", $RestoredDbIdentifier
    )
    $endpoint = $restoredDb.DBInstances[0].Endpoint
    $connectionString = "Host=$($endpoint.Address);Port=$($endpoint.Port);Database=$dbName;Username=$DatabaseUsername;Password=$DatabasePassword;Timeout=15;Command Timeout=30;Pooling=true;Maximum Pool Size=20"

    Write-Host "Creating temporary Secrets Manager secret for restored DB connection..."
    $secret = Invoke-AwsJson @(
        "secretsmanager", "create-secret",
        "--name", $TemporarySecretName,
        "--description", "Temporary TransitOps Sprint 7 restore validation connection string.",
        "--secret-string", $connectionString
    )
    $temporarySecretArn = $secret.ARN

    Write-Host "Registering temporary ECS task definition $TemporaryTaskFamily..."
    $sourceTask = Invoke-AwsJson @(
        "ecs", "describe-task-definition",
        "--task-definition", $taskDefinitionArn
    )

    $task = $sourceTask.taskDefinition
    $temporaryExecutionRoleName = ($task.executionRoleArn -split "/")[-1]
    $temporaryExecutionPolicy = @{
        Version   = "2012-10-17"
        Statement = @(
            @{
                Sid      = "ReadSprint7RestoreSecret"
                Effect   = "Allow"
                Action   = @("secretsmanager:GetSecretValue")
                Resource = $temporarySecretArn
            }
        )
    } | ConvertTo-Json -Depth 10 -Compress

    Write-Host "Granting temporary execution-role access to restored DB secret..."
    Invoke-AwsJson @(
        "iam", "put-role-policy",
        "--role-name", $temporaryExecutionRoleName,
        "--policy-name", $temporaryExecutionPolicyName,
        "--policy-document", $temporaryExecutionPolicy
    ) | Out-Null

    $container = $task.containerDefinitions | Where-Object { $_.name -eq $ContainerName } | Select-Object -First 1
    if (-not $container) {
        throw "Container '$ContainerName' was not found in task definition $taskDefinitionArn."
    }

    foreach ($secretRef in $container.secrets) {
        if ($secretRef.name -eq "ConnectionStrings__DefaultConnection") {
            $secretRef.valueFrom = $temporarySecretArn
        }
    }

    $registerBody = [ordered]@{
        family                  = $TemporaryTaskFamily
        taskRoleArn             = $task.taskRoleArn
        executionRoleArn        = $task.executionRoleArn
        networkMode             = $task.networkMode
        containerDefinitions    = $task.containerDefinitions
        volumes                 = @($task.volumes)
        placementConstraints    = @($task.placementConstraints)
        requiresCompatibilities = @($task.requiresCompatibilities)
        cpu                     = $task.cpu
        memory                  = $task.memory
        runtimePlatform         = $task.runtimePlatform
    }

    $tempJson = New-TemporaryFile
    $registerBody | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $tempJson -Encoding utf8
    $registered = Invoke-AwsJson @(
        "ecs", "register-task-definition",
        "--cli-input-json", "file://$tempJson"
    )
    Remove-Item -LiteralPath $tempJson -Force
    $temporaryTaskDefinitionArn = $registered.taskDefinition.taskDefinitionArn

    Write-Host "Running migrate-only task against restored DB..."
    $subnetsArg = ($appSubnets -join ",")
    $networkConfiguration = "awsvpcConfiguration={subnets=[$subnetsArg],securityGroups=[$ecsSecurityGroup],assignPublicIp=DISABLED}"
    $overrides = '{"containerOverrides":[{"name":"' + $ContainerName + '","command":["--migrate-only"]}]}'

    $runTask = Invoke-AwsJson @(
        "ecs", "run-task",
        "--cluster", $clusterName,
        "--launch-type", "FARGATE",
        "--task-definition", $temporaryTaskDefinitionArn,
        "--network-configuration", $networkConfiguration,
        "--overrides", $overrides
    )
    $startedTaskArn = $runTask.tasks[0].taskArn
    if ([string]::IsNullOrWhiteSpace($startedTaskArn)) {
        throw "Restore migration ECS task was not started."
    }

    Wait-Aws @(
        "ecs", "wait", "tasks-stopped",
        "--cluster", $clusterName,
        "--tasks", $startedTaskArn
    )

    $taskResult = Invoke-AwsJson @(
        "ecs", "describe-tasks",
        "--cluster", $clusterName,
        "--tasks", $startedTaskArn
    )
    $exitCode = ($taskResult.tasks[0].containers | Where-Object { $_.name -eq $ContainerName } | Select-Object -First 1).exitCode
    Write-Host "Restore migrate-only task exit code: $exitCode"

    if ($exitCode -ne 0) {
        throw "Restore validation failed because migrate-only returned exit code $exitCode."
    }
}
finally {
    if ($KeepEvidence) {
        Write-Host "KeepEvidence set; temporary restore resources were intentionally preserved."
    }
    else {
        if ($temporaryTaskDefinitionArn) {
            Write-Host "Deregistering temporary task definition..."
            Invoke-AwsJson @("ecs", "deregister-task-definition", "--task-definition", $temporaryTaskDefinitionArn) | Out-Null
        }

        if ($temporaryExecutionRoleName) {
            Write-Host "Deleting temporary execution-role policy..."
            try {
                Invoke-AwsJson @(
                    "iam", "delete-role-policy",
                    "--role-name", $temporaryExecutionRoleName,
                    "--policy-name", $temporaryExecutionPolicyName
                ) | Out-Null
            }
            catch {
                Write-Warning "Temporary execution-role policy cleanup failed: $($_.Exception.Message)"
            }
        }

        if ($temporarySecretArn) {
            Write-Host "Deleting temporary secret..."
            Invoke-AwsJson @("secretsmanager", "delete-secret", "--secret-id", $temporarySecretArn, "--force-delete-without-recovery") | Out-Null
        }

        if ($createdRestore) {
            Write-Host "Deleting restored DB instance..."
            Invoke-AwsJson @(
                "rds", "delete-db-instance",
                "--db-instance-identifier", $RestoredDbIdentifier,
                "--skip-final-snapshot",
                "--delete-automated-backups"
            ) | Out-Null

            Wait-Aws @(
                "rds", "wait", "db-instance-deleted",
                "--db-instance-identifier", $RestoredDbIdentifier
            )
        }

        if ($createdSnapshot) {
            Write-Host "Deleting manual snapshot..."
            Invoke-AwsJson @("rds", "delete-db-snapshot", "--db-snapshot-identifier", $SnapshotIdentifier) | Out-Null
        }
    }
}
