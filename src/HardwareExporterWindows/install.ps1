Write-Host "check script is run as administrator"
# 检查脚本是否以管理员身份启动
if ( ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "script is run as administrator"  -ForegroundColor Green
} else {
    Write-Host "script isn't run as administrator"  -ForegroundColor Red
    exit
}

# get script path
# $ScriptPath = $MyInvocation.MyCommand.Path
$ScriptPath = (Get-Location).path
# set target path
$TargetPath = "C:\\Program Files\HardwareExporter"

# create target path
if (-not (Test-Path $TargetPath))
 {
	New-Item $TargetPath -ItemType Directory
 }

# get script path all files
$Files = Get-ChildItem $ScriptPath -Recurse -Include *

Write-Host "copy files to target path"
foreach ($File in $Files) {
	Write-Host "copy " $File.Name " to target path"
    Copy-Item $File.FullName $TargetPath -Force
}

# set service name and get service
$serviceName = "HardwareExporter"
$service = Get-Service -Name $serviceName

# 如果服务存在，则停止并删除
if ($service) {
    Write-Host "service already exists, stop and remove"
    Stop-Service -Name $serviceName
    Remove-Service -Name $serviceName
}
else
{
    Write-Host "service does not exists"
}

Write-Host "now install service"
# 注册服务
$exePath = "$TargetPath\HardwareExporterWindows"
New-NetFirewallRule -DisplayName "HardwareExporter" -Direction Inbound -Program $exePath -Action Allow
if (-not $?) {
	Write-Host "Add firewall rule failed"
	exit
}
$service = New-Service -Name $serviceName -BinaryPathName "`"$exePath`"" -StartupType "Automatic" -Description "Hardware Exporter Service"
if (-not $?) {
	Write-Host "New service failed"
	exit
}
Write-Host "now start service"
Start-Service -Name $serviceName
if (-not $?) {
	Write-Host "start service failed"
	exit
}

Write-Host "service install success"