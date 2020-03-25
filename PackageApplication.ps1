$CurrentPath = $PSScriptRoot
$publishPath = "$CurrentPath\Publish"

$WebServiceProjectPath = "$CurrentPath\WOLMeshWebAPI\WOLMeshWebAPI.csproj"
$CoreDaemonProjectPath = "$CurrentPath\WOLMeshClientDaemon\WOLMeshClientDaemon.csproj"
$CoreClientProjectPath = "$CurrentPath\WOLMeshCoreClientProcess\WOLMeshCoreClientProcess.csproj"


Write-Host "Using Path: $publishPath"

if(Test-Path $publishPath){
    Write-Verbose "Removing existing publish path"
    Remove-Item $publishpath -Recurse -Force
}

start-process -file "dotnet" -argumentlist "build-server shutdown" -Wait
New-Item $publishpath -ItemType Directory | Out-Null

if(test-path $publishpath){

    

    if(test-path $CoreClientProjectPath){
        $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreClientProjectPath --output ""$publishPath\Agent\OSX"" --runtime osx-x64 --framework netcoreapp3.1 --interactive /nodeReuse:false" -Wait -PassThru
        write-host "OSX Exit code: $($process.ExitCode)"
        $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreDaemonProjectPath --output ""$publishPath\Agent\ARM"" --runtime linux-arm --framework netcoreapp3.1 --interactive /nodeReuse:false" -Wait -PassThru
        write-host "ARM Exit code: $($process.ExitCode)"
        $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreDaemonProjectPath --output ""$publishPath\Agent\Linux-x64"" --runtime linux-x64 --framework netcoreapp3.1 --interactive /nodeReuse:false" -Wait -PassThru
        write-host "Linux (x64) Exit code: $($process.ExitCode)"
        start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\Arm-Daemon.zip"" ""$publishPath\Agent\ARM"""
        start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\Linux-x64-Daemon.zip"" ""$publishPath\Agent\Linux-x64"""
        start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\OSX-CLient.zip"" ""$publishPath\Agent\OSX"""
    } 
    else{
        Write-Warning "Could not find core client project path"        
    } 
    if(test-path $WebServiceProjectPath){
        $process = start-process -filepath "dotnet" -ArgumentList " publish $WebServiceProjectPath --output ""$publishPath\WebService"" --runtime win-x86 --self-contained --interactive /nodeReuse:false" -Wait -PassThru
        write-host "Web Service Exit code: $($process.ExitCode)"

        if(test-path "$publishPath\WebService\servicesettings.json"){
            remove-item -Force -Path "$publishPath\WebService\servicesettings.json"
        }
        start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\WebServer.zip"" ""$publishPath\WebService""" -wait
    }
    else{
        Write-Warning "Could not find web service project path"
    }

}
else{
    Write-Warning "Could not find publish path"
}

write-host "Upgrading Test Environment"
Get-Service -ComputerName recording.lab.local -Name "WOLMeshWebAPI" | Stop-Service
Copy-Item -Recurse -Path "$publishPath\WebService\*" -Destination '\\recording.lab.local\c$\wolmesh\' -Force
Get-Service -ComputerName recording.lab.local -Name "WOLMeshWebAPI" | Start-Service

remove-item -Recurse "$publishPath\WebService" -force
remove-item -Recurse "$publishPath\Agent" -force
