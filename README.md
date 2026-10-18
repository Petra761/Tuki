# Tuki

Aplicación de registro contable personal. Este repositorio contiene el backend
(microservicios en .NET), la app móvil (React Native) y la documentación.

> Este README es un adelanto: por ahora solo explica el certificado HTTPS.
> Los demás apartados (requisitos, cómo arrancar, estructura) se completan
> más adelante.

## Certificado HTTPS de desarrollo

Todos los servicios del backend escuchan solo por HTTPS en local
(`https://localhost:5000` y de `5101` a `5104`). Para que los servicios y el
navegador confíen en el certificado de desarrollo hay que instalarlo **una vez
por equipo**.

Requisito: tener instalado el SDK de .NET.

### Windows

Abre PowerShell y ejecuta:

    dotnet dev-certs https --trust

Windows muestra una ventana pidiendo confirmar la instalación del
certificado: acéptala.

### Linux (Ubuntu)

En Linux el comando anterior no basta, porque el certificado debe quedar en el
almacén del sistema. Ejecuta en una terminal:

    dotnet dev-certs https --clean
    dotnet dev-certs https --export-path ~/aspnet-dev-localhost.crt --format Pem --no-password
    sudo cp ~/aspnet-dev-localhost.crt /usr/local/share/ca-certificates/aspnet-dev-localhost.crt
    sudo update-ca-certificates

### Comprobar que funcionó

Con un servicio en ejecución, esta llamada no debe mostrar errores de
certificado (reemplaza el puerto por el del servicio que estés probando):

    curl -I https://localhost:5102

### Si algo falla

- **Error de certificado entre servicios:** repite los pasos de tu sistema
  operativo. Si regeneras el certificado (`--clean`), hay que reinstalarlo.
- **El certificado venció:** el de desarrollo dura un año. Se renueva
  repitiendo los mismos pasos.
- **La app móvil:** no usa este certificado. Su conexión con el Gateway se
  explicará en el apartado de la app móvil.
