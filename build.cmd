@echo off
setlocal
cd /d "%~dp0"
if exist DevMonitor.exe del /f /q DevMonitor.exe >nul 2>&1
if exist DevMonitor.exe (
    if exist DevMonitor.old.exe del /f /q DevMonitor.old.exe >nul 2>&1
    ren DevMonitor.exe DevMonitor.old.exe
)
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /out:DevMonitor.exe /win32manifest:src\app.manifest /win32icon:src\app.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
if errorlevel 1 exit /b 1
copy /y src\app.config DevMonitor.exe.config >nul
if /i not "%1"=="nostart" start "" "%~dp0DevMonitor.exe"
