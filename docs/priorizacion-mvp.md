# Tuki: priorización y detalle del MVP

> Este documento no crea historias nuevas: selecciona y detalla las de `docs/historias-de-usuario.md`.
> Los criterios de aceptación completos de cada historia están en ese documento. Aquí se indica qué parte de cada una entra al MVP y se añade lo que falta para construirla: condiciones previas, flujo, errores, datos y servicios.

## 1. Criterio de selección

Una historia entra al MVP si cumple al menos una de estas condiciones:

- Es necesaria para que el producto funcione de punta a punta (sin ella, no se puede registrar ni consultar dinero).
- Resuelve uno de los tres problemas de la app competidora de la Play Store: registrar un gasto es tedioso, no cubre a los deudores y las categorías son pobres.
- Está en los requerimientos que el cliente marcó como indispensables para la beta (RF-01 y RF-03 a RF-11).

El valor para el cliente sale de esos mismos problemas y de las decisiones del propio cliente. La evidencia de validación con usuarios se incorpora en el documento final.

## 2. Historias del MVP, en el orden en que conviene construirlas

El orden respeta las dependencias: cada historia solo depende de las anteriores. Son **11 historias y 79 SP**.

| Orden | Historia                                    | SP  | Necesidad que resuelve                                                                                           | Valor para el cliente                                                               | Depende de                          |
| ----- | ------------------------------------------- | --- | ---------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- | ----------------------------------- |
| 1     | TK-01 Registrarse y crear mi espacio        | 13  | Sin cuenta ni espacio propio no existe nada más. Es además el mayor riesgo técnico, por eso se resuelve primero. | Empezar a usar la app sin configuraciones ni intervención de nadie                  | Nada                                |
| 2     | TK-02 Iniciar y cerrar sesión               | 8   | Permite volver a entrar y asegura que cada petición llegue al espacio correcto                                   | Que solo él acceda a sus datos                                                      | TK-01                               |
| 3     | TK-07 Gestionar mis cuentas y personas      | 8   | Sin cuentas no hay dónde registrar                                                                               | Personas como cuentas con saldo con signo: el diferenciador frente a la competencia | TK-01                               |
| 4     | TK-08 Gestionar mis categorías y marcadores | 5   | Sin categorías no se puede clasificar un gasto                                                                   | Resuelve las «categorías pobres» de la competencia                                  | TK-01                               |
| 5     | TK-11 Registrar gastos e ingresos           | 8   | Es la función central del producto                                                                               | Registrar con pocos pasos, guiado por frases cortas                                 | TK-07, TK-08                        |
| 6     | TK-12 Registrar traslados y deudas          | 5   | Sin traslados no se pueden mover saldos ni cubrir deudores                                                       | Resuelve el problema de los deudores y los pagos parciales                          | TK-07, TK-11                        |
| 7     | TK-14 Consultar y corregir mis movimientos  | 8   | Un error de registro sin corrección deja los saldos mal para siempre                                             | Confianza en que sus datos son correctos                                            | TK-11                               |
| 8     | TK-09 Gestionar mis atajos                  | 3   | Prepara el registro rápido                                                                                       | Atajos propios para sus gastos rutinarios                                           | TK-07, TK-08                        |
| 9     | TK-15 Registrar con atajos                  | 5   | Hace realidad el registro en 1 o 2 clics                                                                         | Registrar un licuado de 5 Bs con un toque, con aviso para deshacer                  | TK-09, TK-11                        |
| 10    | TK-16 Ver el resumen y las métricas         | 8   | Es la pantalla principal; sin ella, lo registrado no se ve                                                       | Saber cuánto tiene, quién le debe y en qué gastó el mes                             | TK-11, TK-12                        |
| 11    | TK-06 Proteger la app con un PIN            | 8   | Es una app de datos financieros y el PIN es un requerimiento del MVP                                             | Tranquilidad de que nadie más abre su app                                           | Solo «Olvidé mi PIN» necesita TK-02 |

TK-06 no depende de las demás, así que se puede construir en cualquier momento. Está al final solo porque no bloquea a nadie.

