@ECHO OFF

CALL cleanDebug32.bat
IF ERRORLEVEL 1 GOTO END

CALL cleanDebug64.bat
IF ERRORLEVEL 1 GOTO END

CALL cleanRelease32.bat
IF ERRORLEVEL 1 GOTO END

CALL cleanRelease64.bat
IF ERRORLEVEL 1 GOTO END

:END