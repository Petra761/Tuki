using System.Net;

namespace Tuki.Reportes.Services;

public sealed class ReportDataSourceException(
    string source,
    HttpStatusCode? statusCode,
    Exception? innerException = null
) : Exception($"No se pudo consultar el servicio {source}.", innerException)
{
    public string ServiceName { get; } = source;

    public HttpStatusCode? StatusCode { get; } = statusCode;
}