## 3. Lo que no entra al MVP

| Tipo                                                               | Historias                                                                                               | Motivo                                                                                             |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| **Dependen del tiempo** (se recortan en este orden si hay retraso) | TK-03 Google, luego TK-10 primer uso guiado, luego TK-17 filtros y por último TK-13 dividir movimientos | El modelo de datos ya soporta las líneas, por eso la interfaz de varias líneas se recorta al final |
| **Posteriores**                                                    | TK-04 Verificar correo, TK-05 Recuperar contraseña, TK-18 Presupuestos                                  | Ninguna es necesaria para registrar y consultar dinero                                             |
| **Solo diseño**                                                    | TK-19, TK-20 y TK-21 (Suscripciones)                                                                    | El plan de suscripciones se diagrama y se contempla en el diseño, pero no se implementa            |

## 4. Alcance del MVP dentro de cada historia

Algunas historias traen más de lo que el MVP necesita. En esos casos, los criterios de aceptación de esa parte quedan para después, sin crear historias nuevas.

| Historia      | Entra en el MVP                                                                             | Queda para después                                                                             |
| ------------- | ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| TK-01         | Registro con correo y contraseña, creación del espacio, cuenta «Efectivo» y categorías base | Asignar el plan Gratis (módulo de suscripciones)                                               |
| TK-02         | Iniciar y cerrar sesión, sesión persistente, una sola sesión por usuario                    | Bloqueo por intentos fallidos (valores por definir)                                            |
| TK-07         | Cuentas, personas, tipos de cuenta predeterminados y saldo inicial                          | Crear tipos de cuenta propios                                                                  |
| TK-08         | Categorías y subcategorías, conjunto base editable                                          | Marcadores                                                                                     |
| TK-11         | Gastos e ingresos, frases guía, pagos hechos por una persona                                | Autocompletar, crear categorías y marcadores desde la pantalla de registro, guardar como atajo |
| TK-12         | Traslados, prestar, abonar y cobrar deudas, frase guía                                      | Nada                                                                                           |
| TK-14         | Historial, edición y eliminación                                                            | Nada                                                                                           |
| TK-09 y TK-15 | Todo                                                                                        | Nada                                                                                           |
| TK-16         | Saldo total y por cuenta, deudas pendientes y gasto del mes por categoría                   | Avance de presupuestos y entrar al detalle de una cuenta o categoría                           |
| TK-06         | Todo, incluido el bloqueo de 5 intentos y 10 minutos                                        | Nada                                                                                           |

## 5. Detalle de las historias del MVP

Todos los flujos pasan por el **Gateway**, que valida la sesión, toma el tenant del token y agrega el encabezado `X-Tenant-Id`. Los servicios de Contabilidad consultan al servicio de **Tenants** para ubicar la base del usuario. Esos dos pasos no se repiten en cada historia.

### TK-01 · Registrarse y crear mi espacio

**Condiciones previas**

- La app tiene conexión y el backend (Gateway, Identidad, Tenants, Contabilidad) y PostgreSQL están en ejecución.
- El correo no está registrado.

**Flujo esperado**

1. La persona elige «Registrarse» e indica correo y contraseña.
2. El Gateway reenvía la petición a Identidad, sin pedir sesión.
3. Identidad valida los datos, genera el identificador del espacio y crea al usuario con la contraseña guardada como hash.
4. Identidad le pide a Tenants, con `HttpClient`, que cree el espacio. Tenants lo registra en su catálogo con estado «en preparación» y define el nombre de la base.
5. Tenants le pide a Contabilidad que cree la base y le aplique las migraciones. Contabilidad la deja con la cuenta «Efectivo», los tipos de cuenta predeterminados y las categorías base.
6. Tenants marca el espacio como activo y deja constancia del evento.
7. Identidad entrega la sesión, cuyo token lleva el identificador del espacio, y la app muestra la pantalla principal.

**Alternativas y errores**

