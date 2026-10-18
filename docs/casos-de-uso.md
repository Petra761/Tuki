# Tuki: casos de uso

> Los diagramas están en `docs/diagramas/casos-de-uso/` (archivos `.puml`, uno por módulo).
> Esta tabla es la base para escribir las historias de usuario: cada código se convierte en una historia.

## Cómo leer este documento

**Actores**

- **Usuario:** la persona que administra sus finanzas. Es el único actor principal.
- **Usuario Premium:** especialización del Usuario. Hace todo lo que hace el Usuario y además puede cancelar su suscripción y usar las funciones premium.
- **Google** y **Proveedor de pago (Google Play):** actores externos, solo en los casos donde participan.

**El espacio del usuario (tenant).** Cada persona que se registra recibe su propio espacio, con una base de datos independiente donde viven sus cuentas, categorías y movimientos. El usuario pertenece a ese espacio y el sistema lo reconoce en cada petición. En los casos de uso este espacio no aparece como actor, sino como el contexto de la persona: nadie ve ni toca datos de otro espacio.

**Columna «Servicios».** Indica qué microservicios participan. Cuando aparece más de uno, el caso requiere interacción entre servicios. Además, todos los casos de Contabilidad pasan por el Gateway y consultan al servicio de Tenants para ubicar la base del usuario, y eso no se repite en cada fila.

**Columna «Nota».** Solo se llena cuando hay algo que aclarar: casos que dependen del tiempo, que son solo de diseño o que tienen valores por definir.

## 1. Acceso y seguridad

Diagramas: `01-acceso.puml` y `02-pin.puml`.

| Código    | Caso de uso                           | Objetivo                                                                                       | Actor principal                     | Servicios                                                                        | Nota                                                          |
| --------- | ------------------------------------- | ---------------------------------------------------------------------------------------------- | ----------------------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| CU-AUT-01 | Registrarse                           | Crear su cuenta con correo y contraseña y obtener su espacio personal para empezar a registrar | Usuario                             | Identidad → Tenants → Contabilidad (y Suscripciones para asignar el plan Gratis) | El plan Gratis es parte del diseño                            |
| CU-AUT-02 | Iniciar sesión                        | Entrar a su espacio con su correo y contraseña                                                 | Usuario                             | Identidad                                                                        |                                                               |
| CU-AUT-03 | Cerrar sesión                         | Salir de la app y cerrar su sesión en el dispositivo                                           | Usuario                             | Identidad                                                                        |                                                               |
| CU-AUT-04 | Iniciar sesión con Google             | Entrar sin escribir contraseña, usando su cuenta de Google                                     | Usuario (Google como actor externo) | Identidad                                                                        | Depende del tiempo                                            |
| CU-AUT-05 | Bloqueo por intentos fallidos         | Proteger la cuenta cuando se falla varias veces al iniciar sesión                              | Usuario                             | Identidad                                                                        | Valores por definir                                           |
| CU-AUT-06 | Verificar correo                      | Confirmar que el correo le pertenece                                                           | Usuario                             | Identidad                                                                        | Opcional para usar la app; obligatorio para contratar un plan |
| CU-AUT-07 | Recuperar contraseña                  | Volver a entrar cuando olvidó su contraseña                                                    | Usuario                             | Identidad                                                                        |                                                               |
| CU-AUT-08 | Solicitar código de recuperación      | Pedir que le llegue un código a su correo indicándolo                                          | Usuario                             | Identidad                                                                        | Paso incluido en CU-AUT-07                                    |
| CU-AUT-09 | Confirmar código del correo           | Ingresar el código que recibió para demostrar que tiene acceso al correo                       | Usuario                             | Identidad                                                                        | Paso incluido en CU-AUT-06 y CU-AUT-07                        |
| CU-AUT-10 | Definir nueva contraseña              | Elegir su nueva contraseña tras confirmar el código                                            | Usuario                             | Identidad                                                                        | Paso incluido en CU-AUT-07                                    |
| CU-AUT-11 | Configurar PIN                        | Activar un PIN para proteger la app al abrirla                                                 | Usuario                             | App móvil (local)                                                                | El PIN existe solo en el dispositivo                          |
| CU-AUT-12 | Cambiar PIN                           | Reemplazar el PIN actual por uno nuevo                                                         | Usuario                             | App móvil (local)                                                                |                                                               |
| CU-AUT-13 | Quitar PIN                            | Desactivar el bloqueo con PIN                                                                  | Usuario                             | App móvil (local)                                                                |                                                               |
| CU-AUT-14 | Desbloquear la app con PIN            | Abrir la app ingresando su PIN                                                                 | Usuario                             | App móvil (local)                                                                |                                                               |
| CU-AUT-15 | Restablecer PIN olvidado              | Recuperar el acceso cuando olvidó el PIN, volviendo a iniciar sesión con su contraseña         | Usuario                             | Identidad                                                                        |                                                               |
| CU-AUT-16 | Ingresar PIN actual                   | Demostrar que conoce el PIN antes de cambiarlo o quitarlo                                      | Usuario                             | App móvil (local)                                                                | Paso incluido en CU-AUT-12 y CU-AUT-13                        |
| CU-AUT-17 | Bloqueo por intentos fallidos del PIN | Proteger la app tras 5 intentos fallidos, bloqueándola 10 minutos                              | Usuario                             | App móvil (local)                                                                |                                                               |

