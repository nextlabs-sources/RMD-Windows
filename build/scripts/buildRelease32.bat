@ECHO OFF

SETLOCAL

CALL importAPIArtifacts.bat Win32_Release

CALL setVersion.bat

CALL "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

cd ..\..\SkydrmLocal

devenv.com SkydrmLocal.sln /Build "Release|x86"
IF ERRORLEVEL 1 GOTO END

cd ..\build\scripts

:END
ENDLOCAL
