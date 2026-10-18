# Tuki: historias de usuario

> Cada historia agrupa uno o más casos de uso de `docs/casos-de-uso.md`.
> Los detalles de qué debe incluir cada historia están en sus criterios de aceptación.
> La prioridad de cada historia se define en el documento de priorización, no aquí.

## Cómo leer este documento

- **Épica:** cada módulo de la aplicación es una épica. Si se traslada a Jira, cada épica se crea como _Epic_ y sus historias como _Story_ dentro de ella.
- **Formato:** Como [rol], quiero [funcionalidad] para [beneficio].
- **SP (puntos de historia):** esfuerzo relativo de construir la historia completa, backend y app móvil. Escala: 1 muy simple, 2 simple, 3 media, 5 compleja, 8 muy compleja, 13 la más compleja del proyecto.
- **Módulo responsable:** el microservicio que guarda la información y aplica la regla. El PIN vive solo en la app móvil.
- **Criterios de aceptación:** cada uno se puede comprobar con una prueba manual de la app o una petición a la API. Entre todos describen lo que la historia debe incluir para considerarse terminada.

## Épicas

| Épica                           | Módulo                              | Historias        | SP      |
| ------------------------------- | ----------------------------------- | ---------------- | ------- |
| EP-01 Acceso y seguridad        | Identidad (y app móvil para el PIN) | TK-01 a TK-06    | 44      |
| EP-02 Cuentas y personalización | Contabilidad                        | TK-07 a TK-10    | 21      |
| EP-03 Movimientos y atajos      | Contabilidad                        | TK-11 a TK-15    | 34      |
| EP-04 Seguimiento               | Contabilidad                        | TK-16 a TK-18    | 18      |
| EP-05 Suscripciones             | Suscripciones (solo diseño)         | TK-19 a TK-21    | 18      |
| **Total**                       |                                     | **21 historias** | **135** |

---

## EP-01 · Acceso y seguridad

Registro, inicio de sesión, recuperación de la cuenta y protección de la app con PIN.

### TK-01 · Registrarse y crear mi espacio

**Como** usuario nuevo, **quiero** registrarme con mi correo y una contraseña, **para** empezar a llevar mis finanzas en un espacio propio y separado de los demás.

- Casos de uso: CU-AUT-01
- Módulo responsable: Identidad (con Tenants y Contabilidad)
- SP: 13

**Criterios de aceptación**

- [ ] Con un correo válido y una contraseña de al menos 8 caracteres, se crea la cuenta y el usuario entra a la app sin pasos adicionales.
- [ ] Si el correo ya está registrado, se muestra un error y no se crea una segunda cuenta.
- [ ] Si el correo no tiene formato válido o la contraseña es demasiado corta, se indica qué campo corregir.
- [ ] Al terminar el registro, el espacio ya contiene la cuenta «Efectivo» y el conjunto base de categorías, listos para registrar un movimiento, y el usuario queda en el plan Gratis.
- [ ] El espacio es independiente: un segundo usuario registrado después no ve ningún dato del primero.
- [ ] Si falla la creación del espacio, no queda un registro a medias: se muestra un error y el usuario puede reintentar con el mismo correo.

### TK-02 · Iniciar y cerrar sesión

**Como** usuario, **quiero** entrar a mi cuenta y salir de ella de forma segura, **para** que solo yo pueda acceder a mis datos.

- Casos de uso: CU-AUT-02, CU-AUT-03, CU-AUT-05
- Módulo responsable: Identidad
- SP: 8

**Criterios de aceptación**

- [ ] **Iniciar sesión:** con correo y contraseña correctos, el usuario entra y ve la pantalla principal con sus datos.
- [ ] Con datos incorrectos se muestra «correo o contraseña incorrectos», sin indicar cuál de los dos falló.
- [ ] La sesión se mantiene al cerrar y volver a abrir la app, hasta que el usuario cierre sesión.
- [ ] Si el usuario inicia sesión en otro dispositivo, la sesión del dispositivo anterior deja de funcionar.
- [ ] **Cerrar sesión:** al cerrar sesión, la app vuelve a la pantalla de inicio y no se vuelve a entrar sin credenciales.
- [ ] Una petición a la API con la sesión ya cerrada es rechazada.
- [ ] **Bloqueo por intentos fallidos (provisional):** tras 5 intentos fallidos seguidos no se puede iniciar sesión durante 10 minutos, aunque se use la contraseña correcta.
- [ ] La pantalla indica cuánto falta para poder intentarlo otra vez, y un inicio de sesión correcto antes del quinto fallo reinicia el contador.

