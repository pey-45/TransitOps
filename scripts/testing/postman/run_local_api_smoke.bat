@echo off
setlocal

if /i not "%~1"=="--hold" (
    start "TransitOps API Local Smoke" cmd /k ""%~f0" --hold"
    exit /b 0
)

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\..\.."
set "COLLECTION_FILE=%SCRIPT_DIR%collections\TransitOps.Api.smoke.postman_collection.json"
set "ENVIRONMENT_FILE=%SCRIPT_DIR%environments\TransitOps.Api.local.postman_environment.json"
set "CLEANUP_SQL_FILE=%SCRIPT_DIR%sql\cleanup_runtime_smoke_data.sql"
set "EXIT_CODE=1"

if not exist "%COLLECTION_FILE%" (
    echo Collection file not found: "%COLLECTION_FILE%"
    exit /b 1
)

if not exist "%ENVIRONMENT_FILE%" (
    echo Environment file not found: "%ENVIRONMENT_FILE%"
    exit /b 1
)

if not exist "%CLEANUP_SQL_FILE%" (
    echo Cleanup SQL file not found: "%CLEANUP_SQL_FILE%"
    exit /b 1
)

where docker >nul 2>&1
if errorlevel 1 (
    echo Docker is required to run the local smoke test.
    exit /b 1
)

where node >nul 2>&1
if errorlevel 1 (
    echo Node.js is required to run the Postman/Newman smoke test.
    exit /b 1
)

pushd "%REPO_ROOT%"
if errorlevel 1 (
    echo Could not access repository root.
    exit /b 1
)

if not defined TRANSITOPS_JWT_SIGNING_KEY (
    for /f %%i in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "[Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')"') do set "TRANSITOPS_JWT_SIGNING_KEY=smoke-%%i"
)
if not defined TRANSITOPS_BOOTSTRAP_ADMIN_TOKEN (
    set "TRANSITOPS_BOOTSTRAP_ADMIN_TOKEN="
)

echo Starting local Docker services...
docker compose up -d --build db api
if errorlevel 1 goto :fail

echo Waiting for API readiness...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$deadline = (Get-Date).AddMinutes(2); $uri = 'http://localhost:8080/api/v1/health/ready'; do { try { $response = Invoke-RestMethod -Method Get -Uri $uri -TimeoutSec 5; if ($response.data.status -eq 'ready' -and $response.data.databaseConnectionAvailable -eq $true) { exit 0 } } catch { } Start-Sleep -Seconds 2 } while ((Get-Date) -lt $deadline); exit 1"
if errorlevel 1 (
    echo API did not become ready within the timeout window.
    goto :fail
)

echo Removing any leftover runtime smoke data...
call :cleanup_runtime_data
if errorlevel 1 goto :fail

echo Resetting deterministic seed data...
call "%REPO_ROOT%\scripts\database\postgres\seed\003_delete_seed_sample_data.bat"
if errorlevel 1 goto :fail

call "%REPO_ROOT%\scripts\database\postgres\seed\002_seed_sample_data.bat"
if errorlevel 1 goto :fail

where newman >nul 2>&1
if not errorlevel 1 (
    set "NEWMAN_CMD=newman"
    goto :run
)

where npx >nul 2>&1
if errorlevel 1 (
    echo Newman is not installed and npx is not available.
    goto :fail
)

echo Newman was not found globally. Falling back to npx newman@6...
set "NEWMAN_CMD=npx --yes newman@6"

:run
echo Running Postman smoke collection...
call %NEWMAN_CMD% run "%COLLECTION_FILE%" -e "%ENVIRONMENT_FILE%" --bail --reporters cli
set "EXIT_CODE=%ERRORLEVEL%"
goto :cleanup_and_exit

:fail
set "EXIT_CODE=%ERRORLEVEL%"
if "%EXIT_CODE%"=="" set "EXIT_CODE=1"

:cleanup_and_exit
echo Cleaning data generated during this execution...
call :cleanup_runtime_data
if errorlevel 1 (
    echo Runtime smoke cleanup failed.
    set "EXIT_CODE=1"
)

call "%REPO_ROOT%\scripts\database\postgres\seed\003_delete_seed_sample_data.bat"
if errorlevel 1 (
    echo Seed cleanup failed.
    set "EXIT_CODE=1"
)

if "%EXIT_CODE%"=="0" (
    echo Smoke test completed successfully and left no generated data behind.
) else (
    echo Smoke test finished with errors. Cleanup was still attempted.
)

popd
exit /b %EXIT_CODE%

:cleanup_runtime_data
type "%CLEANUP_SQL_FILE%" | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U transitops -d transitops
exit /b %ERRORLEVEL%
