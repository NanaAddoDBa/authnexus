[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Invoke-ComposeCommand {
  param(
    [Parameter(Mandatory)]
    [string[]]$Arguments
  )

  & docker compose @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "docker compose $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
  }
}

Invoke-ComposeCommand -Arguments @("config", "--quiet")

$expectedServices = @("postgres", "redis", "mailpit")
foreach ($service in $expectedServices) {
  $containerId = (& docker compose ps --quiet $service).Trim()
  if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) {
    throw "The $service container is not running. Start the stack with 'docker compose up -d --wait'."
  }

  $health = (& docker inspect --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" $containerId).Trim()
  if ($LASTEXITCODE -ne 0 -or $health -ne "healthy") {
    throw "The $service container is not healthy. Current state: $health"
  }
}

Invoke-ComposeCommand -Arguments @(
  "exec",
  "--no-TTY",
  "postgres",
  "sh",
  "-ec",
  'PGPASSWORD="$POSTGRES_PASSWORD" psql --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align --command="SELECT current_database() || '':'' || current_user;"'
)

$redisResponse = (& docker compose exec --no-TTY redis redis-cli ping).Trim()
if ($LASTEXITCODE -ne 0 -or $redisResponse -ne "PONG") {
  throw "Redis did not return PONG. Response: $redisResponse"
}

Invoke-ComposeCommand -Arguments @("exec", "--no-TTY", "mailpit", "/mailpit", "readyz")

Write-Output "AuthNexus local dependencies are healthy: PostgreSQL, Redis, and Mailpit."
