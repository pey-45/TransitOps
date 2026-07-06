param(
    [string]$Profile = "aws-pey-v1",
    [string]$Region = "eu-west-1"
)

$ErrorActionPreference = "Stop"

function Invoke-AwsText {
    param([string[]]$Arguments)

    $output = & aws @Arguments --profile $Profile --region $Region --output text 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $output
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

$clusters = Invoke-AwsText @("ecs", "list-clusters", "--query", "clusterArns[?contains(@, 'transitops-dev')]")
Add-Check $checks "No ECS dev cluster" ([string]::IsNullOrWhiteSpace($clusters) -or $clusters -eq "None") $clusters

$db = Invoke-AwsText @("rds", "describe-db-instances", "--db-instance-identifier", "transitops-dev-db", "--query", "DBInstances[0].DBInstanceIdentifier")
Add-Check $checks "No RDS dev database" ([string]::IsNullOrWhiteSpace($db) -or $db -eq "None") $db

$restoreDb = Invoke-AwsText @("rds", "describe-db-instances", "--db-instance-identifier", "transitops-dev-db-restore-sprint7", "--query", "DBInstances[0].DBInstanceIdentifier")
Add-Check $checks "No temporary restore database" ([string]::IsNullOrWhiteSpace($restoreDb) -or $restoreDb -eq "None") $restoreDb

$repo = Invoke-AwsText @("ecr", "describe-repositories", "--repository-names", "transitops/api", "--query", "repositories[0].repositoryName")
Add-Check $checks "No ECR dev repository" ([string]::IsNullOrWhiteSpace($repo) -or $repo -eq "None") $repo

$logGroup = Invoke-AwsText @("logs", "describe-log-groups", "--log-group-name-prefix", "/aws/ecs/transitops/dev/api", "--query", "logGroups[?logGroupName=='/aws/ecs/transitops/dev/api'].logGroupName | [0]")
Add-Check $checks "No API log group" ([string]::IsNullOrWhiteSpace($logGroup) -or $logGroup -eq "None") $logGroup

$dashboard = Invoke-AwsText @("cloudwatch", "list-dashboards", "--dashboard-name-prefix", "transitops-dev-api", "--query", "DashboardEntries[?DashboardName=='transitops-dev-api'].DashboardName | [0]")
Add-Check $checks "No CloudWatch dashboard" ([string]::IsNullOrWhiteSpace($dashboard) -or $dashboard -eq "None") $dashboard

$alarms = Invoke-AwsText @("cloudwatch", "describe-alarms", "--alarm-name-prefix", "transitops-dev-api", "--query", "MetricAlarms[].AlarmName")
Add-Check $checks "No CloudWatch alarms" ([string]::IsNullOrWhiteSpace($alarms) -or $alarms -eq "None") $alarms

$certs = Invoke-AwsText @("acm", "list-certificates", "--query", "CertificateSummaryList[?DomainName=='api.dev.transitops.net'].CertificateArn | [0]")
Add-Check $checks "No ACM API certificate" ([string]::IsNullOrWhiteSpace($certs) -or $certs -eq "None") $certs

$secrets = Invoke-AwsText @("secretsmanager", "list-secrets", "--include-planned-deletion", "--filters", "Key=name,Values=transitops/dev/app/", "--query", "SecretList[].Name")
Add-Check $checks "No runtime secrets" ([string]::IsNullOrWhiteSpace($secrets) -or $secrets -eq "None") $secrets

$snapshots = Invoke-AwsText @("rds", "describe-db-snapshots", "--snapshot-type", "manual", "--query", "DBSnapshots[?contains(DBSnapshotIdentifier, 'transitops-dev')].DBSnapshotIdentifier")
Add-Check $checks "No manual dev snapshots" ([string]::IsNullOrWhiteSpace($snapshots) -or $snapshots -eq "None") $snapshots

$lbs = Invoke-AwsText @("elbv2", "describe-load-balancers", "--query", "LoadBalancers[?contains(LoadBalancerName, 'transitops-dev')].LoadBalancerName")
Add-Check $checks "No dev ALB" ([string]::IsNullOrWhiteSpace($lbs) -or $lbs -eq "None") $lbs

$routeRecords = Invoke-AwsText @("route53", "list-resource-record-sets", "--hosted-zone-id", "Z0844787W37HXN9FIJR", "--query", "ResourceRecordSets[?contains(Name, 'api.dev.transitops.net')].Name")
Add-Check $checks "No api.dev Route53 records" ([string]::IsNullOrWhiteSpace($routeRecords) -or $routeRecords -eq "None") $routeRecords

$checks | Format-Table -AutoSize

$failed = @($checks | Where-Object { -not $_.Passed })
if ($failed.Count -gt 0) {
    throw "Sprint 8 destroy audit failed: $($failed.Check -join ', ')"
}
