# Datos básicos de las migraciones

## Planes oficiales (Administrador)

La migración del Administrador inserta o actualiza por nombre estos cuatro planes, así todo el que levante el proyecto obtiene los mismos. Los precios se guardan en centavos (`price_cents`, bigint) y `duration_days` nulo significa sin vencimiento.

| Nombre          | Precio            | Duración       | Disponible | Sin anuncios |
| --------------- | ----------------- | -------------- | ---------- | ------------ |
| Gratis          | 0 (Bs 0)          | sin vencimiento| sí         | no           |
| Compra única    | 1990 (Bs 19,90)   | sin vencimiento| sí         | sí           |
| Premium mensual | 990 (Bs 9,90)     | 30 días        | sí         | sí           |
| Premium anual   | 7990 (Bs 79,90)   | 365 días       | sí         | sí           |

La columna `removes_ads` la rellena la misma migración: `true` en los tres planes de pago y `false` en Gratis. El estado de la suscripción (`subscription-status`) devuelve `showAds`: es `true` salvo que el usuario tenga una suscripción vigente cuyo plan tenga `removes_ads` en `true`; sin suscripción, vencida o cancelada, vuelve a ser `true`.

Una suscripción a un plan sin vencimiento no tiene `expires_at` y permanece activa mientras no se cancele: no hay renovaciones ni tareas programadas.

## Datos base de Cuentas (por tenant)

Cada base `tuki_cuentas_<tenantId>` nace con estos datos, insertados por la migración (`HasData`), con ids y fecha fijos:

- **Tipos de cuenta:** Efectivo, Tarjeta bancaria, Cuenta digital, Otro y Persona.
- **Cuenta Efectivo:** la cuenta con la que empieza cada usuario.
- **Categorías:** de gasto (Alimentación, Transporte, Educación, Salud, Hogar, Ocio y Otros gastos) y de ingreso (Sueldo y Otros ingresos).

Movimientos y Presupuestos no siembran datos: sus bases quedan vacías hasta que el usuario registre movimientos o presupuestos.
