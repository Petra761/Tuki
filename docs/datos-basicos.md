# Datos básicos de las migraciones

## Planes oficiales (Administrador)

La migración del Administrador inserta o actualiza por nombre estos cuatro planes, así todo el que levante el proyecto obtiene los mismos. Los precios se guardan en centavos (`price_cents`, bigint) y `duration_days` nulo significa sin vencimiento.

| Nombre          | Precio            | Duración       | Disponible |
| --------------- | ----------------- | -------------- | ---------- |
| Gratis          | 0 (Bs 0)          | sin vencimiento| sí         |
| Compra única    | 1990 (Bs 19,90)   | sin vencimiento| sí         |
| Premium mensual | 990 (Bs 9,90)     | 30 días        | sí         |
| Premium anual   | 7990 (Bs 79,90)   | 365 días       | sí         |

Una suscripción a un plan sin vencimiento no tiene `expires_at` y permanece activa mientras no se cancele: no hay renovaciones ni tareas programadas.

## Datos base de Cuentas (por tenant)

Cada base `tuki_cuentas_<tenantId>` nace con estos datos, insertados por la migración (`HasData`), con ids y fecha fijos:

- **Tipos de cuenta:** Efectivo, Tarjeta bancaria, Cuenta digital, Otro y Persona.
- **Cuenta Efectivo:** la cuenta con la que empieza cada usuario.
- **Categorías:** de gasto (Alimentación, Transporte, Educación, Salud, Hogar, Ocio y Otros gastos) y de ingreso (Sueldo y Otros ingresos).

Movimientos y Presupuestos no siembran datos: sus bases quedan vacías hasta que el usuario registre movimientos o presupuestos.
