@echo off
setlocal
cd /d "%~dp0"
set CSC="%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set OUTDIR=%~dp0dist
set PAYLOAD=installer\obj\payload.zip
set GUIDE=installer\guide

call "%~dp0build.cmd" nostart
if errorlevel 1 exit /b 1

powershell.exe -NoProfile -ExecutionPolicy Bypass -File installer\fetch-pawnio.ps1 -Output "%~dp0installer\redist\PawnIO_setup.exe"
if errorlevel 1 exit /b 1

%CSC% /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /out:Uninstall.exe /win32manifest:installer\uninstall.manifest /win32icon:src\app.ico /r:System.Windows.Forms.dll installer\common\*.cs installer\uninstall\*.cs src\StartupTask.cs
if errorlevel 1 exit /b 1

powershell.exe -NoProfile -ExecutionPolicy Bypass -File installer\make-payload.ps1 -Root "%~dp0." -Output "%~dp0%PAYLOAD%"
if errorlevel 1 exit /b 1

if not exist "%OUTDIR%" mkdir "%OUTDIR%"
%CSC% /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /out:"%OUTDIR%\DevMonitorSetup.exe" /win32manifest:installer\setup.manifest /win32icon:src\app.ico ^
 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
 /resource:%PAYLOAD%,DevMonitor.Payload.zip ^
 /resource:src\app_preview.png,DevMonitor.Guide.icon.png ^
 /resource:%GUIDE%\overlay.png,DevMonitor.Guide.overlay.png ^
 /resource:%GUIDE%\tray_menu.png,DevMonitor.Guide.tray_menu.png ^
 /resource:%GUIDE%\stats_today.png,DevMonitor.Guide.stats_today.png ^
 /resource:%GUIDE%\stats_week.png,DevMonitor.Guide.stats_week.png ^
 /resource:%GUIDE%\settings.png,DevMonitor.Guide.settings.png ^
 installer\common\*.cs installer\setup\*.cs src\StartupTask.cs
if errorlevel 1 exit /b 1

echo Installer: %OUTDIR%\DevMonitorSetup.exe
