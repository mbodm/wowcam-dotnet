@echo off
echo.
echo Build and publish 'Release' for .NET Framework 4.8 (targeting all Windows 10/11 platforms)
echo.
dotnet publish .\src\WOWCAMCLI\WOWCAMCLI.csproj -c Release
echo.
echo Have a nice day.
echo.
pause
