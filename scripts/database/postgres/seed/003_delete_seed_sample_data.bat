@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%..\..\..\.."
set "SQL_FILE=%SCRIPT_DIR%003_delete_seed_sample_data.sql"

pushd "%REPO_ROOT%" >nul || (
    echo Failed to resolve the repository root.
    exit /b 1
)

where docker >nul 2>nul || (
    echo docker was not found in PATH.
    popd >nul
    exit /b 1
)

echo Ensuring the PostgreSQL container is running...
docker compose up -d db
if errorlevel 1 (
    echo Failed to start the db service with docker compose.
    popd >nul
    exit /b 1
)

echo Running %~nx0 against the local transitops database...
docker compose exec -T db psql -v ON_ERROR_STOP=1 -U transitops -d transitops < "%SQL_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo SQL execution failed.
) else (
    echo Seed sample data deleted successfully.
)

popd >nul
exit /b %EXIT_CODE%
