#!/bin/sh

echo
echo "WOWCAM (osx-arm64) build script 1.0.0 (by MBODM 08/2026)"
echo
echo "Performing the following 4 steps:"
echo
echo "1) clean projects"
echo "2) dotnet publish"
echo "3) copy binary"
echo "4) copy sample-config"
echo

# Build CLI binary
if [ -d ./src/WOWCAM/WOWCAM/bin ]; then rm -rf ./src/WOWCAM/WOWCAM/bin; fi
if [ -d ./src/WOWCAM/WOWCAM/obj ]; then rm -rf ./src/WOWCAM/WOWCAM/obj; fi
dotnet publish ./src/WOWCAM/WOWCAM/WOWCAM.csproj -c Release -v quiet
if [ $? -ne 0 ]; then
    echo
    echo "Error: 'dotnet publish' failed"
    echo
    exit 1
fi

# Copy the files
if [ -d ./release/mac ]; then rm -rf ./release/mac; fi
mkdir -p ./release/mac
cp -f ./src/WOWCAM/WOWCAM/bin/Release/net10.0/osx-arm64/publish/WOWCAM ./release/mac/wowcam
cp -f ./wowcam.xml.sample-mac ./release/mac/wowcam.xml.sample

echo "Finished (you can now deploy the content of the 'release/mac' folder)"
echo
echo "Have a nice day."
echo