- Correo ya registrado: error de conflicto y no se crea nada.
- Correo con formato inválido o contraseña de menos de 8 caracteres: error de validación que indica el campo.
- Falla la creación de la base: Tenants marca el espacio como fallido y registra el error; Identidad revierte el usuario para que la persona pueda reintentar con el mismo correo.

**Datos necesarios**

- Entrada: correo y contraseña.
- Generados: identificador del usuario, identificador del espacio y nombre de la base.

**Servicios participantes:** Gateway, Identidad, Tenants, Contabilidad.

**Criterios de aceptación:** los de TK-01, salvo la asignación del plan Gratis.

### TK-02 · Iniciar y cerrar sesión

**Condiciones previas:** la persona tiene una cuenta y su espacio está activo.

**Flujo esperado**

1. La persona ingresa correo y contraseña.
2. El Gateway reenvía a Identidad, que compara con el hash guardado.
3. Si es correcto, Identidad revoca las sesiones anteriores de ese usuario (una sola sesión por persona) y entrega una sesión nueva con el identificador del espacio.
4. La app guarda la sesión de forma segura y muestra la pantalla principal.
5. Al cerrar sesión, la app pide a Identidad que revoque la sesión y borra lo guardado en el dispositivo.

**Alternativas y errores**

- Datos incorrectos: mensaje «correo o contraseña incorrectos», sin indicar cuál falló.
- Cuenta desactivada: acceso rechazado.
- Sesión vencida: la app la renueva sin pedir datos mientras la sesión larga siga vigente; si fue revocada (por ejemplo, por entrar desde otro dispositivo), la app vuelve al inicio de sesión.

**Datos necesarios:** correo, contraseña y la sesión larga guardada en el dispositivo.

**Servicios participantes:** Gateway e Identidad.

**Criterios de aceptación:** los de TK-02, salvo el bloqueo por intentos fallidos.

### TK-07 · Gestionar mis cuentas y personas

**Condiciones previas:** sesión iniciada y espacio activo.

**Flujo esperado**

1. La persona abre «Cuentas» y elige «Nueva cuenta».
2. Indica nombre, tipo (efectivo, tarjeta bancaria, cuenta digital, otro o persona) y, si quiere, un saldo inicial.
3. Contabilidad crea la cuenta y, si hay saldo inicial distinto de cero, registra el movimiento de apertura.
4. La lista de cuentas se actualiza con el saldo calculado.
5. Para editar o archivar, la persona abre la cuenta y elige la acción.

**Alternativas y errores**

- Nombre vacío: se indica el error.
- Tipo archivado: no se ofrece al crear una cuenta.
- Una persona puede empezar con saldo negativo (le debe a ella) o positivo (le debe a la persona).
- Archivar una cuenta con movimientos la oculta del registro, pero conserva el historial.

**Datos necesarios:** nombre, tipo y saldo inicial opcional. La moneda es fija (bolivianos).

**Servicios participantes:** Gateway, Contabilidad y Tenants (para ubicar la base).

**Criterios de aceptación:** los de TK-07, salvo crear tipos de cuenta propios.

### TK-08 · Gestionar mis categorías

**Condiciones previas:** sesión iniciada. El conjunto base ya existe desde el registro.

**Flujo esperado**

1. La persona abre «Categorías» y ve las de gastos y las de ingresos.
2. Elige «Nueva categoría», indica nombre y si es de gasto o de ingreso, y opcionalmente una categoría padre para crear una subcategoría.
3. Contabilidad la guarda y aparece en la lista.
4. Para renombrar o archivar, la persona abre la categoría y elige la acción.

**Alternativas y errores**

- Nombre vacío: error de validación.
- Subcategoría de una subcategoría: se rechaza, porque el máximo es de dos niveles.
- Archivar una categoría con movimientos: se permite y los movimientos la conservan.

**Datos necesarios:** nombre, tipo (gasto o ingreso) y categoría padre opcional.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-08, salvo los marcadores.

### TK-11 · Registrar gastos e ingresos

**Condiciones previas:** sesión iniciada, al menos una cuenta y una categoría del tipo correspondiente.

**Flujo esperado**

