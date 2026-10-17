# Удаление сайта ClinicHub.Web с компьютера клиники. Запуск: двойной клик по uninstall.cmd.
# Файл с контентом site-content.json сохраняется на рабочий стол, чтобы правки не пропали.

param(
    [string]$InstallDir = "C:\ClinicHub\Web"
)

$ErrorActionPreference = "Stop"
$TaskName = "ClinicHub Web"
$FirewallRule = "ClinicHub Web"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$PSCommandPath`"", "-InstallDir", "`"$InstallDir`"")
    exit
}

Write-Host "Удаление сайта клиники (ClinicHub.Web)" -ForegroundColor Yellow

if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Автозапуск убран."
}
Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -like "*$InstallDir*" -and $_.ProcessId -ne $PID } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

Get-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue | Remove-NetFirewallRule
Write-Host "Правило брандмауэра удалено."

$content = Join-Path $InstallDir "site-content.json"
if (Test-Path $content) {
    $backup = Join-Path ([Environment]::GetFolderPath("Desktop")) "site-content (копия $(Get-Date -Format 'yyyy-MM-dd')).json"
    Copy-Item $content $backup -Force
    Write-Host "Контент сохранён: $backup"
}

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
    Write-Host "Папка $InstallDir удалена."
}
$shortcut = Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "Сайт клиники.url"
Remove-Item $shortcut -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Сайт удалён." -ForegroundColor Green
Read-Host "Нажмите Enter, чтобы закрыть окно"
