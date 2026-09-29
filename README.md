# Muro SOC

Navegador de escritorio para el muro de pantallas del SOC. Usa Microsoft Edge WebView2 (Evergreen) y muestra consolas web de seguridad en celdas y pestañas, con SSO de Microsoft Entra ID.

> Estado: hito 2. Una ventana con una pestaña, environment compartido, perfiles, permisos, descargas y lista de dominios. Las celdas, pestañas y layouts llegan en los siguientes hitos.

## Requisitos

- Windows 10 u 11 x64.
- Microsoft Edge WebView2 Runtime (Evergreen). Viene con Windows y se actualiza junto con Edge. Si falta, la app muestra el enlace oficial: <https://developer.microsoft.com/microsoft-edge/webview2/>.
- .NET Framework 4.8 (incluido en Windows 10 1903+ y Windows 11).
- No requiere permisos de administrador.

## Descargar

1. Entra a **Releases** del repositorio y baja `MuroSOC-1.0.N.zip` y `SHA256SUMS.txt`.
2. Verifica el zip:
   ```powershell
   Get-FileHash .\MuroSOC-1.0.N.zip -Algorithm SHA256
   ```
   El hash debe coincidir con la primera línea de `SHA256SUMS.txt`.
3. Descomprime en una carpeta del usuario (por ejemplo `%LOCALAPPDATA%\Programs\MuroSOC`) y ejecuta `MuroSOC.exe`.

## Compilar

La vía principal es GitHub Actions (`.github/workflows/build.yml`): cada push a `main` crea un release `v1.0.<run>` con el zip y sus hashes. Los push a ramas `claude/**` solo compilan y verifican.

Compilar en local:

```cmd
build.cmd
```

- Verifica `lib\SHA256SUMS.txt` y falla si algún hash no coincide.
- Sube la versión de `VERSION` (`1.0.N`) y la escribe de vuelta al terminar bien.
- Compila con `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5, x64).
- Deja el resultado en `dist\MuroSOC-1.0.N\` y `dist\MuroSOC-1.0.N.zip`. No borra versiones anteriores.
- `build.cmd 1.0.57` compila con esa versión exacta sin tocar `VERSION` (así lo usa el CI).

> **Aviso EDR:** compilar en local ejecuta `csc.exe`, y CrowdStrike Falcon puede registrarlo o alertarlo. Por eso se recomienda usar el build de GitHub Actions. La app en sí nunca compila código en tiempo de ejecución.

## Configuración

Todo se guarda en `%LOCALAPPDATA%\MuroSOC\config.json`. Si editas el archivo con la app abierta, los cambios se aplican solos en unos segundos. Si el JSON tiene un error, la app lo avisa y mantiene la configuración anterior. La ventana de configuración llega en el hito 10.

| Clave | Por defecto | Qué hace |
|---|---|---|
| `HomeUrl` | `https://security.microsoft.com` | Página inicial. |
| `Profiles` | `["default"]` | Perfiles de navegador. Cada perfil tiene cookies propias (útil para varios tenants). Nombres con letras, números, `-` y `_`. |
| `DevToolsEnabled` | `false` | Habilita las herramientas de desarrollo (F12). |
| `DownloadsEnabled` | `false` | Permite descargas hacia `%LOCALAPPDATA%\MuroSOC\downloads`. |
| `AllowlistEnabled` | `false` | Si está activo, solo se navega a `AllowedDomains` y a los dominios de login de Microsoft. |
| `AllowedDomains` | `[]` | Dominios permitidos. Incluye subdominios: `crowdstrike.com` cubre `falcon.us-2.crowdstrike.com`. |
| `PermissionRules` | `[]` | Excepciones de permisos por dominio, por ejemplo `{"Domain":"teams.microsoft.com","Permission":"microphone","Allow":true}`. |

Permisos por defecto: se permiten `clipboard`, `autoplay` y `storage`; se deniegan `camera`, `microphone`, `geolocation`, `notifications`, `midi` y el resto. `"Permission":"*"` aplica a todos.

Los enlaces a aplicaciones externas (`mailto:`, `ms-teams:`, etc.) se bloquean para que la app no lance otros procesos.

Clic derecho en cualquier página → **Muro SOC**: abrir la página en Edge, cerrar sesión en todo (por perfil) y Acerca de.

## SDK de WebView2

| Dato | Valor |
|---|---|
| Paquete | `Microsoft.Web.WebView2` |
| Versión | **1.0.4258.31** (publicada en nuget.org el 2026-09-28) |
| Origen | <https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31> |
| SHA512 del .nupkg | `HlGVwvyP/IWiXAU8K57Vmg59khY3DWMOxMnH4AHusFRy0/vDAjKItXUIVNXiXXaq4CLfSZyNUic1wOIlnPLsPQ==` (igual al `packageHash` del catálogo de nuget.org) |

Archivos extraídos a `lib/`:

| Archivo en `lib/` | Ruta dentro del paquete |
|---|---|
| `Microsoft.Web.WebView2.Core.dll` | `lib/net462/` |
| `Microsoft.Web.WebView2.WinForms.dll` | `lib/net462/` |
| `WebView2Loader.dll` | `runtimes/win-x64/native/` |
| `WebView2-LICENSE.txt` | `LICENSE.txt` |

Verificar en Windows:

```powershell
Get-AuthenticodeSignature .\lib\*.dll | Format-Table Status, @{n='Firmante';e={$_.SignerCertificate.Subject}}, Path
Get-FileHash .\lib\*.dll -Algorithm SHA256
```

Las tres deben salir `Valid` y firmadas por `CN=Microsoft Corporation, O=Microsoft Corporation`, y los hashes deben coincidir con `lib/SHA256SUMS.txt`. El workflow de GitHub repite esta verificación en cada build.

La app usa el **runtime Evergreen** instalado en Windows, no uno propio, así que recibe los parches de Chromium por el mismo canal que Edge.
