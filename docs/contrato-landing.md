# Contrato entre la landing y Tuki

## Crear usuario y tenant

La función de servidor de la landing llama al Gateway público por HTTPS:

```text
POST {ADMIN_BASE_URL}/api/users/register
Content-Type: application/json
```

Aunque la variable se llama `ADMIN_BASE_URL`, su valor es la URL pública del
Gateway de Tuki por Dev Tunnels. El Gateway enruta esta ruta al microservicio de
Autenticación; Autenticación crea el usuario y solicita al Administrador el
tenant y las bases de datos.

El endpoint es anónimo en el Gateway. No enviar `SUPABASE_SERVICE_ROLE_KEY` ni
la clave de la landing a este servicio.

### Cuerpo para la landing

La landing omite `password`; el backend genera la contraseña inicial:

```json
{
  "email": "prueba1@example.com",
  "displayName": "Usuario de prueba"
}
```

`email` debe ser un correo válido de hasta 256 caracteres y `displayName` es
obligatorio, de hasta 100 caracteres. Si se omite `password`, el backend genera
32 bytes aleatorios, los codifica como Base64 URL safe y guarda únicamente el
hash de la contraseña. La respuesta la incluye en
`passwordTemporal` una sola vez. No se escribe en logs ni se persiste en texto
claro. No hay un cambio obligatorio de contraseña implementado todavía.

El campo `password` sigue aceptándose para conservar el registro actual de la
app móvil. En ese flujo, el llamador provee la contraseña, el backend guarda su
hash y `passwordTemporal` es `null`.

### Respuesta de éxito

Cuando el aprovisionamiento queda activo, el Gateway devuelve `201 Created`:

```json
{
  "userId": "00000000-0000-4000-8000-000000000001",
  "tenantId": "00000000-0000-4000-8000-000000000002",
  "status": "Active",
  "services": [
    {
      "service": "cuentas",
      "database": "tuki_cuentas_00000000000040008000000000000002",
      "result": "Created",
      "error": null
    },
    {
      "service": "movimientos",
      "database": "tuki_movimientos_00000000000040008000000000000002",
      "result": "Created",
      "error": null
    },
    {
      "service": "presupuestos",
      "database": "tuki_presupuestos_00000000000040008000000000000002",
      "result": "Created",
      "error": null
    }
  ],
  "passwordTemporal": "<43 caracteres Base64URL>"
}
```

El valor de `passwordTemporal` es un marcador ficticio. La landing no debe
guardarlo en Supabase ni en almacenamiento del navegador, y debe evitar
registrarlo en logs. Si el aprovisionamiento termina en `Failed`, la respuesta
`502` también puede incluirlo porque el comportamiento actual ya guardó el
usuario; en ese caso el registro no se puede repetir con el mismo correo.

### Errores relevantes

| HTTP | Significado |
| --- | --- |
| `400` | Email o nombre inválidos; también se rechaza una contraseña provista menor de 8 o mayor de 100 caracteres. |
| `409` | Ya existe un usuario con ese correo. |
| `502` | Administrador no respondió o falló el aprovisionamiento. |

Si Administrador no responde, Auth falla antes de guardar el usuario. Si el
Administrador responde con estado `Failed`, el comportamiento actual guarda el
usuario y devuelve `502` con el resumen de aprovisionamiento; un reintento con
el mismo correo devuelve `409`. No tratar ese caso como un registro limpio que
se pueda volver a enviar sin intervención.

## Configuración

En el proyecto `tuki-landing`, configurar `ADMIN_BASE_URL` en Vercel como
variable de entorno **solo de servidor** (sin prefijo `VITE_`). En local está
documentada en el `.env.example` del repositorio hermano `tuki-landing`. El URL
concreto del túnel cambia según el túnel activo; el Gateway local recibe las
solicitudes reenviadas en el puerto `5005`.
