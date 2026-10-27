using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tuki.Autenticacion.OpenApi;

// El documento "publico" expone las rutas sin el segmento /autenticacion/ para
// que el Gateway las enrute como /api/auth/login, /api/users/register, etc.,
// ocultando qué microservicio las sirve.
// El documento "v1" permanece intacto para que la documentación interna siga siendo completa.
public sealed class SegmentoPublicoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct
    )
    {
        var rutas = document.Paths.ToList();
        document.Paths.Clear();

        foreach (var (ruta, item) in rutas)
        {
            var rutaPublica = ruta.Replace(
                "/autenticacion",
                string.Empty,
                StringComparison.Ordinal
            );
            document.Paths[rutaPublica] = item;
        }

        return Task.CompletedTask;
    }
}
