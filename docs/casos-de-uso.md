# Tuki: casos de uso

> Los diagramas están en `docs/diagramas/casos-de-uso/` (archivos `.puml`, uno por módulo).
> Esta tabla es la base para escribir las historias de usuario: cada código se convierte en una historia.

## Cómo leer este documento

**Actores**

- **Usuario:** la persona que administra sus finanzas. Es el único actor principal.
- **Usuario Premium:** especialización del Usuario. Hace todo lo que hace el Usuario y además puede cancelar su suscripción y usar las funciones premium.
- **Google** y **Proveedor de pago (Google Play):** actores externos, solo en los casos donde participan.

**El espacio del usuario (tenant).** Cada persona que se registra recibe su propio espacio, con bases de datos independientes donde viven sus cuentas y categorías, sus movimientos y sus presupuestos (una base por servicio de negocio). El usuario pertenece a ese espacio y el sistema lo reconoce en cada petición. En los casos de uso este espacio no aparece como actor, sino como el contexto de la persona: nadie ve ni toca datos de otro espacio.

**Servicios.** Tuki se compone de cuatro microservicios:

- **Administrador:** acceso y seguridad, espacios de los usuarios, planes y suscripciones.
- **Cuentas:** cuentas, personas, tipos de cuenta y categorías.
- **Movimientos:** transacciones, atajos y marcadores. También calcula los saldos.
- **Presupuestos:** límites de gasto por categoría o totales.

**Columna «Servicios».** Indica qué microservicios participan. Cuando aparece más de uno, el caso requiere interacción entre servicios. Además, todos los casos de Cuentas, Movimientos y Presupuestos pasan por el Gateway, y cada servicio toma el identificador del espacio de la petición (`X-Tenant-Id`) para ubicar la base del usuario. Eso no se repite en cada fila.

**Columna «Nota».** Solo se llena cuando hay algo que aclarar: casos que dependen del tiempo, que son solo de diseño o que tienen valores por definir.

| CU-AUT-01 | Registrarse | Crear su cuenta con correo y contraseña y obtener su espacio personal para empezar a registrar | Usuario | Administrador → Cuentas, Movimientos y Presupuestos (el Administrador crea el espacio, pide a cada servicio crear su base y asigna el plan Gratis) | El plan Gratis es parte del diseño |
| CU-AUT-02 | Iniciar sesión | Entrar a su espacio con su correo y contraseña | Usuario | Administrador | |
| CU-AUT-03 | Cerrar sesión | Salir de la app y cerrar su sesión en el dispositivo | Usuario | Administrador | |
| CU-AUT-04 | Iniciar sesión con Google | Entrar sin escribir contraseña, usando su cuenta de Google | Usuario (Google como actor externo) | Administrador | Depende del tiempo |
| CU-AUT-05 | Bloqueo por intentos fallidos | Proteger la cuenta cuando se falla varias veces al iniciar sesión | Usuario | Administrador | Valores por definir |
| CU-AUT-06 | Verificar correo | Confirmar que el correo le pertenece | Usuario | Administrador | Opcional para usar la app; obligatorio para contratar un plan |
| CU-AUT-07 | Recuperar contraseña | Volver a entrar cuando olvidó su contraseña | Usuario | Administrador | |
| CU-AUT-08 | Solicitar código de recuperación | Pedir que le llegue un código a su correo indicándolo | Usuario | Administrador | Paso incluido en CU-AUT-07 |
| CU-AUT-09 | Confirmar código del correo | Ingresar el código que recibió para demostrar que tiene acceso al correo | Usuario | Administrador | Paso incluido en CU-AUT-06 y CU-AUT-07 |
| CU-AUT-10 | Definir nueva contraseña | Elegir su nueva contraseña tras confirmar el código | Usuario | Administrador | Paso incluido en CU-AUT-07 |
| CU-AUT-15 | Restablecer PIN olvidado | Recuperar el acceso cuando olvidó el PIN, volviendo a iniciar sesión con su contraseña | Usuario | Administrador | | | |

## 2. Cuentas y personalización

