namespace CalidadAire.Api.Services;

public record ResultadoAlerta(string Nivel, string Mensaje);

public class AlertaService
{
    public ResultadoAlerta? Evaluar(decimal pm25, decimal pm10, decimal co2)
    {
        // Se devuelve un solo nivel, empezando por el de mayor gravedad.
        if (co2 > 5000 || pm25 > 250)
            return new("Extrema", "Nivel de contaminación extremadamente alto. Riesgo severo para la salud.");

        if (pm25 > 150 || pm10 > 200)
            return new("Critica", "La calidad del aire es peligrosa. Se recomienda permanecer en interiores y usar mascarilla.");

        if ((pm25 >= 51 && pm25 <= 100) || co2 > 1000)
            return new("Moderada", "La calidad del aire es poco saludable para grupos sensibles (niños, adultos mayores, personas con enfermedades respiratorias).");

        if (pm25 >= 25 && pm25 <= 50)
            return new("Leve", "La calidad del aire es moderada, se recomienda reducir actividades al aire libre prolongadas.");

        return null;
    }
}
