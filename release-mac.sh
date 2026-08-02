#!/bin/sh
echo
echo "Build and publish 'Release' for .NET 10 (targeting macOS on Apple silicon)"
echo
dotnet publish ./src/WOWCAMCLI/WOWCAMCLI.csproj -c Release
echo
echo "Have a nice day."
echo

