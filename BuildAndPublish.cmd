REM @echo off

lib\CleanProject.exe /D:src /Q

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe buildscripts\build.proj %*

DeployLocal.bat

REM powershell.exe -noprofile buildscripts\publish-nuget-packages.ps1

pause