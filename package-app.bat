@echo off
rem ==========================================================================
rem  Night-Night's download. Website\"2 archive this build.bat" calls:
rem      Code\package-app.bat <version> "<project folder>"
rem  Night-Night is one exe, so the download is that exe with the version in its name:
rem      Website\Night-Night\Night-Night.exe  ->  Website\Night-Night-V<version>.exe
rem ==========================================================================
setlocal
set "VERSION=%~1"
set "WEB=%~2\Website"
copy /y "%WEB%\Night-Night\Night-Night.exe" "%WEB%\Night-Night-V%VERSION%.exe" >nul
