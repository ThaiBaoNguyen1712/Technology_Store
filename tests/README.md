# Tests

## Layout

- `tests/Tech_Store.UnitTests`: unit test suite for services and business logic.
- `tests/Tech_Store.SecurityTests`: security-oriented test suite that invokes SonarQube and OWASP ZAP runners.
- `tests/security`: PowerShell runners used by the security suite and by manual execution.

## Run unit tests

```powershell
dotnet test .\tests\Tech_Store.UnitTests\Tech_Store.UnitTests.csproj
```

## Run SonarQube analysis

Required environment variables:

- `SONAR_HOST_URL`
- `SONAR_TOKEN`

Optional environment variables:

- `SONAR_PROJECT_KEY`
- `SONAR_PROJECT_NAME`
- `SONAR_SCANNER_VERSION`

Run via test:

```powershell
dotnet test .\tests\Tech_Store.SecurityTests\Tech_Store.SecurityTests.csproj --filter FullyQualifiedName~SonarQube
```

Run manually:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\security\run-sonarqube-analysis.ps1 -SonarHostUrl $env:SONAR_HOST_URL -SonarToken $env:SONAR_TOKEN
```

## Run OWASP ZAP SQL injection scan

Required environment variables:

- `ZAP_TARGET_URL`

Optional environment variables:

- `ZAP_DOCKER_IMAGE`
- `ZAP_MAX_MINUTES`

Use a URL reachable from Docker. If the app runs on the host machine, prefer `http://host.docker.internal:8080` instead of `http://localhost:8080`.

Run via test:

```powershell
dotnet test .\tests\Tech_Store.SecurityTests\Tech_Store.SecurityTests.csproj --filter FullyQualifiedName~Zap
```

Run manually:

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\security\run-zap-sqli-scan.ps1 -TargetUrl $env:ZAP_TARGET_URL
```

## Evidence

Both security runners write logs and artifacts under `artifacts/security/...`. The xUnit wrappers also capture full console output into `console.log` so it can be attached as scan evidence.
