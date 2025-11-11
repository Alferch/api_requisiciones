@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem  Requisiciones API - Runner con Swagger (Windows .BAT)
rem  - Compila y publica en Release
rem  - Ejecuta el .exe en puerto 8080
rem  - Abre navegador en /swagger
rem ============================================================

pushd "%~dp0"

rem --- Detectar el .csproj en la carpeta actual ---
set "CSPROJ="
for %%f in (*.csproj) do (
  set "CSPROJ=%%f"
  goto :foundcsproj
)
:foundcsproj

if "%CSPROJ%"=="" (
  echo [ERROR] No se encontro ningun .csproj en %cd%
  echo Coloca este .BAT en la carpeta del proyecto que contiene el .csproj y vuelve a ejecutar.
  pause
  exit /b 1
)

for %%f in ("%CSPROJ%") do set "PROJECT_NAME=%%~nf"
set "PUBLISH_DIR=%cd%\publish"
set "HTTP_PORT=8080"
set "URLS=http://0.0.0.0:%HTTP_PORT%"
set "BROWSER_URL=http://localhost:%HTTP_PORT%/swagger"

echo.
echo ============================================
echo  Proyecto: %PROJECT_NAME%
echo  Carpeta : %cd%
echo  Publica : %PUBLISH_DIR%
echo  Puerto  : %HTTP_PORT%
echo  Swagger : %BROWSER_URL%
echo ============================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se encontro .NET SDK en PATH. Instala .NET 6/7/8 SDK.
  pause
  exit /b 1
)

rem --- Restaurar, Compilar, Publicar ---
echo [INFO] Restaurando paquetes...
dotnet restore "%CSPROJ%"
if errorlevel 1 goto :build_failed

echo [INFO] Compilando en Release...
dotnet build "%CSPROJ%" -c Release --no-restore
if errorlevel 1 goto :build_failed

echo [INFO] Publicando en Release...
dotnet publish "%CSPROJ%" -c Release -o "%PUBLISH_DIR%" --no-build
if errorlevel 1 goto :build_failed

echo [OK] Publicado en: %PUBLISH_DIR%
echo.

rem --- Determinar ruta del .exe ---
set "EXE_PATH=%PUBLISH_DIR%\%PROJECT_NAME%.exe"
if not exist "%EXE_PATH%" (
  for %%e in ("%PUBLISH_DIR%\*.exe") do (
    set "EXE_PATH=%%~fE"
    goto :foundexe
  )
)
:foundexe

if not exist "%EXE_PATH%" (
  echo [ERROR] No se encontro el ejecutable .exe en "%PUBLISH_DIR%".
  echo Revisa que el proyecto sea ejecutable y se publique correctamente.
  pause
  exit /b 1
)

rem --- Intentar abrir puerto en Firewall (si tienes permisos de admin) ---
net session >nul 2>&1
if %errorlevel%==0 (
  rem Ejecutando como admin
  netsh advfirewall firewall show rule name="Requisiciones API HTTP %HTTP_PORT%" >nul 2>&1
  if errorlevel 1 (
    echo [INFO] Creando regla de firewall para puerto %HTTP_PORT%...
    netsh advfirewall firewall add rule name="Requisiciones API HTTP %HTTP_PORT%" dir=in action=allow protocol=TCP localport=%HTTP_PORT% >nul
  )
) else (
  echo [WARN] No se detectan privilegios de administrador. Si accederas desde otra maquina,
  echo        abre el puerto %HTTP_PORT% en el Firewall de Windows manualmente.
)

rem --- Verificar si el puerto esta ocupado ---
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /R /C:":%HTTP_PORT%.*LISTENING"') do (
  echo [WARN] El puerto %HTTP_PORT% parece estar en uso (PID %%p). Cambia el puerto en este BAT si falla el arranque.
  goto :after_netstat
)
:after_netstat

echo [INFO] Iniciando API...
set "ASPNETCORE_URLS=%URLS%"
start "%PROJECT_NAME%" "%EXE_PATH%"

rem Esperar 2 segundos y abrir navegador
timeout /t 2 >nul
start "" "%BROWSER_URL%"

echo.
echo [OK] %PROJECT_NAME% iniciado.
echo     Swagger: %BROWSER_URL%
echo.
echo Cierra esta ventana si ya no necesitas ver los logs de este script.
pause
popd
exit /b 0

:build_failed
echo [ERROR] Fallo la compilacion o publicacion.
pause
popd
exit /b 1