## 2. Cuentas y personalización

Diagrama: `03-cuentas-personalizacion.puml`. Las personas (a quienes el usuario presta o debe dinero) son un tipo de cuenta, por eso se gestionan dentro de CU-PER-01.

| Código    | Caso de uso                          | Objetivo                                                                                                          | Actor principal | Servicios    | Nota                           |
| --------- | ------------------------------------ | ----------------------------------------------------------------------------------------------------------------- | --------------- | ------------ | ------------------------------ |
| CU-PER-01 | Gestionar cuentas                    | Crear, editar y archivar sus cuentas (efectivo, banco, cuentas digitales) y las personas con quienes tiene deudas | Usuario         | Contabilidad |                                |
| CU-PER-02 | Definir saldo inicial                | Indicar cuánto dinero tiene (o debe) una cuenta al empezar a usarla                                               | Usuario         | Contabilidad | Variante opcional de CU-PER-01 |
| CU-PER-03 | Gestionar tipos de cuenta            | Crear y archivar tipos de cuenta propios además de los predeterminados                                            | Usuario         | Contabilidad |                                |
| CU-PER-04 | Gestionar categorías y subcategorías | Adaptar las categorías a su forma de gastar: crear, renombrar y archivar                                          | Usuario         | Contabilidad |                                |
| CU-PER-05 | Gestionar atajos                     | Guardar sus gastos rutinarios para registrarlos con un solo toque                                                 | Usuario         | Contabilidad |                                |
| CU-PER-06 | Gestionar marcadores                 | Crear etiquetas libres para agrupar movimientos                                                                   | Usuario         | Contabilidad |                                |
| CU-PER-07 | Personalizar la app en el primer uso | Dejar lista la app con sus cuentas, categorías y atajos desde el comienzo                                         | Usuario         | Contabilidad | Depende del tiempo             |

## 3. Movimientos y atajos

Diagrama: `04-movimientos.puml`.

