@echo off

set FOLDER=%LocalAppData%\Programs\WOWCAM

cls
echo.
echo WOWCAM Install 1.0.0 (by MBODM 08/2026)
echo.
echo - This batch script just copies the executable file to user's local programs directory
echo - This batch script does nothing else (therefore 'Install' is somewhat misleading here)
echo.

echo Copy...
echo.
if not exist "%FOLDER%" mkdir "%FOLDER%"
copy /B /V /Y .\wowcam.exe "%FOLDER%" >NUL
copy /B /V /Y .\wowcam.xml "%FOLDER%" >NUL
copy /B /V /Y .\WowcamWinUI.exe "%FOLDER%" >NUL

echo The 'wowcam.exe' was copied to '%FOLDER%'
echo.
echo Have a nice day.

REM Show timeout when started via double click
REM From https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window
if /I %0 EQU "%~dpnx0" (
	echo.
	echo.
	pause
	%SystemRoot%\Explorer.exe "%FOLDER%"
	timeout /T 9
)
