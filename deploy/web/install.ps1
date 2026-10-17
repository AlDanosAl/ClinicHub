# Установка сайта ClinicHub.Web на компьютер клиники (Windows 10/11).
# Запуск: двойной клик по install.cmd (попросит права администратора).
#
# Что делает:
#   1. Проверяет ASP.NET Core Runtime 10 (нужен только для переносимой сборки) и при необходимости ставит его через winget.
#   2. Копирует сайт в C:\ClinicHub\Web. Файл с контентом site-content.json при переустановке не перезаписывает.
#   3. Регистрирует задачу планировщика «ClinicHub Web»: сайт стартует вместе с Windows, без входа пользователя,
#      и перезапускается, если упал.
#   4. Открывает порт в брандмауэре, чтобы сайт был виден с других компьютеров в сети клиники.
#   5. Проверяет, что сайт отвечает, и кладёт ярлык на рабочий стол.

param(
    [int]$Port = 8080,
    [string]$InstallDir = "C:\ClinicHub\Web"
)

$ErrorActionPreference = "Stop"
$TaskName = "ClinicHub Web"
$FirewallRule = "ClinicHub Web"
$Source = Join-Path $PSScriptRoot "app"

function Step($text) { Write-Host ""; Write-Host "==> $text" -ForegroundColor Cyan }
function Fail($text) {
    Write-Host ""
    Write-Host "ОШИБКА: $text" -ForegroundColor Red
    Read-Host "Нажмите Enter, чтобы закрыть окно"
    exit 1
}

# ── Права администратора ─────────────────────────────────────────────────────
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"",
        "-Port", $Port, "-InstallDir", "`"$InstallDir`"")
    exit
}

Write-Host "Установка сайта клиники (ClinicHub.Web)" -ForegroundColor Green
Write-Host "Папка: $InstallDir   Порт: $Port"

if (-not (Test-Path (Join-Path $Source "ClinicHub.Web.dll"))) {
    Fail "Рядом со скриптом нет папки app с сайтом. Распакуйте архив целиком и запустите install.cmd снова."
}

# Автономная сборка содержит ClinicHub.Web.exe и сам .NET; переносимая — только ClinicHub.Web.dll.
$selfContained = Test-Path (Join-Path $Source "hostfxr.dll")

# ── .NET ─────────────────────────────────────────────────────────────────────
function Find-Dotnet {
    $candidates = @("$env:ProgramFiles\dotnet\dotnet.exe")
    $cmd = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($cmd) { $candidates = @($cmd.Source) + $candidates }
    foreach ($c in $candidates) {
        if (Test-Path $c) {
            $runtimes = & $c --list-runtimes 2>$null
            if ($runtimes -match "Microsoft\.AspNetCore\.App 10\.") { return $c }
        }
    }
    return $null
}

$dotnet = $null
if (-not $selfContained) {
    Step "Проверяю ASP.NET Core Runtime 10"
    $dotnet = Find-Dotnet
    if (-not $dotnet) {
        $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
        if ($winget) {
            Write-Host "Не установлен. Ставлю через winget (нужен интернет, пара минут)..."
            & winget.exe install --id Microsoft.DotNet.AspNetCore.10 --exact --silent --accept-package-agreements --accept-source-agreements
            $dotnet = Find-Dotnet
        }
        if (-not $dotnet) {
            Start-Process "https://dotnet.microsoft.com/download/dotnet/10.0"
            Fail ("ASP.NET Core Runtime 10 не найден. Открыл страницу загрузки: скачайте «ASP.NET Core Runtime 10» " +
                  "для Windows x64 (Hosting Bundle тоже подойдёт), установите и запустите install.cmd ещё раз.")
        }
    }
    Write-Host "Найден: $dotnet"
}

# ── Остановить прошлую версию ────────────────────────────────────────────────
Step "Останавливаю прошлую версию, если она есть"
if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
}
# Процессы, запущенные из папки установки, держат файлы — закрываем их.
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -like "*$InstallDir*" -and $_.ProcessId -ne $PID } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

# ── Копирование ──────────────────────────────────────────────────────────────
Step "Копирую файлы в $InstallDir"
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
$contentFile = Join-Path $InstallDir "site-content.json"
$keepContent = Test-Path $contentFile
if ($keepContent) {
    Copy-Item $contentFile "$contentFile.bak" -Force
}
# Старые файлы версии убираем, чтобы не оставались лишние (контент сохранён выше).
Get-ChildItem $InstallDir -Force | Where-Object { $_.Name -notlike "site-content.json*" } |
    Remove-Item -Recurse -Force
Copy-Item (Join-Path $Source "*") $InstallDir -Recurse -Force -Exclude "site-content.json"
if ($keepContent) {
    Write-Host "site-content.json оставлен прежним (ваши правки контента сохранены, копия: site-content.json.bak)."
} else {
    Copy-Item (Join-Path $Source "site-content.json") $contentFile -Force
}

# ── Автозапуск ───────────────────────────────────────────────────────────────
Step "Настраиваю автозапуск (задача планировщика «$TaskName»)"
$appArgs = "--urls http://0.0.0.0:$Port --contentRoot `"$InstallDir`""
if ($selfContained) {
    $action = New-ScheduledTaskAction -Execute (Join-Path $InstallDir "ClinicHub.Web.exe") -Argument $appArgs -WorkingDirectory $InstallDir
} else {
    $dll = Join-Path $InstallDir "ClinicHub.Web.dll"
    $action = New-ScheduledTaskAction -Execute $dotnet -Argument "`"$dll`" $appArgs" -WorkingDirectory $InstallDir
}
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) `
    -StartWhenAvailable -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal `
    -Settings $settings -Description "Сайт клиники (ClinicHub.Web), порт $Port" -Force | Out-Null
Start-ScheduledTask -TaskName $TaskName

# ── Брандмауэр ───────────────────────────────────────────────────────────────
Step "Открываю порт $Port в брандмауэре Windows"
Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -DisplayName $FirewallRule -Direction Inbound -Protocol TCP -LocalPort $Port `
    -Action Allow -Profile Any | Out-Null

# ── Проверка ─────────────────────────────────────────────────────────────────
Step "Жду, пока сайт запустится"
$local = "http://localhost:$Port/"
$ok = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $r = Invoke-WebRequest -Uri $local -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -eq 200) { $ok = $true; break }
    } catch { Start-Sleep -Seconds 1 }
}
if (-not $ok) {
    Fail ("Сайт не ответил за 30 секунд. Проверьте, не занят ли порт $Port другой программой " +
          "(тогда запустите install.cmd с другим портом, см. README.txt), и посмотрите задачу «$TaskName» в Планировщике заданий.")
}

# Ярлык на общем рабочем столе.
$shortcut = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "Сайт клиники.url"
Set-Content -Path $shortcut -Value "[InternetShortcut]`r`nURL=$local" -Encoding ASCII

$ips = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
    Select-Object -ExpandProperty IPAddress

Write-Host ""
Write-Host "Готово! Сайт работает и будет запускаться вместе с компьютером." -ForegroundColor Green
Write-Host "  На этом компьютере:      $local"
foreach ($ip in $ips) { Write-Host "  С других компьютеров:    http://${ip}:$Port/" }
Write-Host ""
Write-Host "Контент (телефон, адрес, услуги, врачи): $contentFile — правится в Блокноте, перезапуск не нужен."
Start-Process $local
Read-Host "Нажмите Enter, чтобы закрыть окно"
