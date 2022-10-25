@ECHO ON

SETLOCAL

set CODEBASE=Doom

set S_DRIVE=\\nextlabs.com\share\data

CALL setVersion.bat

IF "%BUILD_NUMBER%"=="" (
  set buildNumber=0
) ELSE (
  set buildNumber=%BUILD_NUMBER%
)

set version="%VERSION_MAJMIN%.%buildNumber%"

CALL c:\windows\syswow64\cscript ISAutoGUIDVersion.js ..\..\installer\SkyDRM_x64.ism %version%

xcopy /s /k /i /y ..\..\SkydrmLocal\bin\Release ..\..\installer\release_win_x64
IF ERRORLEVEL 1 GOTO :EOF
xcopy /s /k /i /y %S_DRIVE%\build\release_candidate\artifacts\%CODEBASE%\API\%VERSION_MAJMIN%\Windows\redist ..\..\install\build\data\redist
IF ERRORLEVEL 1 GOTO :EOF

xcopy /s /y ..\..\installer\themes "C:\Program Files (x86)\InstallShield\2014 SAB\Support\Themes"
IF ERRORLEVEL 1 GOTO :EOF

set POLICYCONTROLLER=%S_DRIVE%\build\release_candidate\artifacts\Doom\API\1.0\Windows\redist\SDKLib_redist_x64.zip
unzip.exe -o -j %POLICYCONTROLLER% ..\..\installer

cd ..\..\installer

"C:\Program Files (x86)\InstallShield\2014 SAB\System\IsCmdBld.exe" -x -p SkyDRM_x64.ism
IF ERRORLEVEL 1 GOTO :EOF

cd ..\build\scripts