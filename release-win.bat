@echo off

echo.
echo WOWCAM (win-x64) build script 1.0.0 (by MBODM 08/2026)
echo.
echo Performing the following 4 steps:
echo.
echo 1) clean projects
echo 2) dotnet publish
echo 3) copy both binaries
echo 4) copy sample-config
echo.

REM Build CLI binary
if exist .\src\WOWCAM\WOWCAM\bin rmdir /s /q .\src\WOWCAM\WOWCAM\bin
if exist .\src\WOWCAM\WOWCAM\obj rmdir /s /q .\src\WOWCAM\WOWCAM\obj
dotnet publish .\src\WOWCAM\WOWCAM\WOWCAM.csproj -c Release -v quiet
if errorlevel 1 goto :error

REM Build UI binary
if exist .\src\WOWCAM\WOWCAM.Windows\bin rmdir /s /q .\src\WOWCAM\WOWCAM.Windows\bin
if exist .\src\WOWCAM\WOWCAM.Windows\obj rmdir /s /q .\src\WOWCAM\WOWCAM.Windows\obj
dotnet publish .\src\WOWCAM\WOWCAM.Windows\WOWCAM.Windows.csproj -c Release -v quiet
if errorlevel 1 goto :error

REM Copy the files
if exist .\release\win  rmdir /s /q .\release\win
mkdir .\release\win
copy /B /V /Y .\src\WOWCAM\WOWCAM.Windows\bin\Release\net10.0-windows\win-x64\publish\WOWCAM.Windows.exe .\release\win\wowcamui.exe >NUL
copy /B /V /Y .\src\WOWCAM\WOWCAM\bin\Release\net10.0\win-x64\publish\WOWCAM.exe .\release\win\wowcam.exe >NUL
copy /B /V /Y .\wowcam.xml.sample .\release\win >NUL

echo Finished (you can now deploy the content of the 'release\win' folder)
echo.
echo Have a nice day.
goto :end

:error
echo.
echo Error: 'dotnet publish' failed

:end
REM Show timeout when started via double click
REM (copied from https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window)
if /I %0 EQU "%~dpnx0" timeout /T 9
