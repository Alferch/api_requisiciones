# Variables personalizadas
$projectPath = "D:\Desarrollo\requisicionesWebApi\RequisicionesApi"
$publishPath = "$projectPath\publish"
$deployPath = "C:\inetpub\wwwroot\requisicion"
$csprojFile = "$projectPath\RequisicionesApi.csproj"
$logsPath = "$deployPath\logs"

Write-Host "  Iniciando publicación..."

# 1. Compilar y publicar como framework-dependent
dotnet publish $csprojFile -c Release -o $publishPath --self-contained false

# 2. Crear carpeta destino si no existe
if (!(Test-Path $deployPath)) {
    Write-Host "  Creando directorio de despliegue..."
    New-Item -ItemType Directory -Path $deployPath
}

# 3. Copiar archivos al directorio IIS
Write-Host " Copiando archivos publicados a IIS..."
Copy-Item "$publishPath\*" $deployPath -Recurse -Force

# 4. Crear carpeta logs si no existe
if (!(Test-Path $logsPath)) {
    Write-Host "  Creando carpeta de logs..."
    New-Item -ItemType Directory -Path $logsPath
}

# 5. Limpiar logs antiguos
Write-Host "  Limpiando logs antiguos (si hay)..."
Get-ChildItem $logsPath -Include *.log -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

# 6. Validar disponibilidad del endpoint
Write-Host "  Probando el endpoint..."
try {
    $response = Invoke-WebRequest "http://localhost/requis/api/area" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host " ¡La API está respondiendo correctamente!"
    } else {
        Write-Host "  La API respondió con código: $($response.StatusCode)"
    }
} catch {
    Write-Host "❌ No se pudo conectar al endpoint. Verifica IIS y los permisos."
}

Write-Host "  Despliegue completado."