1. La persona toca «Registrar» y elige gasto o ingreso. Bajo cada opción ve una frase guía corta.
2. Indica monto, cuenta y categoría. La fecha viene en hoy y puede cambiarla; la descripción es opcional.
3. Toca «Guardar». La app envía el movimiento con un identificador generado en el dispositivo.
4. Contabilidad guarda el movimiento: en un gasto el monto resta de la cuenta y en un ingreso suma.
5. La app vuelve a la pantalla principal con los saldos actualizados.

**Alternativas y errores**

- Monto vacío o menor o igual a 0: no se guarda y se indica el error.
- Falta cuenta o categoría: se indica cuál.
- Categoría de otro tipo (de ingreso en un gasto): no se ofrece, y si llega por la API se rechaza.
- Cuenta archivada: no se ofrece, y si llega por la API se rechaza.
- Gasto pagado por una persona: se elige a la persona como cuenta y su saldo baja.
- Sin conexión: se muestra el error y el movimiento no queda registrado a medias.

**Datos necesarios:** tipo, monto, cuenta, categoría, fecha y descripción opcional.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-11, salvo autocompletar, crear categorías o marcadores desde esa pantalla y guardar como atajo; incluye las frases guía.

### TK-12 · Registrar traslados y deudas

**Condiciones previas:** sesión iniciada y al menos dos cuentas (para prestar o cobrar, una de ellas es una persona).

**Flujo esperado**

1. La persona toca «Registrar» y elige «Traslado». Ve la frase guía.
2. Elige cuenta de origen, cuenta de destino y monto.
3. Contabilidad guarda el traslado: el origen baja y el destino sube el mismo monto.
4. Para abonar o cobrar, la persona abre una cuenta de tipo persona, elige «me pagó» o «le pagué» e indica el monto. Es un traslado entre su cuenta y la persona.
5. El saldo de la persona se actualiza: si debía 40 y pagó 15, queda en 25.

**Alternativas y errores**

- Origen igual al destino: se rechaza.
- Monto vacío o menor o igual a 0: no se guarda.
- Un pago que iguala la deuda deja el saldo de la persona en 0.
- Un traslado entre cuentas propias no cambia el saldo total ni cuenta como gasto o ingreso.

**Datos necesarios:** cuenta de origen, cuenta de destino, monto, fecha y descripción opcional.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-12, completos.

### TK-14 · Consultar y corregir mis movimientos

**Condiciones previas:** sesión iniciada. Para editar o eliminar, debe haber movimientos.

**Flujo esperado**

1. La persona abre «Historial» y ve los 30 movimientos más recientes. Al desplazarse hacia abajo se cargan los siguientes.
2. Toca un movimiento para ver el detalle.
3. Para corregirlo, cambia los datos y guarda. Contabilidad actualiza el movimiento y los saldos se recalculan.
4. Para eliminarlo, elige «Eliminar» y confirma. Contabilidad lo borra y los saldos vuelven a como estaban.

**Alternativas y errores**

- Sin movimientos: mensaje que invita a registrar el primero.
- Los mismos errores de validación que al registrar (monto, cuenta, categoría).
- Un traslado se edita o se elimina completo, con sus dos cuentas.
- Movimiento ya eliminado o inexistente: se informa y la lista se actualiza.

**Datos necesarios:** identificador del movimiento y, al editar, los campos que cambian.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-14, completos.

### TK-09 · Gestionar mis atajos

**Condiciones previas:** sesión iniciada, con al menos una cuenta y una categoría de gasto.

**Flujo esperado**

1. La persona abre «Atajos» y elige «Nuevo atajo».
2. Indica nombre, monto, categoría y cuenta.
3. Contabilidad lo guarda y aparece en la pantalla principal.
4. La persona puede reordenar, editar o archivar sus atajos.

**Alternativas y errores**

- Datos vacíos o monto menor o igual a 0: no se guarda.
- Categoría de ingreso: no se ofrece, porque un atajo registra gastos.
- Editar o archivar un atajo no cambia los gastos ya registrados.

