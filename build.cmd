@echo off
rem MuroSOC - build.cmd
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "TAR=%WINDIR%\System32\tar.exe"
set "FXDIR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"

if not exist "%CSC%" (
  echo [ERROR] No se encontro csc.exe de .NET Framework 4.8 en "%CSC%".
  exit /b 1
)
if not exist "%TAR%" (
  echo [ERROR] No se encontro tar.exe en "%TAR%". Se necesita Windows 10 1803 o superior.
  exit /b 1
)

echo.
echo == 1. Verificando lib\SHA256SUMS.txt
if not exist "lib\SHA256SUMS.txt" (
  echo [ERROR] Falta lib\SHA256SUMS.txt.
  exit /b 1
)
set "LIBOK=1"
for %%r in (Microsoft.Web.WebView2.Core.dll Microsoft.Web.WebView2.WinForms.dll WebView2Loader.dll) do (
  findstr /r /c:" %%r$" /c:" %%r.$" "lib\SHA256SUMS.txt" >nul 2>&1
  if errorlevel 1 (
    echo [ERROR] lib\SHA256SUMS.txt no incluye %%r.
    set "LIBOK=0"
  )
)
for /f "usebackq tokens=1,2" %%a in ("lib\SHA256SUMS.txt") do (
  call :sha256 "lib\%%b" ACTUAL
  if /i "!ACTUAL!"=="%%a" (
    echo   OK     %%b
  ) else (
    echo   FALLA  %%b
    echo          esperado %%a
    echo          calculado !ACTUAL!
    set "LIBOK=0"
  )
)
if not "!LIBOK!"=="1" (
  echo [ERROR] Los archivos de lib\ no coinciden con lib\SHA256SUMS.txt. Build detenido.
  exit /b 1
)

echo.
echo == 2. Version
set "BUMP=0"
if "%~1"=="" (
  if not exist VERSION (
    echo [ERROR] Falta el archivo VERSION.
    exit /b 1
  )
  for /f "usebackq tokens=1-3 delims=. " %%a in ("VERSION") do (
    set "VMAJ=%%a"
    set "VMIN=%%b"
    set /a "VPAT=%%c+1"
  )
  set "APPVER=!VMAJ!.!VMIN!.!VPAT!"
  set "BUMP=1"
) else (
  set "APPVER=%~1"
)
echo %APPVER%| findstr /r "^[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*$" >nul
if errorlevel 1 (
  echo [ERROR] Version invalida: "!APPVER!". Formato esperado 1.0.N.
  exit /b 1
)
echo   Version !APPVER!

set "PKG=MuroSOC-!APPVER!"
set "OUT=dist\!PKG!"
if exist "!OUT!" (
  echo [ERROR] Ya existe !OUT!. No se sobrescriben versiones anteriores.
  exit /b 1
)
if exist "dist\!PKG!.zip" (
  echo [ERROR] Ya existe dist\!PKG!.zip. No se sobrescriben versiones anteriores.
  exit /b 1
)

echo.
echo == 3. Generando obj\AssemblyInfo.cs
if not exist obj mkdir obj
> obj\AssemblyInfo.cs echo // MuroSOC - AssemblyInfo generado por build.cmd
>> obj\AssemblyInfo.cs echo using System.Reflection;
>> obj\AssemblyInfo.cs echo using System.Runtime.InteropServices;
>> obj\AssemblyInfo.cs echo [assembly: AssemblyTitle("Muro SOC")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyProduct("Muro SOC")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyDescription("Navegador para el muro de pantallas del SOC")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyCompany("SOC")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyVersion("!APPVER!.0")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyFileVersion("!APPVER!.0")]
>> obj\AssemblyInfo.cs echo [assembly: AssemblyInformationalVersion("!APPVER!")]
>> obj\AssemblyInfo.cs echo [assembly: ComVisible(false)]

echo.
echo == 4. Compilando con %CSC%
mkdir "!OUT!"
set "RESOURCES="
if exist "src\scripts\*.js" (
  for %%s in (src\scripts\*.js) do set "RESOURCES=!RESOURCES! /resource:%%s,MuroSoc.Scripts.%%~nxs"
)
"%CSC%" /nologo /noconfig /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ /codepage:65001 /utf8output ^
  /win32manifest:app.manifest ^
  /out:"!OUT!\MuroSOC.exe" ^
  /reference:"%FXDIR%\System.dll" ^
  /reference:"%FXDIR%\System.Core.dll" ^
  /reference:"%FXDIR%\System.Drawing.dll" ^
  /reference:"%FXDIR%\System.Windows.Forms.dll" ^
  /reference:"%FXDIR%\System.Runtime.Serialization.dll" ^
  /reference:"%FXDIR%\System.Xml.dll" ^
  /reference:lib\Microsoft.Web.WebView2.Core.dll ^
  !RESOURCES! ^
  /recurse:src\*.cs obj\AssemblyInfo.cs
if errorlevel 1 (
  echo [ERROR] La compilacion fallo.
  rmdir /s /q "!OUT!" >nul 2>&1
  exit /b 1
)

if defined MUROSOC_SIGN_CMD (
  echo.
  echo == 4b. Firmando MuroSOC.exe
  call %MUROSOC_SIGN_CMD% "!OUT!\MuroSOC.exe"
  if errorlevel 1 (
    echo [ERROR] La firma de codigo fallo.
    exit /b 1
  )
)

echo.
echo == 5. Armando !OUT!
copy /y lib\Microsoft.Web.WebView2.Core.dll "!OUT!\" >nul || exit /b 1
copy /y lib\Microsoft.Web.WebView2.WinForms.dll "!OUT!\" >nul || exit /b 1
copy /y lib\WebView2Loader.dll "!OUT!\" >nul || exit /b 1
if exist lib\WebView2-LICENSE.txt copy /y lib\WebView2-LICENSE.txt "!OUT!\" >nul
if exist README.md copy /y README.md "!OUT!\" >nul
if exist SECURITY.md copy /y SECURITY.md "!OUT!\" >nul

set "SUMS=!OUT!\SHA256SUMS.txt"
type nul > "!SUMS!"
for %%f in (MuroSOC.exe Microsoft.Web.WebView2.Core.dll Microsoft.Web.WebView2.WinForms.dll WebView2Loader.dll) do (
  call :sha256 "!OUT!\%%f" H
  if not defined H exit /b 1
  >> "!SUMS!" echo !H!  %%f
)
type "!SUMS!"

echo.
echo == 6. Creando dist\!PKG!.zip
"%TAR%" -a -c -f "dist\!PKG!.zip" -C dist "!PKG!"
if errorlevel 1 (
  echo [ERROR] No se pudo crear el zip.
  exit /b 1
)
call :sha256 "dist\!PKG!.zip" ZH
> "dist\!PKG!-SHA256SUMS.txt" echo !ZH!  !PKG!.zip
type "!SUMS!" >> "dist\!PKG!-SHA256SUMS.txt"

if "!BUMP!"=="1" (
  > VERSION echo !APPVER!
)

echo.
echo Listo: !OUT!\MuroSOC.exe
echo        dist\!PKG!.zip
echo        dist\!PKG!-SHA256SUMS.txt
endlocal
exit /b 0

:sha256
set "%~2="
if not exist "%~1" goto :eof
for /f "skip=1 delims=" %%h in ('certutil -hashfile "%~1" SHA256') do (
  if not defined %~2 set "%~2=%%h"
)
if defined %~2 set "%~2=!%~2: =!"
goto :eof