### TK-03 · Iniciar sesión con Google

**Como** usuario, **quiero** entrar con mi cuenta de Google, **para** no tener que recordar otra contraseña.

- Casos de uso: CU-AUT-04
- Módulo responsable: Identidad
- SP: 5

**Criterios de aceptación**

- [ ] Si el correo de Google no tiene cuenta en Tuki, se crea la cuenta con su espacio y el usuario entra.
- [ ] Si ya existe una cuenta con ese correo, el usuario entra a ella y no se crea un duplicado.
- [ ] Si el usuario cancela el cuadro de Google, vuelve a la pantalla de inicio sin errores.
- [ ] Una misma cuenta de Google no puede quedar vinculada a dos usuarios distintos.

### TK-04 · Verificar mi correo

**Como** usuario, **quiero** verificar mi correo, **para** poder contratar un plan.

- Casos de uso: CU-AUT-06, CU-AUT-09
- Módulo responsable: Identidad
- SP: 5

**Criterios de aceptación**

- [ ] El usuario puede usar toda la app sin haber verificado su correo.
- [ ] Desde su perfil puede pedir un código, que llega por correo con 6 dígitos.
- [ ] Con el código correcto, el correo queda marcado como verificado y el perfil lo muestra.
- [ ] Un código incorrecto o vencido (más de 15 minutos) no verifica nada, se muestra un mensaje y se puede pedir uno nuevo.
- [ ] Un código ya usado no sirve una segunda vez.

### TK-05 · Recuperar mi contraseña

**Como** usuario que olvidó su contraseña, **quiero** restablecerla con un código enviado a mi correo, **para** volver a entrar a mi cuenta.

- Casos de uso: CU-AUT-07, CU-AUT-08, CU-AUT-09, CU-AUT-10
- Módulo responsable: Identidad
- SP: 5

**Criterios de aceptación**

- [ ] Desde la pantalla de inicio de sesión, al indicar su correo, el usuario recibe un código de 6 dígitos.
- [ ] Si el correo no está registrado, se muestra el mismo mensaje que si lo estuviera, sin revelar si existe la cuenta.
- [ ] Con el código correcto y vigente (15 minutos), el usuario puede escribir una contraseña nueva de al menos 8 caracteres.
- [ ] Después del cambio, la contraseña anterior ya no funciona y la nueva sí.
- [ ] Con un código incorrecto, vencido o ya usado, la contraseña no cambia y se puede pedir otro código.

### TK-06 · Proteger la app con un PIN

**Como** usuario, **quiero** proteger mi app con un PIN que pueda activar, cambiar y quitar, **para** que nadie pueda abrirla aunque tenga mi teléfono.

- Casos de uso: CU-AUT-11, CU-AUT-12, CU-AUT-13, CU-AUT-14, CU-AUT-15, CU-AUT-16, CU-AUT-17
- Módulo responsable: App móvil (con Identidad para «Olvidé mi PIN»)
- SP: 8

**Criterios de aceptación**