**Datos necesarios:** nombre, monto, categoría, cuenta y posición en la lista.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-09, completos.

### TK-15 · Registrar con atajos

**Condiciones previas:** existe al menos un atajo activo.

**Flujo esperado**

1. En la pantalla principal, la persona toca un atajo.
2. La app envía un gasto con los datos del atajo, la fecha de hoy y un identificador generado en el dispositivo.
3. Contabilidad guarda el gasto y el saldo de la cuenta baja.
4. Durante 5 segundos aparece un aviso con «Deshacer» y «Editar monto».
5. «Deshacer» elimina ese gasto. «Editar monto» permite cambiarlo y ajusta el saldo. Si no se toca nada, el gasto queda registrado.

**Alternativas y errores**

- La cuenta del atajo está archivada: no se registra y se avisa.
- Falla la conexión: se muestra el error y no queda un gasto a medias.
- Si se toca el mismo atajo dos veces seguidas, se registran dos gastos; cada uno con su aviso.

**Datos necesarios:** identificador del atajo, fecha de hoy e identificador del movimiento generado en el dispositivo.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-15, completos.

### TK-16 · Ver el resumen y las métricas

**Condiciones previas:** sesión iniciada. Es la pantalla a la que se llega después de iniciar sesión.

**Flujo esperado**

1. Al abrir la app, la pantalla principal pide el resumen a Contabilidad.
2. Contabilidad calcula, a partir de los movimientos del espacio, el saldo total, el saldo por cuenta, las deudas pendientes y el gasto del mes en curso por categoría.
3. La app muestra el resultado, junto con los atajos.

**Alternativas y errores**

- Sin movimientos: valores en 0 y un mensaje que invita a registrar el primero.
- Un gasto en una subcategoría suma también a su categoría padre.
- Falla la conexión: mensaje de error con opción de reintentar.

**Datos necesarios:** el mes en curso. El resto se calcula desde los movimientos.

**Servicios participantes:** Gateway, Contabilidad y Tenants.

**Criterios de aceptación:** los de TK-16, salvo el avance de presupuestos y entrar al detalle de una cuenta o categoría.

### TK-06 · Proteger la app con un PIN

**Condiciones previas:** la app está instalada. Configurar el PIN no exige haber iniciado sesión, pero lo normal es hacerlo después.

**Flujo esperado**

1. En «Seguridad», la persona elige «Configurar PIN» y lo escribe dos veces.
2. La app guarda el PIN solo en el dispositivo, de forma protegida.
3. Desde ese momento, al abrir la app aparece primero la pantalla de PIN.
4. Con el PIN correcto, se entra a la app. Para cambiarlo o quitarlo, primero se pide el PIN actual.

**Alternativas y errores**

- Los dos PIN no coinciden: no se guarda y se avisa.
- PIN incorrecto: error sin mostrar datos. Tras 5 fallos seguidos, la app se bloquea 10 minutos y cerrar y reabrir no reinicia el bloqueo.
- «Olvidé mi PIN»: la persona inicia sesión con correo y contraseña a través de Identidad; si es correcto, se elimina el PIN y puede configurar uno nuevo.

**Datos necesarios:** PIN de 4 dígitos, contador de intentos y hora de fin del bloqueo, todo guardado en el dispositivo. Nada de esto viaja al servidor.

**Servicios participantes:** App móvil. Solo «Olvidé mi PIN» usa el Gateway y a Identidad.

**Criterios de aceptación:** los de TK-06, completos.

## 6. Riesgos que conviene dejar escritos

- **Crear y migrar una base por usuario (TK-01).** Es el mayor riesgo y por eso se construye primero. Si el tiempo aprieta, es preferible simplificar el resto antes que esta parte.
- **Sin recuperar contraseña (TK-05).** Una persona que olvide su contraseña no puede volver a entrar. Para una beta con pocos usuarios es aceptable, pero debe aclararse en la presentación.
- **Marcadores, autocompletar y presupuestos quedan fuera.** El modelo de datos ya los contempla, así que añadirlos después no obliga a rehacer las tablas.
