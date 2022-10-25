
@echo off
setlocal enabledelayedexpansion

set VERSION_MAJOR=10
set VERSION_MINOR=11
set VERSION_MAJMIN=%VERSION_MAJOR%.%VERSION_MINOR%

IF "%BUILD_NUMBER%"=="" (
   set BUILD_NUMBER=0
) ELSE (
   set BUILD_NUMBER=%BUILD_NUMBER%
)

set SEARCHTEXT=1.0.0.0
set REPLACETEXT=%VERSION_MAJMIN%.%BUILD_NUMBER%

REM 1st Assembly file
set VERSION_NUM_FILE=..\..\SkydrmLocal\Viewer\Properties\AssemblyInfo.cs
set TMP_FILE=%TMP%\%RANDOM%

for /f "tokens=1,* delims=~" %%A in ( '"type %INTEXTFILE%"') do (
    SET string=%%A
    SET modified=!string:%SEARCHTEXT%=%REPLACETEXT%!

    echo !modified! >> %TMP_FILE%
)

fc /b %TMP_FILE% %VERSION_NUM_FILE% > nul
IF ERRORLEVEL 1 copy %TMP_FILE% %VERSION_NUM_FILE%
del %TMP_FILE%

REM 2st Assembly file

set VERSION_NUM_FILE=..\..\SkydrmLocal\SkydrmDesktop\Properties\AssemblyInfo.cs
set TMP_FILE=%TMP%\%RANDOM%


for /f "tokens=1,* delims=~" %%A in ( '"type %INTEXTFILE%"') do (
    SET string=%%A
    SET modified=!string:%SEARCHTEXT%=%REPLACETEXT%!

    echo !modified! >> %TMP_FILE%
)

fc /b %TMP_FILE% %VERSION_NUM_FILE% > nul
IF ERRORLEVEL 1 copy %TMP_FILE% %VERSION_NUM_FILE%
del %TMP_FILE%

ENDLOCAL
