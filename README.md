# Muro SOC

Navegador de escritorio para el muro de pantallas del SOC. Muestra consolas web (CrowdStrike Falcon NG-SIEM, Check Point Harmony Email, Microsoft Defender, Entra, ServiceNow, Power BI…) en celdas con pestañas, repartidas en varios monitores, con SSO de Microsoft Entra ID.

Usa Microsoft Edge WebView2 (Evergreen): el mismo motor que Edge, actualizado por Microsoft. No requiere permisos de administrador ni instala nada. Detalles de seguridad en [SECURITY.md](SECURITY.md).

## Requisitos

- Windows 10 u 11 x64.
- Microsoft Edge WebView2 Runtime (Evergreen). Viene con Windows y se actualiza junto con Edge. Si falta, la app muestra el enlace oficial: <https://developer.microsoft.com/microsoft-edge/webview2/>.
- .NET Framework 4.8 (incluido en Windows 10 1903+ y Windows 11).

## Descargar

1. En **Releases** baja `MuroSOC-1.0.N.zip` y `SHA256SUMS.txt`.
2. Verifica el zip:
   ```powershell
   Get-FileHash .\MuroSOC-1.0.N.zip -Algorithm SHA256
   ```
   Debe coincidir con la primera línea de `SHA256SUMS.txt`.
3. Descomprime en una carpeta del usuario (por ejemplo `%LOCALAPPDATA%\Programs\MuroSOC`) y ejecuta `MuroSOC.exe`.

## Uso rápido

- La primera vez abre una celda con la página inicial en el monitor principal, a pantalla completa.
- **F10** muestra u oculta la interfaz (pestañas y barra de direcciones). **F11** entra o sale de pantalla completa.
- En la tira de pestañas: **+** pestaña nueva, **⊞** plantillas de layout, **⛶** pantalla completa y **☰** menú de la celda. También con **clic derecho → Muro SOC**.
- Con la interfaz oculta, al pasar el mouse por una celda aparece una barra flotante abajo a la derecha con el tiempo de autorefresh, **Interfaz**, **⛶** y **☰**. Se desactiva en *Configuración → General*.
- Para salir: **Alt+F4** (pide confirmación) o menú → *Cerrar Muro SOC*.

### Plantillas de layout

**⊞** en la tira de pestañas, **Ctrl+Shift+P** o menú → *Plantillas de layout...* abre las plantillas: 1 celda, 2 y 3 columnas o filas, 2×2, 3×2, 2×3, 3×3, 4×2, 4×3, 4×4 y combinaciones con una celda grande.

Cada plantilla se ve en miniatura gris. Al pasar el mouse, la vista previa grande muestra cómo quedaría el monitor y qué pestaña cae en cada celda. Nada cambia hasta pulsar **Aplicar**. Las pestañas abiertas se reparten en orden sin recargarse; se puede aplicar a un monitor o a todos.

### Crear un layout

1. Elige una plantilla o pulsa **F2** (modo edición). Cada celda muestra una barra azul.
2. **Columnas ⇆** o **Filas ⇅** divide la celda. Arrastra los divisores azules para ajustar el tamaño.
3. En cada celda escribe una URL (celda vacía) o usa **URL...**. Con **+** o **Ctrl+T** agregas más pestañas a la celda.
4. **Perfil** elige el perfil de navegador de la celda (útil para tenants distintos).
5. Para usar otro monitor: menú → *Monitores* → marca el monitor.
6. Arrastra pestañas entre celdas o monitores tomándolas de la tira de pestañas. No se recargan ni pierden la sesión.
7. **Guardar layout** (o **Ctrl+Shift+S**), ponle nombre ("Turno día", "Incidente") y pulsa **Terminar** o **Esc**.

Cambia de layout con **Ctrl+1…9** o menú → *Layout → Cargar*. *Exportar* e *Importar* comparten layouts `.json` entre equipos. Si falta un monitor del layout, sus celdas se acomodan en los disponibles y aparece un aviso.

Al cerrar y abrir la app se restaura el último estado (se guarda cada minuto y al cerrar).

### Ajustes por pestaña

Menú de la celda (☰ o clic derecho → Muro SOC):

- **Autorefresh**: apagado o cada 30 s a 30 min. Se pausa solo en páginas de login, sin conexión, si alguien usó la pestaña en los últimos 2 minutos, si hay texto escrito o un diálogo abierto. El tiempo restante se ve en modo edición o al pasar el mouse.
- **Zoom y ancho virtual**: zoom por pestaña (también Ctrl+rueda). *Ancho virtual* renderiza la página como si la celda midiera, por ejemplo, 1920 px.
- **Ajustes de página**: fijar encuadre (posición de scroll), aislar un elemento (queda solo ocupando la celda), ocultar elementos, CSS propio por patrón de URL, ocultar scrollbars y ancho mínimo.

Todo se guarda con el layout. Las pestañas con autorefresh muestran **⟳** junto al título (⏸ si está en pausa); el indicador desaparece al ocultar la interfaz.

