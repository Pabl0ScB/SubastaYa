<#
.SYNOPSIS
    Prueba de concurrencia del motor de pujas.

.DESCRIPTION
    Varios postores ofertan el mismo monto sobre la misma subasta en el mismo instante.
    El control de concurrencia optimista (columna Version) tiene que aceptar una sola
    oferta por ronda y rechazar las demas con 409 Conflict, sin que ningun saldo quede
    retenido de mas.

    El script crea sus propios postores (usuarios nuevos con saldo cargado), asi no
    depende de quien lidera cada subasta en ese momento. No borra ni modifica nada que
    ya exista: solo registra usuarios de prueba, les deposita saldo simulado y oferta.

    Requisitos: la API corriendo y la base sembrada. Se ejecuta desde la raiz del repo:

        powershell -ExecutionPolicy Bypass -File scripts/prueba-concurrencia.ps1

.PARAMETER Api
    URL base de la API.

.PARAMETER SubastaId
    Subasta sobre la que se oferta. Si no se indica, se usa la subasta activa que cierra
    mas tarde, para que no venza en medio de la prueba.

.PARAMETER Postores
    Cantidad de postores que ofertan a la vez en cada ronda.

.PARAMETER Rondas
    Maximo de rondas. La prueba termina en cuanto una ronda produce un 409.
#>
param(
    [string] $Api = 'http://localhost:5093/api/v1',
    [int] $SubastaId = 0,
    [ValidateRange(2, 20)] [int] $Postores = 5,
    [ValidateRange(1, 20)] [int] $Rondas = 5
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$invariante = [System.Globalization.CultureInfo]::InvariantCulture
$pesos = [System.Globalization.CultureInfo]::GetCultureInfo('es-AR')

function Invocar([string] $Metodo, [string] $Ruta, $Cuerpo = $null, [string] $Token = $null) {
    $parametros = @{ Method = $Metodo; Uri = "$Api$Ruta"; ContentType = 'application/json' }
    if ($Cuerpo) { $parametros.Body = ($Cuerpo | ConvertTo-Json -Compress) }
    if ($Token) { $parametros.Headers = @{ Authorization = "Bearer $Token" } }
    Invoke-RestMethod @parametros
}

# ---------- 1. Subasta ----------
try {
    if ($SubastaId -eq 0) {
        $activas = (Invocar GET '/auctions?estado=Activa&tamano=100').items |
            Where-Object { [datetime]$_.fechaFin -gt (Get-Date).AddMinutes(2) } |
            Sort-Object { [datetime]$_.fechaFin } -Descending
        if (-not $activas) {
            throw 'No hay ninguna subasta activa con mas de 2 minutos por delante. Volve a sembrar la base.'
        }
        $SubastaId = $activas[0].id
    }
    $subasta = Invocar GET "/auctions/$SubastaId"
}
catch {
    Write-Host "No se pudo leer la subasta: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Verifica que la API este corriendo en $Api."
    exit 1
}
Write-Host "Subasta $SubastaId - $($subasta.titulo) (estado: $($subasta.estado))"

# ---------- 2. Postores de prueba ----------
# Cada ejecucion usa un sufijo distinto, asi se puede repetir sin chocar con los
# emails y seudonimos de una corrida anterior.
$sufijo = [guid]::NewGuid().ToString('N').Substring(0, 6)
$password = 'Prueba1234'
$postoresCreados = @()

for ($n = 1; $n -le $Postores; $n++) {
    $email = "concurrencia-$sufijo-$n@prueba.com"
    Invocar POST '/users' @{
        email = $email; password = $password
        nombre = "Postor de prueba $n"; seudonimo = "prueba-$sufijo-$n"
    } | Out-Null
    $sesion = Invocar POST '/auth/sessions' @{ email = $email; password = $password }
    # Alcanza para varias rondas aunque la subasta ya este alta.
    Invocar POST '/wallets/me/deposits' @{ monto = 10000000 } $sesion.token | Out-Null
    $postoresCreados += [pscustomobject]@{ Id = $sesion.usuario.id; Seudonimo = $sesion.usuario.seudonimo; Token = $sesion.token }
}
Write-Host "$Postores postores de prueba creados, con saldo simulado cargado."

# ---------- 3. Rondas ----------
$cliente = New-Object System.Net.Http.HttpClient
$huboConflicto = $false
$aceptacionDoble = $false

for ($ronda = 1; $ronda -le $Rondas -and -not $huboConflicto; $ronda++) {
    $detalle = Invocar GET "/auctions/$SubastaId"
    $monto = [decimal]$detalle.pujaActual + [decimal]$detalle.incrementoMinimo
    $cuerpo = '{"monto":' + $monto.ToString($invariante) + '}'

    # El lider actual no puede volver a ofertar: quedaria rechazado por esa regla y no
    # por la concurrencia, que es lo que se quiere medir.
    $participantes = $postoresCreados | Where-Object { $_.Seudonimo -ne $detalle.seudonimoLider }

    # Todas las peticiones se arman antes y se disparan juntas, para que lleguen al
    # servidor en el mismo instante y lean la misma Version de la subasta.
    $peticiones = foreach ($p in $participantes) {
        $mensaje = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::Post), "$Api/auctions/$SubastaId/bids"
        $mensaje.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue 'Bearer', $p.Token
        $mensaje.Content = New-Object System.Net.Http.StringContent $cuerpo, ([System.Text.Encoding]::UTF8), 'application/json'
        [pscustomobject]@{ Postor = $p.Seudonimo; Mensaje = $mensaje }
    }
    $tareas = foreach ($pet in $peticiones) { $cliente.SendAsync($pet.Mensaje) }
    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]$tareas)

    Write-Host ""
    Write-Host "Ronda $ronda - $($peticiones.Count) ofertas simultaneas de $($monto.ToString('C', $pesos))"
    $aceptadas = 0
    for ($i = 0; $i -lt $tareas.Count; $i++) {
        $respuesta = $tareas[$i].Result
        $codigo = [int]$respuesta.StatusCode
        $texto = $respuesta.Content.ReadAsStringAsync().Result
        $color = switch ($codigo) { 201 { 'Green' } 409 { 'Yellow' } default { 'Gray' } }
        Write-Host ("  {0,-22} {1}  {2}" -f $peticiones[$i].Postor, $codigo, $texto) -ForegroundColor $color
        if ($codigo -eq 409) { $huboConflicto = $true }
        if ($codigo -eq 201) { $aceptadas++ }
    }

    # Es el error que la prueba busca: dos ofertas iguales aceptadas a la vez significan
    # dos postores con saldo retenido por la misma subasta.
    if ($aceptadas -gt 1) { $aceptacionDoble = $true; break }
}
$cliente.Dispose()

# ---------- 4. Resultado ----------
Write-Host ""
if ($aceptacionDoble) {
    Write-Host "FALLA: en una misma ronda se aceptaron $aceptadas ofertas. El control de concurrencia no funciono." -ForegroundColor Red
    exit 1
}
elseif ($huboConflicto) {
    Write-Host 'OK: las ofertas simultaneas se rechazaron con 409 y solo una por ronda fue aceptada.' -ForegroundColor Green
    Write-Host 'Verificacion en la base: la tabla RegistrosAuditoria tiene una fila PUJA_RECHAZADA_CONCURRENCIA por cada 409.'
    exit 0
}
else {
    Write-Host 'Ninguna ronda produjo un 409: las ofertas no llegaron a superponerse.' -ForegroundColor Yellow
    Write-Host 'Volve a ejecutar la prueba o aumenta -Postores (por ejemplo, -Postores 10).'
    exit 2
}
