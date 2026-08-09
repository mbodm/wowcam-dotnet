@echo off

cls

echo.
echo WOWCAM build script 1.0.0 (by MBODM 08/2026)
echo.
echo Will perform the following 5 steps now:
echo.
echo 1) project cleanup
echo 2) dotnet publish
echo 3) copy executables
echo 4) copy config
echo 5) copy installer
echo.

REM Publish the CLI tool
if exist .\src\WOWCAM\WOWCAM\bin rmdir /s /q .\src\WOWCAM\WOWCAM\bin
if exist .\src\WOWCAM\WOWCAM\obj rmdir /s /q .\src\WOWCAM\WOWCAM\obj
dotnet publish .\src\WOWCAM\WOWCAM\WOWCAM.csproj -c Release
REM -v quiet

REM Publish the Windows UI tool
if exist .\src\WOWCAM\WOWCAM.Windows\bin rmdir /s /q .\src\WOWCAM\WOWCAM.Windows\bin
if exist .\src\WOWCAM\WOWCAM.Windows\obj rmdir /s /q .\src\WOWCAM\WOWCAM.Windows\obj
dotnet publish .\src\WOWCAM\WOWCAM.Windows\WOWCAM.Windows.csproj -c Release

REM Deploy the files
if not exist .\release mkdir .\release
copy /B /V /Y .\src\WOWCAM\WOWCAM.Windows\bin\Release\net10.0-windows\publish\win-x64\WowcamWinUI.exe .\release\WOWCAMWINUI.exe >NUL
copy /B /V /Y .\src\WOWCAM\WOWCAM\bin\Release\net10.0\publish\win-x64\wowcam.exe .\release\wowcam.exe >NUL
copy /B /V /Y .\src\WOWCAM\WOWCAM\bin\Release\net10.0\publish\win-x64\wowcam.xml .\release\WOWCAM.xml >NUL
copy /B /V /Y .\win-install\Install.bat .\release\Install.bat >NUL

echo Finished (you can now deploy everything inside 'release' folder)
echo.
echo Have a nice day.

REM Show timeout when started via double click
REM (copied from https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window)
if /I %0 EQU "%~dpnx0" timeout /T 9