Diagrama: `03-cuentas-personalizacion.puml`. Las personas (a quienes el usuario presta o debe dinero) son un tipo de cuenta, por eso se gestionan dentro de CU-PER-01.

| Código    | Caso de uso                          | Objetivo                                                                                                          | Actor principal | Servicios                                                              | Nota                                                                                      |
| --------- | ------------------------------------ | ----------------------------------------------------------------------------------------------------------------- | --------------- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| CU-PER-01 | Gestionar cuentas                    | Crear, editar y archivar sus cuentas (efectivo, banco, cuentas digitales) y las personas con quienes tiene deudas | Usuario         | Cuentas                                                                |                                                                                           |
| CU-PER-02 | Definir saldo inicial                | Indicar cuánto dinero tiene (o debe) una cuenta al empezar a usarla                                               | Usuario         | Cuentas, Movimientos                                                   | Variante opcional de CU-PER-01; el saldo inicial se guarda como un movimiento de apertura |
| CU-PER-03 | Gestionar tipos de cuenta            | Crear y archivar tipos de cuenta propios además de los predeterminados                                            | Usuario         | Cuentas                                                                |                                                                                           |
| CU-PER-04 | Gestionar categorías y subcategorías | Adaptar las categorías a su forma de gastar: crear, renombrar y archivar                                          | Usuario         | Cuentas                                                                |                                                                                           |
| CU-PER-05 | Gestionar atajos                     | Guardar sus gastos rutinarios para registrarlos con un solo toque                                                 | Usuario         | Movimientos ↔ Cuentas (comprobar que la cuenta y la categoría existen) |                                                                                           |
| CU-PER-06 | Gestionar marcadores                 | Crear etiquetas libres para agrupar movimientos                                                                   | Usuario         | Movimientos                                                            |                                                                                           |
| CU-PER-07 | Personalizar la app en el primer uso | Dejar lista la app con sus cuentas, categorías y atajos desde el comienzo                                         | Usuario         | Cuentas, Movimientos                                                   | Depende del tiempo                                                                        |

## 3. Movimientos y atajos

Diagrama: `04-movimientos.puml`.
Las transacciones guardan solo los identificadores de las cuentas y categorías, que viven en Cuentas. Por eso, al guardar, Movimientos comprueba con Cuentas que existan.

| Código    | Caso de uso                                   | Objetivo                                                                   | Actor principal | Servicios             | Nota                                                                                      |
| --------- | --------------------------------------------- | -------------------------------------------------------------------------- | --------------- | --------------------- | ----------------------------------------------------------------------------------------- |
| CU-MOV-01 | Registrar gasto                               | Anotar dinero que consumió, saliendo de una cuenta propia o de una persona | Usuario         | Movimientos ↔ Cuentas |                                                                                           |
| CU-MOV-02 | Registrar ingreso                             | Anotar dinero nuevo que entra a una cuenta                                 | Usuario         | Movimientos ↔ Cuentas |                                                                                           |
| CU-MOV-03 | Registrar traslado                            | Mover dinero entre cuentas cuando nadie consumió nada                      | Usuario         | Movimientos ↔ Cuentas |                                                                                           |
| CU-MOV-04 | Abonar o cobrar una deuda                     | Pagar o cobrar una deuda, completa o en partes, con una persona            | Usuario         | Movimientos ↔ Cuentas | Tipo particular de CU-MOV-03                                                              |
| CU-MOV-05 | Dividir en varias categorías o cuentas        | Repartir un mismo movimiento entre varias categorías o varias cuentas      | Usuario         | Movimientos ↔ Cuentas | Depende del tiempo                                                                        |
| CU-MOV-06 | Autocompletar al registrar                    | Recibir sugerencias de datos ya usados para registrar más rápido           | Usuario         | Movimientos           |                                                                                           |
| CU-MOV-07 | Ver historial de movimientos                  | Revisar lo que ha registrado                                               | Usuario         | Movimientos           | La app muestra los nombres de cuentas y categorías con las listas que ya cargó de Cuentas |
| CU-MOV-08 | Gestionar movimientos                         | Corregir o eliminar un movimiento ya registrado                            | Usuario         | Movimientos ↔ Cuentas |                                                                                           |
| CU-MOV-09 | Registrar con un atajo                        | Registrar un gasto rutinario con un solo toque                             | Usuario         | Movimientos ↔ Cuentas | Tipo particular de CU-MOV-01                                                              |
| CU-MOV-10 | Deshacer o editar el monto tras usar un atajo | Corregir un atajo recién usado si se equivocó                              | Usuario         | Movimientos           |                                                                                           |

