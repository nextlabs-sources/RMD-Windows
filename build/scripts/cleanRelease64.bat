@ECHO OFF

SETLOCAL

CALL "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

cd ..\..\SkydrmLocal

devenv.com SkydrmLocal.sln /clean "Release|x64"
IF ERRORLEVEL 1 GOTO END

cd ..\build\scripts

:END
ENDLOCAL
