param(
    [switch]$SkipFrontend,
    [switch]$SkipBrowser,
    [switch]$Silent
)

$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceProcesses = @()

function Stop-ProcessesOnPort {
    param(
        [int]$Port
    )

    try {
        $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction Stop
        $pids = $connections | Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($pid in $pids) {
            try {
                Stop-Process -Id $pid -Force -ErrorAction Stop
                Write-Host "[INFO] Existing process on port $Port (PID $pid) was stopped." -ForegroundColor DarkYellow
            }
            catch {
                Write-Warning ("Unable to stop process {0} on port {1}: {2}" -f $pid, $Port, $_)
            }
        }
    }
    catch {
        # No process using the port or access denied; ignore silently
    }
}

function Start-ServiceProcess {
    param(
        [hashtable]$Definition
    )

    $workingDirectory = Join-Path -Path $workspaceRoot -ChildPath $Definition.Path
    if (-not (Test-Path $workingDirectory)) {
        Write-Warning "${($Definition.Name)}: folder not found ($workingDirectory)."
        return
    }

    if ($Definition.ContainsKey('Port')) {
        Stop-ProcessesOnPort -Port $Definition.Port
    }

    $arguments = $Definition.Args
    if ($SkipBrowser -and $Definition.ContainsKey('ArgsNoBrowser')) {
        $arguments = $Definition.ArgsNoBrowser
    }

    if (-not $Silent) {
        $argPreview = if ($arguments) { $arguments -join ' ' } else { '' }
        Write-Host "[RUN] $($Definition.Name) -> $($Definition.File) $argPreview" -ForegroundColor Cyan
    }

    try {
        $process = Start-Process -FilePath $Definition.File -ArgumentList $arguments -WorkingDirectory $workingDirectory -WindowStyle Hidden -PassThru
        $script:serviceProcesses += [PSCustomObject]@{
            Name = $Definition.Name
            ProcessId = $process.Id
            Port = if ($Definition.ContainsKey('Port')) { $Definition.Port } else { $null }
        }
    }
    catch {
        Write-Warning ("Failed to start {0}: {1}" -f $Definition.Name, $_)
        return
    }
}

$services = @(
    @{ Name = "IdentityService"; Path = "Backend\IdentityService"; File = "dotnet"; Args = @("run", "--urls", "http://localhost:5001"); Port = 5001 },
    @{ Name = "ReservationService"; Path = "Backend\ReservationService"; File = "dotnet"; Args = @("run", "--urls", "http://localhost:5002"); Port = 5002 },
    @{ Name = "TurnstileService"; Path = "Backend\TurnstileService"; File = "dotnet"; Args = @("run", "--urls", "http://localhost:5003"); Port = 5003 },
    @{ Name = "FeedbackService"; Path = "Backend\FeedbackService"; File = "dotnet"; Args = @("run", "--urls", "http://localhost:5004"); Port = 5004 },
    @{ Name = "ApiGateway"; Path = "Backend\ApiGateway"; File = "dotnet"; Args = @("run", "--urls", "http://localhost:5010"); Port = 5010 }
)

foreach ($service in $services) {
    Start-ServiceProcess -Definition $service
}

if (-not $SkipFrontend) {
    $frontendDefinition = @{ Name = "AngularFrontend"; Path = "Frontend"; File = "npm.cmd"; Args = @("start"); ArgsNoBrowser = @("run", "start:no-open"); Port = 4200 }
    Start-ServiceProcess -Definition $frontendDefinition
}

Start-Sleep -Seconds 3

foreach ($proc in $serviceProcesses) {
    if ($proc.Port) {
        $test = Test-NetConnection -ComputerName 'localhost' -Port $proc.Port -WarningAction SilentlyContinue
        if ($test.TcpTestSucceeded) {
            Write-Host "[STATUS] $($proc.Name) (PID $($proc.ProcessId)) -> Port $($proc.Port): Listening" -ForegroundColor Green
        }
        else {
            Write-Host "[STATUS] $($proc.Name) (PID $($proc.ProcessId)) -> Port $($proc.Port): Not reachable" -ForegroundColor Red
        }
    }
    else {
        Write-Host "[STATUS] $($proc.Name) (PID $($proc.ProcessId)) started." -ForegroundColor Green
    }
}

if (-not $Silent) {
    Write-Host "Services are running. Press Ctrl+C to stop them or close this terminal window." -ForegroundColor Yellow
}
