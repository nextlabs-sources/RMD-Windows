set VERSION_MAJOR=10
set VERSION_MINOR=12

set VERSION_MAJMIN=%VERSION_MAJOR%.%VERSION_MINOR%



SETLOCAL

set VERSION_NUM_FILE=..\..\SkydrmLocal\ShellPlugin\include\nxversionnum.h
set TMP_FILE=%TMP%\%RANDOM%

REM
REM Generate version number file content in a temp file.  Use build number 0
REM if we are not invoked by a build job.
REM
echo // This file has been automatically overwritten by setVersion.bat. > %TMP_FILE%
echo #define VERSION_MAJOR %VERSION_MAJOR% >> %TMP_FILE%
echo #define VERSION_MINOR %VERSION_MINOR% >> %TMP_FILE%
IF "%BUILD_NUMBER%"=="" (
   echo #define BUILD_NUMBER 0 >> %TMP_FILE%
) ELSE (
   echo #define BUILD_NUMBER %BUILD_NUMBER% >> %TMP_FILE%
)

REM
REM If the file content has changed, copy new content to version number file.
REM
fc /b %TMP_FILE% %VERSION_NUM_FILE% > nul
IF ERRORLEVEL 1 copy %TMP_FILE% %VERSION_NUM_FILE%

del %TMP_FILE%

ENDLOCAL
