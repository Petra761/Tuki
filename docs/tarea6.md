# Tarea 6 — Integración de microservicios y reportes

## Alcance

Los puntos 1 y 4 se consideran implementados previamente, según lo indicado por
el equipo. Esta etapa completa la lógica de Reportes para los puntos 2 y 3. No se
modifican las bases de datos de los servicios de negocio ni se crean relaciones
entre bases independientes.

## 1. Microservicios y Gateway

El Gateway YARP conserva las rutas existentes de Administración, Cuentas,
Movimientos y Presupuestos. Se integra Reportes bajo `/api/reports/...` y se
publica su OpenAPI en `/openapi/reportes.json` para que aparezca en Scalar.

La ruta de Reportes requiere un JWT válido con el claim `tenant_id`. El Gateway
valida la firma, emisor y audiencia; si el cliente envía `X-Tenant-Id`, lo
compara con el claim y rechaza una discrepancia. Reportes reenvía ese tenant en
cada consulta a Cuentas, Movimientos y Presupuestos. Así, los joins se resuelven
con información obtenida por API y dentro del mismo tenant.

## 2. Consultas cruzadas de Reportes

Todas las rutas siguientes se llaman a través del Gateway. Las fechas usan
`YYYY-MM-DD`; si se omiten ambas, se usa el mes UTC actual, salvo la tendencia,
que toma la ventana de 12 meses incluyendo el actual. Los rangos explícitos no
pueden superar 366 días.

| Consulta | Endpoint | Servicios consultados y significado |
| --- | --- | --- |
| Flujo por cuenta | `GET /api/reports/cash-flow/accounts?from=2026-10-01&to=2026-10-31` | Movimientos + Cuentas. Agrupa ingresos, gastos, transferencias de entrada/salida y cambio neto por cuenta. Excluye aperturas; no las confunde con ingresos. Incluye cuentas archivadas para conservar historial. |
| Gastos por categoría | `GET /api/reports/spending/categories?from=2026-10-01&to=2026-10-31` | Movimientos + Cuentas/Categorías. Suma el valor absoluto de las líneas de gasto y agrega nombre, tipo y padre de categoría. La suma es por categoría asignada directamente; no consolida automáticamente en la categoría padre. |
| Ejecución mensual de presupuestos | `GET /api/reports/budgets/execution?year=2026&month=10` | Presupuestos + Movimientos + Cuentas/Categorías. Para cada presupuesto mensual compara el límite con el gasto real, calcula diferencia y porcentaje usado. El presupuesto sin categoría se compara con el gasto total del mes. |
| Transferencias entre cuentas | `GET /api/reports/transfers?from=2026-10-01&to=2026-10-31` | Movimientos + Cuentas. Separa cuentas de origen y destino, con sus nombres e importes, para entender los traslados sin contarlos como ingresos o gastos. |
| Tendencia de gastos por categoría | `GET /api/reports/spending/categories/trend` | Movimientos + Cuentas/Categorías. Agrupa gastos por mes y categoría, usando por defecto los últimos 12 meses calendario. Acepta también un `from` y `to` explícitos. |

Los importes de estas consultas se expresan en centavos (`100` centavos = `1 BOB`).
Las referencias a una cuenta o categoría que ya no exista se conservan por ID y
se devuelven con nombre `null`, no se descarta silenciosamente el movimiento.

## 3. Indicadores de desempeño

Estos cinco indicadores son adicionales a las cinco consultas cruzadas. Los
indicadores monetarios devuelven BOB; el rango de fechas sigue las mismas reglas
de la sección anterior.

| Indicador | Endpoint | Cálculo |
| --- | --- | --- |
| Ingresos | `GET /api/reports/performance/income?from=2026-10-01&to=2026-10-31` | Suma las líneas de transacciones `Income`. |
| Gastos | `GET /api/reports/performance/expenses?from=2026-10-01&to=2026-10-31` | Suma el valor absoluto de las líneas de transacciones `Expense`. |
| Flujo neto | `GET /api/reports/performance/net-cash-flow?from=2026-10-01&to=2026-10-31` | Ingresos menos gastos; no cuenta transferencias ni saldo inicial. |
| Gasto diario promedio | `GET /api/reports/performance/average-daily-expense?from=2026-10-01&to=2026-10-04` | Gastos del rango divididos entre todos sus días calendario, inclusive, incluso si un día no tuvo movimientos. |
| Cumplimiento de presupuestos | `GET /api/reports/performance/budget-adherence?year=2026&month=10` | Porcentaje de presupuestos activos cuyo gasto real no supera el límite. Si no existen presupuestos, el valor es `null` y el número evaluado es cero. Cada presupuesto, incluido el global, cuenta como un límite independiente. |

