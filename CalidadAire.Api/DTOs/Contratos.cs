using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CalidadAire.Api.Models;

namespace CalidadAire.Api.DTOs;

public class SensorDto
{
    [Required, StringLength(150)]
    public string Ubicacion { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string TipoGas { get; set; } = string.Empty;
}

public class EstadoSensorDto
{
    [Required]
    public bool? Estado { get; set; }
}

public record SensorRespuesta(int Id, string Ubicacion, string TipoGas, bool Estado);

public class LoginDto
{
    [Required, StringLength(80)]
    public string Usuario { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string Contrasena { get; set; } = string.Empty;
}

public class LecturaDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int SensorId { get; set; }
    [Required, Range(typeof(decimal), "0", "99999999.9999", ParseLimitsInInvariantCulture = true)]
    [JsonPropertyName("pm2_5")]
    public decimal? PM2_5 { get; set; }
    [Required, Range(typeof(decimal), "0", "99999999.9999", ParseLimitsInInvariantCulture = true)]
    [JsonPropertyName("pm10")]
    public decimal? PM10 { get; set; }
    [Required, Range(typeof(decimal), "0", "99999999.9999", ParseLimitsInInvariantCulture = true)]
    [JsonPropertyName("co2")]
    public decimal? CO2 { get; set; }
    [Required]
    public DateTimeOffset? FechaHora { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (new[] { PM2_5, PM10, CO2 }.Any(valor => valor.HasValue && decimal.Round(valor.Value, 4) != valor.Value))
            yield return new ValidationResult("Las mediciones admiten hasta cuatro decimales.", [nameof(PM2_5), nameof(PM10), nameof(CO2)]);
        if (FechaHora.HasValue && FechaHora.Value.UtcDateTime.Year < 1000)
            yield return new ValidationResult("La fecha está fuera del rango admitido.", [nameof(FechaHora)]);
        if (FechaHora.HasValue && FechaHora.Value.UtcDateTime.Ticks % 10 != 0)
            yield return new ValidationResult("La fecha admite hasta seis decimales de segundo.", [nameof(FechaHora)]);
    }
}

public record LecturaRespuesta(int Id, int SensorId,
    [property: JsonPropertyName("pm2_5"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PM2_5,
    [property: JsonPropertyName("pm10"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PM10,
    [property: JsonPropertyName("co2"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CO2,
    DateTime FechaHora)
{
    public static LecturaRespuesta De(LecturaAire lectura, string? contaminante = null) => new(
        lectura.Id, lectura.SensorId,
        contaminante is null or "PM2_5" ? lectura.PM2_5 : null,
        contaminante is null or "PM10" ? lectura.PM10 : null,
        contaminante is null or "CO2" ? lectura.CO2 : null,
        DateTime.SpecifyKind(lectura.FechaHora, DateTimeKind.Utc));
}

public record AlertaRespuesta(int Id, int SensorId, string Nivel, string Mensaje, DateTime FechaHora)
{
    public static AlertaRespuesta De(AlertaAire alerta) => new(alerta.Id, alerta.SensorId, alerta.Nivel,
        alerta.Mensaje, DateTime.SpecifyKind(alerta.FechaHora, DateTimeKind.Utc));
}

public record RegistroLecturaRespuesta(LecturaRespuesta Lectura, AlertaRespuesta? Alerta);