### Ajustes para todas las pestañas

Menú → *Ajustes para todas las pestañas...* (**Ctrl+Shift+G**) cambia de una vez el autorefresh, las scrollbars o el zoom de todo el muro, de un monitor o de una celda. Opcionalmente guarda esos valores para las pestañas nuevas. Después cada pestaña se puede seguir ajustando por separado desde *Esta pestaña*.

### Atajos de teclado

Solo funcionan dentro de la app (no hay atajos globales). Se cambian en *Configuración → Atajos*.

| Atajo | Acción |
|---|---|
| F10 | Mostrar u ocultar la interfaz |
| F11 | Pantalla completa |
| Ctrl+F11 / Esc | Maximizar la celda activa / volver |
| Ctrl+Shift+P | Plantillas de layout |
| Ctrl+Shift+G | Ajustes para todas las pestañas |
| F2 | Editar layout |
| Ctrl+Shift+L | Bloquear o desbloquear el muro |
| Ctrl+T / Ctrl+W | Pestaña nueva / cerrar pestaña |
| Ctrl+Shift+T | Reabrir pestaña cerrada |
| Ctrl+Tab / Ctrl+Shift+Tab | Pestaña siguiente / anterior |
| Ctrl+L | Barra de direcciones |
| Ctrl+Shift+F5 | Recargar todo el muro |
| Ctrl+Shift+S | Guardar layout |
| Ctrl+Shift+E | Aislar elemento |
| Ctrl+, | Configuración |
| Ctrl+1…9 | Cambiar de layout |

### Modo muro

- **Bloqueo**: una capa transparente evita clics accidentales. Se desbloquea con el atajo configurado y se recuerda al reiniciar.
- **Reloj** opcional en una esquina, con zona horaria configurable. Se coloca dentro del área de la página, sin tapar las pestañas ni el menú.
- El **cursor** se oculta tras unos segundos sin movimiento.

### SSO y popups

Los logins con popup (MSAL) abren una ventana flotante sobre la celda, conservan `window.opener` y se cierran solos al terminar. Cuando una pestaña cae en una página de login, la celda se marca en naranja con "Requiere login".

Si se cae un proceso del navegador, la pestaña se recrea sola (celda azul). Sin red, la celda se pone roja con el motivo y reintenta a los 10 s, 30 s, 60 s y luego cada 5 min.

## Configuración

*Menú → Configuración…* (**Ctrl+,**). Todo queda en `%LOCALAPPDATA%\MuroSOC\config.json`; si editas el archivo con la app abierta, los cambios se aplican solos.

## Compilar

La vía principal es **GitHub Actions** (`.github/workflows/build.yml`): cada push a `main` crea un release `v1.0.<run>` con el zip y sus hashes. Los push a ramas `claude/**` solo compilan y verifican.

En local:

```cmd
build.cmd
```

- Verifica `lib\SHA256SUMS.txt` y falla si algún hash no coincide.
- Sube la versión de `VERSION` (`1.0.N`) cuando el build termina bien.
- Compila con `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5, x64) y embebe `src\scripts\*.js`.
- Deja `dist\MuroSOC-1.0.N\` y `dist\MuroSOC-1.0.N.zip`. No borra versiones anteriores.
- `build.cmd 1.0.57` usa esa versión exacta sin tocar `VERSION` (así lo usa el CI).

> **Aviso EDR:** compilar en local ejecuta `csc.exe` y CrowdStrike Falcon puede registrarlo o alertarlo. Usa preferiblemente el build de GitHub Actions. La app en sí nunca compila código en tiempo de ejecución.

### Firma de código (opcional)

El workflow trae un paso de firma apagado. Para activarlo:

1. Guarda el certificado corporativo `.pfx` en base64 como secreto `CODESIGN_PFX_BASE64` y su contraseña como `CODESIGN_PFX_PASSWORD`.
2. Crea la variable de repositorio `MUROSOC_SIGNING` = `true` (opcional: `CODESIGN_TIMESTAMP_URL`).

Mientras no haya firma, el SHA256 de cada release sirve para crear una excepción en el EDR si hiciera falta.

## SDK de WebView2

| Dato | Valor |
|---|---|
| Paquete | `Microsoft.Web.WebView2` |
| Versión | **1.0.4258.31** (publicada en nuget.org el 2026-09-28) |
| Origen | <https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31> |
| SHA512 del .nupkg | `HlGVwvyP/IWiXAU8K57Vmg59khY3DWMOxMnH4AHusFRy0/vDAjKItXUIVNXiXXaq4CLfSZyNUic1wOIlnPLsPQ==` (igual al `packageHash` del catálogo de nuget.org) |

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

Las tres deben salir `Valid`, firmadas por `CN=Microsoft Corporation`, y los hashes deben coincidir con `lib/SHA256SUMS.txt`. El workflow repite esta verificación en cada build. El procedimiento para actualizar el SDK está en [SECURITY.md](SECURITY.md).