- [ ] **Configurar:** el usuario crea un PIN de 4 dígitos escribiéndolo dos veces; si no coinciden, no se guarda y se avisa.
- [ ] **Privacidad:** el PIN no viaja al servidor; no aparece en ninguna petición de red.
- [ ] **Desbloquear:** con el PIN activo, al abrir la app aparece primero la pantalla de PIN, sin mostrar ningún dato. Con el PIN correcto se entra; con uno incorrecto se muestra un error.
- [ ] **Cambiar:** se pide el PIN actual; si es incorrecto no se puede continuar. Con el correcto se define el nuevo escribiéndolo dos veces, y desde ese momento el anterior ya no abre la app.
- [ ] **Quitar:** se pide el PIN actual; una vez quitado, la app abre sin pedir PIN.
- [ ] **Bloqueo:** tras 5 intentos fallidos seguidos, la app no acepta ningún PIN durante 10 minutos, aunque sea el correcto. Se muestra cuánto falta, un PIN correcto antes del quinto fallo reinicia el contador, y cerrar y volver a abrir la app no reinicia el bloqueo.
- [ ] **Olvidé mi PIN:** la pantalla de PIN ofrece esa opción. Al elegirla se pide iniciar sesión con correo y contraseña; si es correcto, el PIN se elimina y el usuario entra pudiendo configurar uno nuevo; si falla, el PIN no se elimina.

---

## EP-02 · Cuentas y personalización

Cuentas, personas, categorías, marcadores y atajos: todo lo que el usuario adapta a su medida.

### TK-07 · Gestionar mis cuentas y personas

**Como** usuario, **quiero** crear y mantener mis cuentas y las personas con las que tengo deudas, **para** saber cuánto dinero tengo y quién me debe o a quién le debo.

- Casos de uso: CU-PER-01, CU-PER-02, CU-PER-03
- Módulo responsable: Contabilidad
- SP: 8

**Criterios de aceptación**

- [ ] **Cuentas:** el usuario puede crear una cuenta con nombre y tipo, y la cuenta «Efectivo» existe desde el primer uso.
- [ ] Puede editar el nombre y el tipo de una cuenta. Al archivarla, deja de ofrecerse al registrar, pero sus movimientos siguen visibles en el historial.
- [ ] **Tipos de cuenta:** existen los tipos efectivo, tarjeta bancaria, cuenta digital, otro y persona. El usuario puede crear tipos propios y archivar cualquiera: un tipo archivado deja de ofrecerse, pero las cuentas que ya lo usan siguen funcionando.
- [ ] Siempre queda al menos un tipo de persona disponible, para poder seguir registrando deudas.
- [ ] **Personas:** una persona es una cuenta de tipo persona y aparece en la lista con su saldo: positivo significa que le debe al usuario y negativo, que el usuario le debe.
- [ ] **Saldo inicial:** al crear una cuenta se puede indicar un saldo inicial, que si se deja vacío es 0. En una persona puede ser positivo o negativo.
- [ ] El saldo mostrado de la cuenta es igual al saldo inicial. Este no se suma a los ingresos del mes ni a los reportes de gastos, y si el usuario lo corrige después, el saldo se ajusta.

### TK-08 · Gestionar mis categorías y marcadores

**Como** usuario, **quiero** adaptar las categorías y crear marcadores a mi forma de gastar, **para** ver mis finanzas con el detalle que me interesa.

- Casos de uso: CU-PER-04, CU-PER-06
- Módulo responsable: Contabilidad
- SP: 5

**Criterios de aceptación**

- [ ] **Categorías:** desde el primer uso existe un conjunto base de categorías de gastos y de ingresos.
- [ ] El usuario puede crear categorías y subcategorías, con un máximo de dos niveles: una subcategoría no puede tener subcategorías.
- [ ] Puede renombrar y archivar cualquier categoría, incluso las predeterminadas. Una categoría archivada deja de ofrecerse al registrar, pero los movimientos anteriores la conservan.
- [ ] Una categoría de ingreso no aparece al registrar un gasto, y una de gasto no aparece al registrar un ingreso.
- [ ] **Marcadores:** el usuario puede crear un marcador con un nombre, sin repetir nombres, y renombrarlo o archivarlo.
- [ ] Un marcador archivado deja de ofrecerse al registrar, pero los movimientos que ya lo tenían lo conservan.

### TK-09 · Gestionar mis atajos

**Como** usuario, **quiero** guardar mis gastos rutinarios como atajos, **para** registrarlos después con un solo toque.

- Casos de uso: CU-PER-05
- Módulo responsable: Contabilidad
- SP: 3

**Criterios de aceptación**

