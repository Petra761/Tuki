# Tuki

<p align="center">
  <img src="./mobile/assets/images/icon.png" alt="Logo de Tuki" width="96" />
</p>

Tuki es una aplicación para organizar cuentas, gastos, ingresos, traslados y presupuestos personales. Incluye una app multiplataforma hecha con React Native y Expo y un backend de microservicios .NET.

## Qué incluye

- **Autenticación:** registro e inicio de sesión.
- **Cuentas:** saldos, cuentas personales y cuentas para préstamos.
- **Movimientos:** gastos, ingresos, traslados, categorías, marcadores y atajos.
- **Presupuestos:** límites mensuales por categoría.
- **Administración:** usuarios, tenants, planes y suscripciones.
- **Datos aislados:** cada usuario tiene sus propias bases de datos de movimientos, cuentas y presupuestos.

La app calcula los saldos a partir de los movimientos. Los importes se guardan en centavos y se muestran en bolivianos (Bs).

## Arquitectura

La app móvil y los clientes web se comunican con el backend a través del Gateway. El Gateway enruta cada solicitud al microservicio correspondiente.

| Servicio | Responsabilidad | Puerto local |
|---|---|---:|
| Gateway | Entrada de la API, autenticación, CORS y enrutamiento | HTTP `5005`, HTTPS `5000` |
| Autenticación | Registro, login y tokens | `5105` |
| Administrador | Tenants, planes y suscripciones | `5101` |
| Cuentas | Cuentas, tipos y categorías | `5102` |
| Movimientos | Gastos, ingresos, traslados, marcadores y atajos | `5103` |
| Presupuestos | Presupuestos mensuales | `5104` |

El cliente usa rutas como `/api/auth`, `/api/accounts`, `/api/transactions` y `/api/budgets`. Para consultas de API, abre Scalar en `https://localhost:5000/scalar` cuando el backend está en marcha.

## Requisitos

- .NET SDK 10
- Node.js y npm
- PostgreSQL con un usuario que pueda crear bases de datos (`CREATEDB`)
- Expo Go para probar en un teléfono

## Ejecutar localmente

### 1. Configura el backend

Desde la raíz del repositorio:

```bash
cp .env.example .env
```

Completa en `.env` la conexión a PostgreSQL y `Jwt__Key`. Usa la misma clave para el Gateway y Autenticación; no la compartas ni la agregues a Git.

Instala las dependencias y levanta los seis servicios:

```bash
npm install
npm run dev:backend
```

El Gateway queda disponible en `http://localhost:5005` y `https://localhost:5000`. Al iniciar, los servicios globales crean o actualizan sus bases mediante migraciones; al registrar usuarios, se preparan las bases de datos de sus tenants.

### 2. Ejecuta la app

En otra terminal, crea la configuración de Expo:

```bash
cp mobile/.env.example mobile/.env
```

En `mobile/.env`, configura la URL de la API:

- Para el navegador de esta misma computadora: `http://localhost:5005`
- Para Expo Go en un teléfono conectado a la misma Wi-Fi: `http://<IP-DE-TU-PC>:5005`

Luego inicia Expo:

```bash
cd mobile
npm install
npx expo start --clear
```

Escanea el QR con Expo Go. El teléfono y la computadora deben estar en la misma red.

## Ejecutar con Dev Tunnels

Usa Dev Tunnels si el teléfono no está en la misma Wi-Fi o si pruebas la web de Expo desde otro dispositivo. El Gateway local recibe HTTP en el puerto `5005`; el túnel proporciona la dirección HTTPS pública.

1. Levanta el backend desde la raíz:

   ```bash
   npm run dev:backend
   ```

2. En otra terminal, prepara el túnel una vez y luego déjalo activo:

   ```bash
   ./scripts/tunel.sh preparar
   ./scripts/tunel.sh
   ```

   Autoriza el acceso público cuando se solicite. El túnel debe reenviar el puerto `5005`. Copia la dirección HTTPS que entrega.

3. En `mobile/.env`, asigna la URL HTTPS del túnel:

   ```env
   EXPO_PUBLIC_API_URL=https://<id>-5005.brs.devtunnels.ms
   ```

4. Reinicia Expo para incluir la URL nueva:

   ```bash
   cd mobile
   npx expo start --clear
   ```

Mantén abiertos el backend y el túnel mientras pruebes. La app y el backend usan `.env` separados: `mobile/.env` contiene solo la URL pública de la API. El Gateway permite CORS para los dominios HTTPS de Expo Preview.

## Comprobaciones útiles

```bash
# Confirmar que el Gateway responde
curl http://localhost:5005/

# Revisar que el túnel funciona
./scripts/verificar-tunel.sh https://<id>-5005.brs.devtunnels.ms

# Revisar tipos de la app
cd mobile && npm run typecheck
```

## Estructura principal

```text
backend/src/      Gateway y microservicios .NET
mobile/src/       App React Native, rutas, API, sesión y componentes
scripts/          Inicio de túneles y pruebas de API
docs/             Diagramas y material del proyecto
```

## Privacidad

El repositorio y el código son privados. No subas archivos `.env`, claves JWT, contraseñas ni datos de tenants.
