@ECHO OFF

SETLOCAL

IF "%1"=="" GOTO USAGE

set CONFIG=%1

set CODEBASE=Doom
set CODEBASE_LOWERCASE=doom
set TARGET_OS=Windows

set API_VERSION_MAJMIN=1.0

cd ..\..

set S_DRIVE=\\nextlabs.com\share\data
set API_VERSION_DIR=%S_DRIVE%\build\release_candidate\artifacts\%CODEBASE%\SDK\%API_VERSION_MAJMIN%
set ARTIFACT_FILE_PREFIX=%API_VERSION_DIR%\%TARGET_OS%\last_stable\%CODEBASE_LOWERCASE%-SDK-%API_VERSION_MAJMIN%

FOR %%f IN (%ARTIFACT_FILE_PREFIX%.*-release-bin.zip) DO (
  md SkydrmLocal\ShellPlugin\include\SDWL
  cd SkydrmLocal\ShellPlugin\include\SDWL
  unzip.exe -o -j %%f sources/include/SDWL*.*
  cd ..\..\..\..

  md libs\windows\SDWL\%CONFIG%
  cd libs\windows\SDWL\%CONFIG%
  unzip.exe -o -j %%f build/build.msvc/%CONFIG%/*
  cd ..\..\..\..

  GOTO AFTERUNZIP
)
:AFTERUNZIP

FOR %%f IN (%ARTIFACT_FILE_PREFIX%.*-release-install.zip) DO (
  md installer\RPMInstallerx64
  cd installer\RPMInstallerx64
  unzip.exe -o -j %%f install/build_RPM/output/Release_64bit/Media_MSI/DiskImages/DISK1/*
  cd ..\..

  GOTO AFTERUNZIP2
)
:AFTERUNZIP2

cd build\scripts

cd ..\..\installer
mkdir 3DConverter
mkdir hps_exchange
xcopy /s /y "%NLEXTERNALDIR2%\HOOPSCADViewer\2018_U1\bin\win64_v140" 3DConverter
xcopy /s /y "%NLEXTERNALDIR2%\HOOPS_Exchange_Publish_2018_SP2_U2_Win_VS2015" hps_exchange
cd ..\build\scripts

GOTO :EOF

:USAGE
echo Usage: %0 config
echo config can be "Win32_Debug", "Win32_Release", "x64_Debug", or "x64_Release"