| Código    | Caso de uso                                   | Objetivo                                                                   | Actor principal | Servicios    | Nota                         |
| --------- | --------------------------------------------- | -------------------------------------------------------------------------- | --------------- | ------------ | ---------------------------- |
| CU-MOV-01 | Registrar gasto                               | Anotar dinero que consumió, saliendo de una cuenta propia o de una persona | Usuario         | Contabilidad |                              |
| CU-MOV-02 | Registrar ingreso                             | Anotar dinero nuevo que entra a una cuenta                                 | Usuario         | Contabilidad |                              |
| CU-MOV-03 | Registrar traslado                            | Mover dinero entre cuentas cuando nadie consumió nada                      | Usuario         | Contabilidad |                              |
| CU-MOV-04 | Abonar o cobrar una deuda                     | Pagar o cobrar una deuda, completa o en partes, con una persona            | Usuario         | Contabilidad | Tipo particular de CU-MOV-03 |
| CU-MOV-05 | Dividir en varias categorías o cuentas        | Repartir un mismo movimiento entre varias categorías o varias cuentas      | Usuario         | Contabilidad | Depende del tiempo           |
| CU-MOV-06 | Autocompletar al registrar                    | Recibir sugerencias de datos ya usados para registrar más rápido           | Usuario         | Contabilidad |                              |
| CU-MOV-07 | Ver historial de movimientos                  | Revisar lo que ha registrado                                               | Usuario         | Contabilidad |                              |
| CU-MOV-08 | Gestionar movimientos                         | Corregir o eliminar un movimiento ya registrado                            | Usuario         | Contabilidad |                              |
| CU-MOV-09 | Registrar con un atajo                        | Registrar un gasto rutinario con un solo toque                             | Usuario         | Contabilidad | Tipo particular de CU-MOV-01 |
| CU-MOV-10 | Deshacer o editar el monto tras usar un atajo | Corregir un atajo recién usado si se equivocó                              | Usuario         | Contabilidad |                              |

## 4. Seguimiento

Diagrama: `05-seguimiento.puml`.

| Código    | Caso de uso            | Objetivo                                                                                                                       | Actor principal | Servicios    | Nota                                    |
| --------- | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------ | --------------- | ------------ | --------------------------------------- |
| CU-SEG-01 | Ver resumen y métricas | Saber cómo van sus finanzas: saldo total y por cuenta, deudas pendientes, gasto del mes por categoría y avance de presupuestos | Usuario         | Contabilidad |                                         |
| CU-SEG-02 | Filtrar resultados     | Acotar lo que ve en el resumen o en el historial                                                                               | Usuario         | Contabilidad | Depende del tiempo; filtros por definir |
| CU-SEG-03 | Gestionar presupuestos | Fijar límites de gasto por categoría o en total, y mantenerlos                                                                 | Usuario         | Contabilidad |                                         |

## 5. Suscripciones

Diagrama: `06-suscripciones.puml`. Este módulo se diseña pero no se implementa en la beta.

| Código    | Caso de uso              | Objetivo                                                                      | Actor principal                                | Servicios                                                       | Nota                                    |
| --------- | ------------------------ | ----------------------------------------------------------------------------- | ---------------------------------------------- | --------------------------------------------------------------- | --------------------------------------- |
| CU-SUS-01 | Ver mi plan              | Saber qué plan tiene y qué incluye                                            | Usuario                                        | Suscripciones                                                   | Solo diseño                             |
| CU-SUS-02 | Contratar Premium        | Pasar al plan Premium para usar sus funciones                                 | Usuario                                        | Suscripciones ↔ Identidad (para comprobar el correo verificado) | Solo diseño                             |
| CU-SUS-03 | Cancelar suscripción     | Dejar de pagar Premium, conservando el acceso hasta el fin del período pagado | Usuario Premium                                | Suscripciones                                                   | Solo diseño                             |
| CU-SUS-04 | Usar una función premium | Aprovechar las funciones exclusivas de Premium                                | Usuario                                        | Contabilidad ↔ Suscripciones (verificar el plan)                | Solo diseño                             |
| CU-SUS-05 | Pagar suscripción        | Realizar el pago de la suscripción con el proveedor                           | Usuario (Proveedor de pago como actor externo) | Suscripciones                                                   | Solo diseño; paso incluido en CU-SUS-02 |

## Resumen

- **42 casos de uso** en 6 diagramas.
- **5 casos existen solo como pasos incluidos** en otros (CU-AUT-08, CU-AUT-09, CU-AUT-10, CU-AUT-16 y CU-SUS-05). Al escribir las historias de usuario se pueden agrupar con el caso que los usa.
- **Casos con interacción entre servicios:** CU-AUT-01, CU-SUS-02 y CU-SUS-04, además del paso común de ubicar la base del usuario que comparten todos los casos de Contabilidad.