- [ ] El usuario puede crear un atajo con nombre, monto, categoría y cuenta.
- [ ] Los atajos aparecen en la pantalla principal en el orden que el usuario eligió.
- [ ] Se puede editar o archivar un atajo; uno archivado deja de aparecer.
- [ ] Editar o archivar un atajo no modifica los gastos que ya se registraron con él.

### TK-10 · Personalizar la app en el primer uso

**Como** usuario nuevo, **quiero** que la app me guíe para configurar mis cuentas, categorías y atajos, **para** empezar a registrar con todo a mi medida.

- Casos de uso: CU-PER-07
- Módulo responsable: Contabilidad
- SP: 5

**Criterios de aceptación**

- [ ] Tras registrarse, la app guía al usuario por tres pasos cortos: cuentas, categorías y atajos.
- [ ] En cada paso el usuario puede aceptar lo predeterminado o cambiarlo.
- [ ] Al terminar, el usuario llega a la pantalla principal con lo que configuró.
- [ ] La guía aparece solo la primera vez.

---

## EP-03 · Movimientos y atajos

Registrar, consultar y corregir el dinero que entra, sale o se mueve.

### TK-11 · Registrar gastos e ingresos

**Como** usuario, **quiero** registrar lo que gasto y lo que gano de forma rápida, **para** saber en qué se va mi dinero y cuánto tengo.

- Casos de uso: CU-MOV-01, CU-MOV-02, CU-MOV-06
- Módulo responsable: Contabilidad
- SP: 8

**Criterios de aceptación**

- [ ] Con monto, cuenta y categoría, el gasto se guarda y el saldo de la cuenta baja ese monto; en un ingreso, el saldo sube.
- [ ] El monto debe ser mayor que 0; con 0 o vacío, no se guarda y se indica el error.
- [ ] La fecha por defecto es hoy y se puede cambiar. La descripción y los marcadores son opcionales.
- [ ] Bajo cada tipo de movimiento aparece una frase guía corta que ayuda a elegir el correcto, por ejemplo «¿Consumiste algo?» bajo gasto y «¿Entró dinero nuevo?» bajo ingreso.
- [ ] Si el gasto lo pagó una persona, el usuario la elige como cuenta y el saldo de esa persona baja.
- [ ] Sin salir de la pantalla de registro, el usuario puede crear una categoría o un marcador nuevo y guardar el gasto como atajo.
- [ ] **Autocompletar:** al escribir en la descripción aparecen sugerencias de descripciones usadas antes. Al elegir una, se rellenan la categoría y la cuenta que el usuario usó la última vez con ella. Las sugerencias salen solo de sus propios movimientos y se pueden ignorar.
- [ ] El movimiento aparece en el historial y suma al gasto o al ingreso del mes de su categoría.

### TK-12 · Registrar traslados y deudas

**Como** usuario, **quiero** mover dinero entre mis cuentas y registrar lo que presto, debo o me pagan, **para** mantener mis saldos y mis deudas al día.

- Casos de uso: CU-MOV-03, CU-MOV-04
- Módulo responsable: Contabilidad
- SP: 5

**Criterios de aceptación**

- [ ] El usuario elige cuenta de origen, cuenta de destino y monto: el origen baja y el destino sube ese mismo monto. No se pide categoría y no se puede elegir la misma cuenta como origen y destino.
- [ ] Bajo el tipo traslado aparece una frase guía corta, por ejemplo «¿Solo cambió de lugar tu dinero?».
- [ ] Un traslado entre dos cuentas propias no cambia el saldo total, no suma a los gastos ni a los ingresos del mes, y se distingue de ellos en el historial.
- [ ] **Prestar:** un traslado hacia una persona sube su saldo, que significa que ahora le debe al usuario.
- [ ] **Abonar o cobrar:** desde una persona, el usuario elige «me pagó» o «le pagué» e indica un monto. Si un amigo debe 40 Bs y paga 15 Bs, su saldo pasa a 25 Bs y la cuenta del usuario sube 15 Bs.
- [ ] Si el pago iguala la deuda, el saldo de la persona queda en 0. Cada pago aparece en el historial como traslado.

### TK-13 · Dividir un movimiento en varias categorías o cuentas

