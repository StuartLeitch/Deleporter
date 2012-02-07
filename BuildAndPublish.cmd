REM @echo off

lib\CleanProject.exe /D:src /Q

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe buildscripts\build.proj %*

DeployLocal.bat

pause