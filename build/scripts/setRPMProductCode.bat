@echo off
setlocal enabledelayedexpansion

powershell .\getproductcode.ps1 -Path '..\..\installer\RPMInstallerx64\SkyDRM Rights Protection Manager.msi' -Property 'ProductCode' > productcode.txt
FOR /F %%i in (productcode.txt) do SET RPMPRODUCTCODE=%%i

set SEARCHTEXT={F9C2E244-BF56-4F9D-8FC2-47B15A524C20}
set REPLACETEXT=%RPMPRODUCTCODE%

REM 1st Assembly file
set ISM_FILE=..\..\installer\SkyDRM_x64.ism
set TMP_FILE=%TMP%\%RANDOM%

powershell .\replacekeywords.ps1 -InPath '%ISM_FILE%' -OutPath '%TMP_FILE%' -srcwords '%SEARCHTEXT%' -destwords '%REPLACETEXT%'

fc /b %TMP_FILE% %ISM_FILE% > nul
IF ERRORLEVEL 1 copy %TMP_FILE% %ISM_FILE%
del %TMP_FILE%

ENDLOCAL