La implementación actual del producto usa BOB para las cuentas creadas. Las
consultas por cuenta devuelven `currencyCode`; si se habilitan otras monedas, los
indicadores monetarios globales deberán agruparse por moneda o incorporar una
conversión antes de sumarse.

## Proceso y verificación

1. Se identificaron los datos de las APIs de Movimientos, Cuentas y Presupuestos.
   Las líneas de movimientos guardan `accountId` y `categoryId`; las referencias
   se enriquecen consultando las APIs propietarias de esas entidades.
2. Se añadió el servicio de aplicación de Reportes con clientes HTTP a los tres
   servicios. Cada solicitud lleva el mismo `X-Tenant-Id`; las respuestas
   fallidas o inválidas producen `503` y no un reporte parcial.
3. Se añadieron cinco informes cruzados y cinco indicadores de negocio, con
   límites explícitos de fechas y tratamiento separado de ingresos, gastos,
   aperturas y transferencias.
4. Se documentaron las cabeceras JWT y tenant en el OpenAPI de Reportes para
   permitir probar las rutas desde Scalar.
5. Se añadieron pruebas unitarias con respuestas ficticias en memoria. No
   escriben en PostgreSQL ni consumen registros reales.

Ejecutar las pruebas desde la raíz del repositorio:

```bash
dotnet test backend/tests/Tuki.Reportes.Tests/Tuki.Reportes.Tests.csproj
```

Las pruebas usan este conjunto controlado: ingreso de `10.000` centavos, gasto
de `2.500` centavos, traslado de `1.000` centavos entre dos cuentas, una apertura
de `50.000` centavos, presupuesto de categoría de `5.000` centavos y presupuesto
global de `10.000` centavos. Para ese conjunto se verifican:

- Flujo de la cuenta de origen: `6.500` centavos netos; el traslado aparece
  separado y la apertura no se suma al flujo.
- Gasto de la categoría de prueba: `2.500` centavos.
- Presupuesto de categoría: `2.500` centavos usados de `5.000` (50 %); el global
  compara el mismo gasto total contra `10.000` (25 %).
- Ingresos: `100 BOB`; gastos: `25 BOB`; flujo neto: `75 BOB`.
- Gasto diario promedio durante cuatro días: `6,25 BOB`; cumplimiento de los dos
  presupuestos: `100 %`.
- Los traslados devuelven el nombre de ambas cuentas y las llamadas a las APIs
  llevan el ID del tenant de prueba.

### Comprobación manual con datos del proyecto

La prueba unitaria confirma los cálculos y el aislamiento de cabecera usando
respuestas ficticias. En esta sesión no se ejecutaron consultas contra PostgreSQL
ni se dispuso de una sesión JWT/tenant de prueba; por tanto, no se afirma que esos
valores sean datos reales ni que se haya completado una prueba cruzada en vivo.
Para completar esa evidencia:

1. Inicia los servicios con `npm run dev:backend` desde la raíz y abre
   `https://localhost:5000/scalar`.
2. En **Autenticación**, ejecuta `POST /api/auth/login` con una cuenta de prueba y
   conserva localmente el token y el `tenantId` devueltos; no los incluyas en
   capturas ni los publiques.
3. En **Reportes**, autoriza la solicitud con `Bearer <token>` y
   `X-Tenant-Id: <tenantId-del-token>`, selecciona una de las consultas anteriores
   y ejecútala. Comprueba los importes frente a las respuestas de las APIs fuente
   del mismo tenant.
4. Para verificar aislamiento, no alteres el tenant de la cabecera: envía un ID
   distinto del claim en un entorno de prueba y comprueba que el Gateway lo
   rechaza con `403`. No uses datos de otra persona.

La disponibilidad del túnel y la prueba remota del punto 4 se consideran
realizadas previamente según lo indicado por el equipo; esta implementación no
inicia ni mantiene un túnel.
