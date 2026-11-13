using System.Globalization;
using System.Text.Json;

namespace CalidadAire.Api.Services;

public record ClimaRespuesta(decimal Temperatura, decimal Humedad, DateTime FechaHora,
    string Fuente = "Open-Meteo", string Tipo = "Actual");

public class ClimaService(HttpClient cliente, IConfiguration config)
{
    public async Task<ClimaRespuesta> Obtener(CancellationToken ct)
    {
        var latitud = config.GetValue<double>("Clima:Latitud");
        var longitud = config.GetValue<double>("Clima:Longitud");
        var ruta = FormattableString.Invariant($"v1/forecast?latitude={latitud}&longitude={longitud}&current=temperature_2m,relative_humidity_2m&timezone=UTC");
        try
        {
            using var respuesta = await cliente.GetAsync(ruta, ct);
            if (!respuesta.IsSuccessStatusCode)
                throw new ApiException(502, "El servicio de clima no está disponible.");
            using var contenido = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (contenido.RootElement.ValueKind != JsonValueKind.Object ||
                !contenido.RootElement.TryGetProperty("current", out var actual) || actual.ValueKind != JsonValueKind.Object ||
                !actual.TryGetProperty("temperature_2m", out var temperatura) || temperatura.ValueKind != JsonValueKind.Number || !temperatura.TryGetDecimal(out var grados) ||
                !actual.TryGetProperty("relative_humidity_2m", out var humedad) || humedad.ValueKind != JsonValueKind.Number || !humedad.TryGetDecimal(out var porcentaje) ||
                porcentaje < 0 || porcentaje > 100 ||
                !actual.TryGetProperty("time", out var hora) || hora.ValueKind != JsonValueKind.String ||
                !DateTime.TryParse(hora.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fecha))
                throw new ApiException(502, "El servicio de clima devolvió datos incompletos.");
            return new(grados, porcentaje, fecha);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(504, "El servicio de clima superó el tiempo de espera.");
        }
        catch (HttpRequestException)
        {
            throw new ApiException(502, "No fue posible conectar con el servicio de clima.");
        }
        catch (JsonException)
        {
            throw new ApiException(502, "El servicio de clima devolvió una respuesta inválida.");
        }
    }
}
