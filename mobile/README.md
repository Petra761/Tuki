# Tuki Mobile

<p align="center">
  <img src="./assets/images/icon.png" alt="Logo de Tuki" width="112" />
</p>

App para Android, iOS y web. La app y el backend usan archivos de entorno separados: el del backend contiene credenciales privadas; el de esta carpeta solo tiene la dirección de la API.

## Ejecutar en la red local

1. En la raíz del proyecto, prepara el backend:

   ```bash
   cp .env.example .env
   ```

   Completa en `.env` los datos de PostgreSQL y la clave JWT. Luego instala y levanta los servicios:

   ```bash
   npm install
   npm run dev:backend
   ```

2. Prepara la app. En `mobile/.env`, configura la IP de tu computadora en el puerto `5005`:

   ```env
   EXPO_PUBLIC_API_URL=http://<IP-DE-TU-PC>:5005
   ```

   Si el archivo aún no existe, créalo desde la plantilla:

   ```bash
   cp mobile/.env.example mobile/.env
   ```

3. En otra terminal, instala y abre Expo:

   ```bash
   cd mobile
   npm install
   npx expo start --clear
   ```

4. Abre Expo Go y escanea el QR. El teléfono y la computadora deben estar en la misma Wi-Fi.

## Ejecutar con Dev Tunnels

Usa esta opción cuando el teléfono no está en la misma red o cuando pruebas la web publicada de Expo.

1. En la raíz, inicia el backend con `npm run dev:backend`.
2. En otra terminal, prepara el túnel una vez y luego déjalo activo:

   ```bash
   ./scripts/tunel.sh preparar
   ./scripts/tunel.sh
   ```

   El túnel debe reenviar el puerto `5005`. Copia su dirección HTTPS, por ejemplo `https://<id>-5005.brs.devtunnels.ms`.

3. En `mobile/.env`, configura esa dirección y reinicia Expo:

   ```env
   EXPO_PUBLIC_API_URL=https://<id>-5005.brs.devtunnels.ms
   ```

   ```bash
   cd mobile
   npx expo start --clear
   ```

El backend y el túnel deben permanecer activos mientras uses la app. Cada vez que cambies la dirección de la API, reinicia Expo.
