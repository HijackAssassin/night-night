@echo off
rem ==========================================================================
rem  Night-Night's build steps. build.bat (in the project folder) calls:
rem      Code\build-app.bat <version> "<project folder>"
rem  ->  Build\Night-Night.exe, with the version stamped in as assembly attributes (the update check and
rem  Explorer's Properties > Details both read it from there).
rem  Uses the C# compiler that comes with Windows (.NET Framework 4), so nothing needs installing.
rem ==========================================================================
setlocal
set "VERSION=%~1"
set "ROOT=%~2"
set "CODE=%ROOT%\Code"
set "BUILD=%ROOT%\Build"
if "%VERSION%"=="" echo Run build.bat in the project folder, not this. & exit /b 1

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" echo The .NET Framework 4 C# compiler was not found: %CSC% & exit /b 1

set "STAMP=%TEMP%\NightNightVersion.cs"
> "%STAMP%" echo using System.Reflection;
>>"%STAMP%" echo [assembly: AssemblyTitle("Night-Night")]
>>"%STAMP%" echo [assembly: AssemblyProduct("Night-Night")]
>>"%STAMP%" echo [assembly: AssemblyCompany("Patrick Price")]
>>"%STAMP%" echo [assembly: AssemblyVersion("%VERSION%")]
>>"%STAMP%" echo [assembly: AssemblyFileVersion("%VERSION%")]

if not exist "%BUILD%" mkdir "%BUILD%"
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%CODE%\appicon.ico" /out:"%BUILD%\Night-Night.exe" "%CODE%\NightNight.cs" "%CODE%\UpdateCheck.cs" "%STAMP%"
set "RC=%ERRORLEVEL%"
del "%STAMP%" >nul 2>nul
exit /b %RC%