**Como** usuario, **quiero** repartir un movimiento entre varias categorías o varias cuentas, **para** registrar con exactitud las compras mixtas.

- Casos de uso: CU-MOV-05
- Módulo responsable: Contabilidad
- SP: 8

**Criterios de aceptación**

- [ ] El usuario puede dividir un gasto o un ingreso en varias líneas, cada una con categoría y monto.
- [ ] La pantalla muestra el total como suma de las líneas y no permite guardar si alguna línea es 0.
- [ ] Con una compra de 60 Bs repartida en 45 Bs de «Comida» y 15 Bs de «Limpieza», cada categoría suma solo su parte en el gasto del mes.
- [ ] Un mismo movimiento puede pagarse desde dos cuentas y cada una baja solo su parte.
- [ ] El historial muestra un solo movimiento con el total.

### TK-14 · Consultar y corregir mis movimientos

**Como** usuario, **quiero** revisar lo que he registrado y corregirlo si me equivoqué, **para** que mis datos reflejen la realidad.

- Casos de uso: CU-MOV-07, CU-MOV-08
- Módulo responsable: Contabilidad
- SP: 8

**Criterios de aceptación**

- [ ] **Historial:** los movimientos aparecen del más reciente al más antiguo, con fecha, tipo, descripción, monto y categoría. Los traslados se distinguen de los gastos y los ingresos.
- [ ] La lista carga de 30 en 30: los siguientes aparecen al desplazarse hacia abajo.
- [ ] Si todavía no hay movimientos, se muestra un mensaje que invita a registrar el primero.
- [ ] **Editar:** desde el historial, el usuario abre un movimiento y puede cambiar monto, cuenta, categoría, fecha y descripción. Los saldos se recalculan.
- [ ] **Eliminar:** se pide confirmación y, una vez eliminado, los saldos vuelven a como estaban antes del movimiento.
- [ ] Un traslado se edita o se elimina completo, afectando a sus dos cuentas.

### TK-15 · Registrar con atajos

**Como** usuario, **quiero** registrar un gasto rutinario con un solo toque y poder corregirlo enseguida, **para** no perder tiempo en lo que repito todos los días.

- Casos de uso: CU-MOV-09, CU-MOV-10
- Módulo responsable: Contabilidad
- SP: 5

**Criterios de aceptación**

- [ ] Al tocar un atajo se registra de inmediato un gasto con su monto, categoría y cuenta, con fecha de hoy y sin pedir confirmación.
- [ ] El gasto aparece en el historial y el saldo de la cuenta baja.
- [ ] Si la cuenta del atajo está archivada, no se registra el gasto y se avisa al usuario.
- [ ] Después de tocar el atajo aparece, durante 5 segundos, un aviso con las opciones «Deshacer» y «Editar monto».
- [ ] «Deshacer» elimina el gasto y el saldo vuelve a como estaba. «Editar monto» permite cambiarlo y el saldo se ajusta a la diferencia. Si no se toca ninguna opción, el gasto queda registrado.

---

## EP-04 · Seguimiento

Saber cómo van las finanzas: resumen, filtros y presupuestos.

### TK-16 · Ver el resumen y las métricas de mis finanzas

**Como** usuario, **quiero** ver cómo van mis finanzas apenas abro la app, **para** tomar decisiones con información clara.

- Casos de uso: CU-SEG-01
- Módulo responsable: Contabilidad
- SP: 8

**Criterios de aceptación**

- [ ] La pantalla principal muestra el saldo total y el saldo de cada cuenta.
- [ ] Las deudas pendientes aparecen separadas: quién le debe al usuario y a quién le debe el usuario, con sus montos.
- [ ] Se muestra el gasto del mes en curso por categoría, y lo registrado en una subcategoría suma también a su categoría padre.
- [ ] Se muestra el avance de cada presupuesto: lo gastado frente al límite.
- [ ] Las cifras coinciden con la suma de los movimientos del historial.
- [ ] Al tocar una cuenta o una categoría, se ven sus movimientos.
- [ ] Si el usuario aún no tiene movimientos, se muestran los valores en 0 y un mensaje que lo invita a registrar el primero.

