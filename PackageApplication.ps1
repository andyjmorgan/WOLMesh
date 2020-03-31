param(
    [switch]$JustServer=$false
)
$CurrentPath = $PSScriptRoot
$publishPath = "$CurrentPath\Publish"

function KillBuildServer {
    start-process -file "dotnet" -argumentlist "build-server shutdown" -Wait
    
}
$sourceFilePath = "$PSScriptRoot\ExampleFiles"

$WebServiceProjectPath = "$CurrentPath\WOLMeshWebAPI\WOLMeshWebAPI.csproj"
$CoreDaemonProjectPath = "$CurrentPath\WOLMeshClientDaemon\WOLMeshClientDaemon.csproj"
$CoreClientProjectPath = "$CurrentPath\WOLMeshCoreClientProcess\WOLMeshCoreClientProcess.csproj"


Write-Host "Using Path: $publishPath"

if(Test-Path $publishPath){
    Write-Verbose "Removing existing publish path"
    Remove-Item $publishpath -Recurse -Force
}

KillBuildServer
New-Item $publishpath -ItemType Directory | Out-Null

if(test-path $publishpath){

    if(!$JustServer){
        if(test-path $CoreClientProjectPath){
            remove-item "$CurrentPath\WOLMeshClientDaemon\obj" -recurse -Force
            remove-item "$currentpath\WOLMeshCoreClientProcess\obj" -Recurse -force
            $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreClientProjectPath --output ""$publishPath\Agent\OSX"" --runtime osx-x64 --framework netcoreapp3.1" -Wait -PassThru -NoNewWindow
            write-host "OSX Exit code: $($process.ExitCode)"
            if($process.ExitCode -eq 0){
                start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\OSX-CLient.zip"" ""$publishPath\Agent\OSX"""  
            }
            KillBuildServer
            $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreDaemonProjectPath --output ""$publishPath\Agent\ARM"" --runtime linux-arm --framework netcoreapp3.1" -Wait -PassThru -NoNewWindow
            write-host "ARM Exit code: $($process.ExitCode)"
            if($process.ExitCode -eq 0){
                start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\Arm-Daemon.zip"" ""$publishPath\Agent\ARM"""
            }

            KillBuildServer
            $process = Start-Process -file "dotnet" -ArgumentList " publish $CoreDaemonProjectPath --output ""$publishPath\Agent\Linux-x64"" --runtime linux-x64 --framework netcoreapp3.1" -Wait -PassThru -NoNewWindow
            write-host "Linux (x64) Exit code: $($process.ExitCode)"
            if($process.ExitCode -eq 0){
                start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\Linux-x64-Daemon.zip"" ""$publishPath\Agent\Linux-x64"""
            }
            KillBuildServer
        } 
        else{
            Write-Warning "Could not find core client project path"        
        } 
    }
    if(test-path $WebServiceProjectPath){
        remove-item "$currentpath\WOLMeshWebAPI\obj" -Recurse -force
        $process = start-process -filepath "dotnet" -ArgumentList " publish $WebServiceProjectPath --output ""$publishPath\WebService"" --runtime win-x86 --framework netcoreapp3.1 --self-contained" -Wait -PassThru -NoNewWindow
        write-host "Web Service Exit code: $($process.ExitCode)"
        if($process.ExitCode -eq 0){
            
            if(test-path "$publishPath\WebService\servicesettings.json"){
                remove-item -Force -Path "$publishPath\WebService\servicesettings.json"
            }
            start-process "C:\Program Files\7-Zip\7z.exe" -ArgumentList "a -tzip ""$publishPath\WebServer.zip"" ""$publishPath\WebService"""
        
            write-host "Upgrading Test Environment" -NoNewline
            Get-Service -ComputerName recording.lab.local -Name "WOLMeshWebAPI" | Stop-Service
            Write-Host "< Service Stopped" -NoNewline
            Copy-Item -Recurse -Path "$publishPath\WebService\*" -Destination '\\recording.lab.local\c$\wolmesh\' -Force
            Write-Host "< Files Copied" -NoNewline
            Get-Service -ComputerName recording.lab.local -Name "WOLMeshWebAPI" | Start-Service
            Write-Host "< Service Started"
        }
    }
    else{
        Write-Warning "Could not find web service project path"
    }

}
else{
    Write-Warning "Could not find publish path"
}

Get-ChildItem $sourceFilePath | %{
    Copy-Item $_.fullname -Destination $publishPath
}



if(!$JustServer){
    write-host "SCP Files to PI"
    pscp -pw "P@ssw0rd10" "$publishPath\Agent\ARM\*"  "pi@192.168.1.252:/home/pi/Downloads/"
    write-host "Updating Version"
    plink pi@192.168.1.252 -batch -pw "P@ssw0rd10" /home/pi/updateagent.sh
    remove-item -Recurse "$publishPath\Agent" -force
}



remove-item -Recurse "$publishPath\WebService" -force

KillBuildServer