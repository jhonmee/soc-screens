# Seguridad de Muro SOC

Muro SOC está hecho para verse "aburrido" ante un EDR como CrowdStrike Falcon: corre como usuario estándar, no se instala, no lanza otros procesos y solo usa lo que trae Windows más el SDK oficial de WebView2.

## Qué hace

- Corre como usuario estándar. El manifiesto pide `asInvoker`; nunca solicita elevación.
- Usa el **WebView2 Evergreen Runtime** que ya trae Windows. Se actualiza con Edge (Edge Update / Windows Update) y recibe los mismos parches de Chromium. No empaqueta un runtime propio ni usa "Fixed Version".
- No cambia el User-Agent ni pasa argumentos a Chromium (`AdditionalBrowserArguments` queda vacío).
- Mantiene SmartScreen y los avisos de certificado inválido. Los errores de certificado se muestran en rojo y no se reintentan ni se saltan.
- Deja que WebView2 muestre su diálogo de certificados de cliente y no toca WebAuthn / passkeys / Windows Hello.
- Deniega por defecto cámara, micrófono, geolocalización, notificaciones, MIDI y otros permisos; permite portapapeles. Las excepciones por dominio se configuran y no se guardan en el perfil.
- Bloquea las descargas por defecto. Si se habilitan, van a `%LOCALAPPDATA%\MuroSOC\downloads`.
- Bloquea los enlaces a aplicaciones externas (`mailto:`, `ms-teams:`, etc.) para no lanzar otros procesos.
- Lista opcional de dominios permitidos. Los dominios de login de Microsoft siempre están permitidos.
- Inyecta un único script propio, corto y revisable: [`src/scripts/murosoc.js`](src/scripts/murosoc.js), embebido en el exe. No carga scripts remotos.

## Qué no hace

- No escribe en el registro. Solo **lee** (sin escribir) la versión de Edge desde `HKLM/HKCU\SOFTWARE\Microsoft\EdgeUpdate\Clients\{56EB18F8-…}` para mostrarla en "Acerca de".
- No crea servicios, tareas programadas, drivers ni entradas `Run`.
- No lanza PowerShell, cmd, wscript, mshta, rundll32 ni ningún otro proceso. Las únicas excepciones las pide el usuario: abrir una URL en el navegador por defecto o en Edge (`microsoft-edge:`), vía `ShellExecute`.
- No usa hooks globales (`SetWindowsHookEx`), inyección, `WriteProcessMemory`, `VirtualAllocEx` ni carga dinámica de código.
- No compila código en tiempo de ejecución (nada de `CSharpCodeProvider`, `csc.exe` ni `Assembly.Load` de bytes).
- No hace telemetría ni conexiones propias. Las únicas conexiones son las páginas que abre el usuario (y las que esas páginas hacen).
- Los procesos hijos `msedgewebview2.exe` son los del runtime de WebView2 y son esperados.

APIs de Windows que usa (todas de solo consulta o de la propia ventana):

| API | Para qué |
|---|---|
| `user32!GetDpiForWindow`, `GetDpiForSystem` | Escalar la interfaz según el DPI del monitor. |
| `user32!WindowFromPoint` | Saber sobre qué celda se suelta una pestaña al arrastrarla. |
| `Cursor.Position` (GetCursorPos), `Cursor.Hide/Show` | Ocultar el cursor tras N segundos sin movimiento. Es sondeo, no un hook. |
| COM `IShellLinkW` + `IPersistFile` (shell32) | Solo si el usuario activa "Abrir al iniciar sesión": crea `Muro SOC.lnk` en la carpeta Inicio del usuario. |
| Mutex `Local\MuroSOC-SingleInstance` | Evitar dos instancias en la misma sesión. |

> El acceso directo en la carpeta Inicio es una técnica de persistencia conocida (MITRE T1547.001) y el EDR puede registrarlo. Por eso es opcional y se activa solo desde la app.

## Dónde guarda datos

Todo en `%LOCALAPPDATA%\MuroSOC\`:

| Ruta | Contenido |
|---|---|
| `Profile\` | Carpeta de datos de WebView2 (cookies, caché, perfiles). |
| `config.json` | Configuración. |
| `state.json` | Estado de la sesión (layout abierto, pestañas). Se guarda cada minuto y al cerrar. |
| `layouts\*.json` | Layouts con nombre. |
| `logs\murosoc*.log` | Log local con rotación (5 archivos de 5 MB). |
| `downloads\` | Solo si se habilitan las descargas. |

Además, solo si el usuario activa el inicio automático: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\Muro SOC.lnk`.

El log **nunca** registra cookies, tokens ni query strings: las URL se recortan a esquema, host y ruta. En `state.json`, en los layouts y en las exportaciones se quitan parámetros sensibles (`code`, `id_token`, `access_token`, `refresh_token`, `token`, `client_info`, `session_state`, `SAMLResponse`, …) y no se guardan URLs de páginas de login.

"Cerrar sesión en todo" (menú o Configuración → Perfiles) borra cookies, caché y datos de sitios del perfil elegido, previa confirmación.

## Cómo verificar hashes

Release:

```powershell
Get-FileHash .\MuroSOC-1.0.N.zip -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

Contenido del zip (el `SHA256SUMS.txt` interno lista el exe y las DLL):

```powershell
Get-FileHash .\MuroSOC-1.0.N\* -Algorithm SHA256
Get-AuthenticodeSignature .\MuroSOC-1.0.N\*.dll
```

Las DLL deben estar firmadas por Microsoft y coincidir con `lib/SHA256SUMS.txt` del repositorio. `build.cmd` falla si `lib/` no coincide con sus hashes.

## Actualizar el SDK de WebView2 de forma controlada

1. Elige la nueva versión estable en <https://www.nuget.org/packages/Microsoft.Web.WebView2> y bájala una sola vez:
   ```powershell
   $v = '1.0.XXXX.YY'
   Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$v/microsoft.web.webview2.$v.nupkg" -OutFile wv2.nupkg
   ```
2. Compara el SHA512 del paquete con el `packageHash` del catálogo de nuget.org:
   ```powershell
   $sha = [Security.Cryptography.SHA512]::Create()
   [Convert]::ToBase64String($sha.ComputeHash([IO.File]::ReadAllBytes((Resolve-Path .\wv2.nupkg))))
   ```
3. Extrae solo `lib/net462/Microsoft.Web.WebView2.Core.dll`, `lib/net462/Microsoft.Web.WebView2.WinForms.dll`, `runtimes/win-x64/native/WebView2Loader.dll` y `LICENSE.txt` (como `WebView2-LICENSE.txt`) a `lib/`.
4. Verifica la firma: `Get-AuthenticodeSignature .\lib\*.dll` → las tres `Valid`, firmante `CN=Microsoft Corporation`.
5. Regenera los hashes:
   ```powershell
   Get-FileHash .\lib\*.dll -Algorithm SHA256 | ForEach-Object { '{0}  {1}' -f $_.Hash.ToLower(), (Split-Path $_.Path -Leaf) } | Set-Content .\lib\SHA256SUMS.txt -Encoding ascii
   ```
6. Actualiza la versión y el SHA512 en el README, haz commit y deja que el workflow compile y verifique.

## Reportar un problema

Abre un issue privado en el repositorio o avisa al equipo del SOC. No pegues cookies, tokens ni URLs completas con parámetros en los reportes.
