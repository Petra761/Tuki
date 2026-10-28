# Tuki

<div align="center">
  <img src="./mobile/assets/images/icon.png" alt="Logo de Tuki" width="100" />

### Finanzas personales, en un solo lugar

Aplicación multiplataforma para organizar cuentas, ingresos, gastos y presupuestos.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Expo](https://img.shields.io/badge/Expo-SDK%2058-000020?style=flat-square&logo=expo&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-database-4169E1?style=flat-square&logo=postgresql&logoColor=white)
![Licencia privada](https://img.shields.io/badge/Licencia-Privada-lightgrey?style=flat-square)

</div>

---

## Contenido

- [¿Qué es Tuki?](#qué-es-tuki)
- [Cómo funciona](#cómo-funciona)
- [Cómo está organizado](#cómo-está-organizado)
- [Requisitos](#requisitos)
- [Ejecutar en local](#ejecutar-en-local)
- [Ejecutar con Dev Tunnels](#ejecutar-con-dev-tunnels)
- [Rutas y herramientas](#rutas-y-herramientas)
- [Pruebas y empaquetado](#pruebas-y-empaquetado)

## ¿Qué es Tuki?

Tuki es una aplicación para registrar y entender las finanzas del día a día. Desde la app puedes administrar cuentas, anotar gastos e ingresos, mover dinero entre cuentas y definir presupuestos mensuales.

La interfaz está hecha con **React Native y Expo** para Android, iOS y web. El backend está construido con **.NET 10** y dividido en servicios que se comunican detrás de un único punto de entrada: el Gateway.

## Cómo funciona

La app no se conecta directamente a cada microservicio. Envía sus solicitudes al Gateway, que las dirige al servicio responsable. Los servicios consultan PostgreSQL y devuelven la respuesta por la misma ruta.

```text
┌──────────────────────┐       ┌────────────────────┐
│ App Expo             │──────▶│ Gateway            │
│ Android · iOS · Web  │◀──────│ HTTP 5005 / HTTPS  │
└──────────────────────┘       └─────────┬──────────┘
                                         │ enruta solicitudes
                  ┌──────────────────────┼──────────────────────┐
                  ▼                      ▼                      ▼
          Autenticación             Cuentas /                Administrador
                                    Movimientos /
                                    Presupuestos
                  └──────────────────────┬──────────────────────┘
                                         ▼
                                    PostgreSQL
```

### Datos y cuentas de usuario

- Autenticación y administración mantienen sus bases globales.
- Al registrar un usuario, Tuki aprovisiona sus bases de datos de cuentas, movimientos y presupuestos.
- Las solicitudes incluyen la identidad del usuario y su tenant para mantener separados sus datos.
- El saldo se obtiene de los movimientos de cada cuenta; los importes se guardan en centavos y se muestran en bolivianos (Bs).

### Servicios del backend

| Servicio          | Qué hace                                         |               Puerto local |
| ----------------- | ------------------------------------------------ | -------------------------: |
| **Gateway**       | Recibe las solicitudes, aplica CORS y las enruta | HTTPS `5000` · HTTP `5005` |
| **Autenticación** | Registro, inicio de sesión y tokens              |               HTTPS `5105` |
| **Administrador** | Usuarios, tenants, planes y suscripciones        |               HTTPS `5101` |
| **Cuentas**       | Cuentas, tipos y categorías                      |               HTTPS `5102` |
| **Movimientos**   | Gastos, ingresos, traslados, etiquetas y atajos  |               HTTPS `5103` |
| **Presupuestos**  | Presupuestos mensuales                           |               HTTPS `5104` |

El comando de desarrollo inicia el Gateway y los cinco servicios. Normalmente solo necesitas llamar a la API a través del Gateway.

## Cómo está organizado

```text
.
├── backend/
│   └── src/
│       ├── Tuki.Gateway/        # Entrada HTTP, CORS y rutas a los servicios
│       ├── Tuki.Autenticacion/  # Registro, login y tokens
│       ├── Tuki.Administrador/  # Tenants, planes y suscripciones
│       ├── Tuki.Cuentas/        # Cuentas, tipos y categorías
│       ├── Tuki.Movimientos/    # Gastos, ingresos y traslados
│       └── Tuki.Presupuestos/   # Presupuestos
├── mobile/
│   ├── src/
│   │   ├── app/                 # Pantallas y navegación Expo Router
│   │   ├── api/                 # Comunicación con el Gateway
│   │   ├── auth/                # Sesión y autenticación
│   │   ├── components/          # Componentes reutilizables
│   │   └── theme/               # Colores y estilos
│   └── assets/                  # Iconos, imágenes y fuentes
├── scripts/                     # Dev Tunnels, verificaciones y empaquetado
├── docs/                        # Documentación y recursos
├── .env.example                 # Configuración privada del backend
└── mobile/.env.example          # URL de API para la app
```

Los archivos `.env` se mantienen separados intencionalmente: el de la raíz configura el backend y contiene credenciales; el de `mobile/` define la URL pública de la API que usa Expo.

## Requisitos

- **.NET SDK 10**
- **Node.js y npm**
- **PostgreSQL** y un usuario con permiso para crear bases (`CREATEDB`)
- **Expo Go** para probar en un teléfono
- Para túneles: **Dev Tunnels CLI** y una cuenta GitHub para iniciar sesión

## Ejecutar en local

Antes de arrancar, inicia PostgreSQL y crea una vez el usuario de desarrollo
con permiso para crear las bases de datos de cada tenant:

```sql
CREATE ROLE tuki_app WITH LOGIN CREATEDB;
```

Define su contraseña de forma interactiva en `psql` con `\password tuki_app`.
Luego confía el certificado HTTPS de desarrollo que usan las llamadas entre
microservicios:

```bash
dotnet dev-certs https --trust
```

### 1. Configurar y levantar el backend

Desde la raíz del proyecto:

```bash
cp .env.example .env
```

Edita `.env` con la conexión a PostgreSQL. Genera una clave ejecutando
`openssl rand -base64 32` y asígnala a `Jwt__Key`; usa el mismo valor para el Gateway y
Autenticación. Conserva los valores no secretos `Jwt__Issuer=Tuki`,
`Jwt__Audience=Tuki` y `Jwt__ExpirationMinutes=60`. No compartas `.env` ni lo
subas a Git.

Instala las dependencias de desarrollo y arranca los servicios:

```bash
npm install
npm run dev:backend
```

Espera a que el Gateway y los cinco servicios indiquen que están escuchando.
La API queda disponible en `http://localhost:5005` y `https://localhost:5000`.
Al arrancar se aplican las migraciones de las bases globales de Administrador y
Autenticación. Al registrar un usuario, el Administrador crea las bases de su
tenant y cada servicio aplica sus migraciones. No ejecutes esas migraciones
manualmente para el flujo local.

### 2. Configurar y abrir la app

En otra terminal, desde la raíz:

```bash
cp mobile/.env.example mobile/.env
```

Edita `mobile/.env` y configura la URL de acuerdo con dónde corre Expo:

| Dónde pruebas             | `EXPO_PUBLIC_API_URL`       |
| ------------------------- | --------------------------- |
| Web en esta computadora   | `http://localhost:5005`     |
| Expo Go en la misma Wi-Fi | `http://<IP-DE-TU-PC>:5005` |

Inicia la app:

```bash
cd mobile
npm install
npx expo start --clear
```

Abre el proyecto en el navegador o escanea el QR con Expo Go. Para usar Expo Go, el teléfono y la computadora deben estar en la misma red. Si cambias la URL de la API, reinicia Expo con `npx expo start --clear`.

## Ejecutar con Dev Tunnels

Dev Tunnels expone temporalmente el Gateway local para que un teléfono o una web alojada puedan alcanzarlo fuera de tu red. El acceso público es **HTTPS** y se reenvía al Gateway local por **HTTP en el puerto `5005`**.

### 1. Arrancar el backend

Desde la raíz:

```bash
npm run dev:backend
```

Déjalo activo en su terminal.

### 2. Crear y levantar el túnel

En otra terminal, prepara el túnel una vez:

```bash
./scripts/tunel.sh preparar
```

El script inicia sesión con GitHub si hace falta, crea el túnel `tuki-gateway` y publica el puerto `5005`. Después, levántalo:

```bash
./scripts/tunel.sh
```

Mantén esta terminal abierta y copia la dirección HTTPS que muestra, por ejemplo `https://<id>-5005.brs.devtunnels.ms`.

> Si el comando `devtunnel` no está instalado, sigue las instrucciones de instalación de [Dev Tunnels CLI](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started).

### 3. Conectar Expo al túnel

En `mobile/.env`, configura la dirección HTTPS copiada:

```env
EXPO_PUBLIC_API_URL=https://<id>-5005.brs.devtunnels.ms
```

Inicia o reinicia Expo:

```bash
cd mobile
npx expo start --clear
```

Deja activos tanto el backend como el túnel. La configuración de Dev Tunnels concede acceso anónimo temporal (8 horas); vuelve a prepararlo cuando expire. El Gateway permite CORS para los dominios HTTPS de Expo Preview.

## Descargar un APK de prueba

Al subir cambios de `mobile/` a `master`, GitHub Actions compila la app como APK e inicia una Release de prueba. También puedes volver a compilarlo desde **GitHub → Actions → Build Android APK → Run workflow** e indicar la URL HTTPS actual del túnel.

Descarga el archivo `Tuki-test.apk` desde la Release más reciente en la sección **Releases** del repositorio. Al ser un repositorio privado, debes iniciar sesión con una cuenta que tenga acceso. Android puede pedir permiso para instalar apps desconocidas.

La URL del backend queda incluida en el APK al compilarlo. El túnel debe estar activo y apuntar al mismo puerto `5005` para que la app conecte; si cambia su URL, genera otro APK con la URL nueva.

## Rutas y herramientas

La API se consulta a través del Gateway. Algunos ejemplos:

| Ruta                                | Uso                         |
| ----------------------------------- | --------------------------- |
| `/api/auth`                         | Registro e inicio de sesión |
| `/api/accounts`                     | Cuentas y tipos             |
| `/api/categories`                   | Categorías                  |
| `/api/transactions`                 | Movimientos                 |
| `/api/budgets`                      | Presupuestos                |
| `/api/plans` y `/api/subscriptions` | Planes y suscripciones      |

Con el backend iniciado, la documentación interactiva está en [`https://localhost:5000/scalar`](https://localhost:5000/scalar). Para confirmar que el túnel llega al Gateway:

```bash
./scripts/verificar-tunel.sh https://<id>-5005.brs.devtunnels.ms
```

Para validar los tipos de la app:

```bash
cd mobile
npm run typecheck
```

## Pruebas y empaquetado

Desde la raíz, compila toda la solución:

```bash
dotnet build backend/Tuki.sln
```

Con el backend en marcha, `./scripts/verificar-rutas.sh` comprueba que las
rutas públicas de OpenAPI estén enrutadas por el Gateway. Si usas Dev Tunnels,
`./scripts/verificar-tunel.sh <url-https-del-tunel>` comprueba también la
conectividad externa; sus solicitudes de login usan un correo ficticio y no
crean cuentas.

Para preparar el comprimido de entrega:

```bash
./scripts/package.sh
```

El paquete queda en `artifacts/` y contiene el último `HEAD` comprometido; por
eso, confirma primero los cambios que quieras entregar. Omite `.env`,
dependencias y resultados de compilación. Usa RAR cuando `rar` está
instalado; si no, genera ZIP. El empaquetador detiene la creación si encuentra
patrones conocidos de credenciales para que se revisen antes de compartir el
archivo.

---

<div align="center">
  Hecho para llevar las finanzas con más claridad · Proyecto privado
</div>
