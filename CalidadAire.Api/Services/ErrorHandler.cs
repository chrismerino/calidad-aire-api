using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalidadAire.Api.Services;

public class ApiException(int estado, string mensaje) : Exception(mensaje)
{
    public int Estado { get; } = estado;
}

public class ErrorHandler(ILogger<ErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var estado = exception is ApiException error ? error.Estado :
            exception is DbUpdateException ? 409 : 500;
        var mensaje = exception is ApiException ? exception.Message :
            estado == 409 ? "No se pudo guardar por un conflicto con los datos relacionados." :
            "No fue posible completar la operación.";

        if (estado == 500)
            logger.LogError(exception, "Error al procesar la solicitud.");
        context.Response.StatusCode = estado;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = estado,
            Title = mensaje
        }, ct);
        return true;
    }
}