## 4. Seguimiento

Diagrama: `05-seguimiento.puml`.

Estas pantallas reúnen datos de varios servicios: los saldos y los gastos vienen de Movimientos, los nombres de Cuentas y los límites de Presupuestos. La app junta las respuestas.

| Código    | Caso de uso            | Objetivo                                                                                                                       | Actor principal | Servicios                                                  | Nota                                                                     |
| --------- | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------ | --------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------ | --- | ------- | ------------ | --- |
| CU-SEG-01 | Ver resumen y métricas | Saber cómo van sus finanzas: saldo total y por cuenta, deudas pendientes, gasto del mes por categoría y avance de presupuestos | Usuario         | Movimientos, Cuentas y Presupuestos                        | Presupuestos pide a Movimientos lo gastado y a Cuentas las subcategorías |
| CU-SEG-02 | Filtrar resultados     | Acotar lo que ve en el resumen o en el historial                                                                               | Usuario         | Movimientos                                                | Depende del tiempo; filtros por definir                                  |
| CU-SEG-03 | Gestionar presupuestos | Fijar límites de gasto por categoría o en total, y mantenerlos                                                                 | Usuario         | Presupuestos ↔ Cuentas (comprobar que la categoría existe) |                                                                          |     | Usuario | Contabilidad |     |

## 5. Suscripciones

Diagrama: `06-suscripciones.puml`. Este módulo vive en el Administrador. Los planes, la suscripción y la consulta de su estado se implementan en el backend de la tarea 4, sin cobro real. El pago con un proveedor externo queda solo como diseño.

| Código    | Caso de uso              | Objetivo                                                                      | Actor principal                                | Servicios                                                               | Nota                                                                        |
| --------- | ------------------------ | ----------------------------------------------------------------------------- | ---------------------------------------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| CU-SUS-01 | Ver mi plan              | Saber qué plan tiene y qué incluye                                            | Usuario                                        | Administrador                                                           | Se implementa en la tarea 4: indica si está activa, vencida o inexistente   |
| CU-SUS-02 | Contratar Premium        | Pasar al plan Premium para usar sus funciones                                 | Usuario                                        | Administrador                                                           | Se implementa en la tarea 4, sin cobro real; hoy no exige correo verificado |
| CU-SUS-03 | Cancelar suscripción     | Dejar de pagar Premium, conservando el acceso hasta el fin del período pagado | Usuario Premium                                | Administrador                                                           | Solo diseño                                                                 |
| CU-SUS-04 | Usar una función premium | Aprovechar las funciones exclusivas de Premium                                | Usuario                                        | Cuentas, Movimientos o Presupuestos ↔ Administrador (verificar el plan) | Solo diseño; usaría la consulta de estado de CU-SUS-01                      |
| CU-SUS-05 | Pagar suscripción        | Realizar el pago de la suscripción con el proveedor                           | Usuario (Proveedor de pago como actor externo) | Administrador                                                           | Solo diseño; no se integra pasarela de pago                                 |

## Resumen

- **42 casos de uso** en 6 diagramas.
- **5 casos existen solo como pasos incluidos** en otros (CU-AUT-08, CU-AUT-09, CU-AUT-10, CU-AUT-16 y CU-SUS-05). Al escribir las historias de usuario se pueden agrupar con el caso que los usa.
- **Casos con interacción entre servicios:** CU-AUT-01, CU-PER-02, CU-PER-05, CU-PER-07, CU-MOV-01 a CU-MOV-05, CU-MOV-08, CU-MOV-09, CU-SEG-01, CU-SEG-03 y CU-SUS-04, además del paso común de ubicar la base del usuario, que comparten todos los casos de los servicios de negocio.