### TK-17 · Filtrar resultados

**Como** usuario, **quiero** filtrar lo que veo, **para** analizar un período, una cuenta o una categoría en particular.

- Casos de uso: CU-SEG-02
- Módulo responsable: Contabilidad
- SP: 5
- Nota: los filtros concretos siguen por definir; estos criterios son provisionales.

**Criterios de aceptación**

- [ ] El usuario puede filtrar el resumen y el historial por período.
- [ ] Puede combinar el período con cuenta, categoría o marcador.
- [ ] Los totales y las listas cambian de acuerdo con los filtros activos.
- [ ] Si no hay resultados, se muestra un mensaje y la opción de quitar los filtros.
- [ ] Al quitar los filtros, se vuelve a ver todo.

### TK-18 · Gestionar mis presupuestos

**Como** usuario, **quiero** fijar límites de gasto, **para** controlar cuánto gasto por categoría o en total.

- Casos de uso: CU-SEG-03
- Módulo responsable: Contabilidad
- SP: 5

**Criterios de aceptación**

- [ ] El usuario puede crear un presupuesto mensual para una categoría o para el gasto total.
- [ ] No puede haber dos presupuestos mensuales para la misma categoría.
- [ ] El gasto del presupuesto se calcula con los gastos del mes; si es de una categoría padre, incluye sus subcategorías.
- [ ] Cuando el gasto supera el límite, el resumen lo marca como excedido.
- [ ] El usuario puede editar el límite y archivar el presupuesto.

---

## EP-05 · Suscripciones

Planes Gratis y Premium. Este módulo se diseña pero no se implementa en la beta.

### TK-19 · Consultar y cancelar mi suscripción

**Como** usuario, **quiero** saber qué plan tengo y poder cancelarlo, **para** tener control de lo que pago.

- Casos de uso: CU-SUS-01, CU-SUS-03
- Módulo responsable: Suscripciones
- SP: 5

**Criterios de aceptación**

- [ ] El usuario ve su plan actual (Gratis o Premium) y las funciones que incluye. Todo usuario nuevo empieza en el plan Gratis.
- [ ] Si es Premium, ve hasta qué fecha está pagado; si está en período de prueba o de gracia, se indica con su fecha de fin.
- [ ] La opción de cancelar solo aparece si el usuario es Premium y pide confirmación.
- [ ] Tras cancelar, el usuario conserva Premium hasta el fin del período ya pagado, y se muestra esa fecha.
- [ ] Al llegar esa fecha, el plan pasa a Gratis y el usuario conserva todos sus datos; solo se limitan las funciones premium.

### TK-20 · Contratar Premium

**Como** usuario, **quiero** contratar el plan Premium, **para** usar sus funciones exclusivas.

- Casos de uso: CU-SUS-02, CU-SUS-05
- Módulo responsable: Suscripciones (con Identidad)
- SP: 8

**Criterios de aceptación**

- [ ] El usuario ve el precio mensual y el anual y elige uno.
- [ ] Si su correo no está verificado, se le pide verificarlo antes de poder pagar.
- [ ] El pago se realiza con el proveedor de pago; si falla o el usuario cancela, sigue en el plan Gratis.
- [ ] Con el pago exitoso, el plan pasa a Premium de inmediato y el usuario puede usar las funciones premium.
- [ ] Contratar el plan no modifica ni borra ningún dato del usuario.

### TK-21 · Usar las funciones premium

**Como** usuario Premium, **quiero** usar las funciones exclusivas de mi plan, **para** aprovechar lo que pago.

- Casos de uso: CU-SUS-04
- Módulo responsable: Contabilidad (con Suscripciones)
- SP: 5

**Criterios de aceptación**

- [ ] Un usuario Premium puede usar las funciones premium sin restricciones.
- [ ] Si un usuario Gratis intenta usar una, la función no se ejecuta y se le ofrece contratar Premium.
- [ ] El control lo hace el servidor: una petición directa a una función premium, sin plan Premium, es rechazada aunque la app no muestre el botón.
