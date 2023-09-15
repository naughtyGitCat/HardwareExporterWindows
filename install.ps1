Write-Host "check script is run as administrator"
if ([Security.Principal.WindowsPrincipal]::GetCurrent().IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "script is run as administrator"  -ForegroundColor Green
} else {
    Write-Host "script isn't run as administrator"  -ForegroundColor Red
    exit
}

# get script path
$ScriptPath = $MyInvocation.MyCommand.Path

# set target path
$TargetPath = "C:\\Program Files\HardwareExporter"

# create target path
New-Item $TargetPath -ItemType Directory

# get script path all files
$Files = Get-ChildItem $ScriptPath -Recurse -Include *

Write-Host "copy files to target path"
foreach ($File in $Files) {
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
$exePath = "$TargetPath\HardwareExporterWindows.exe"
$service = New-Service -Name $serviceName -BinaryPathName $exePath -StartupType "Automatic" -Description "Hardware Exporter Service"
Start-Service -Name $serviceName

Write-Host "service install success"