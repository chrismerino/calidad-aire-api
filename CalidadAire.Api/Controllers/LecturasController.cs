using System.ComponentModel.DataAnnotations;
using CalidadAire.Api.Data;
using CalidadAire.Api.DTOs;
using CalidadAire.Api.Models;
using CalidadAire.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalidadAire.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lecturas")]
public class LecturasController(AireDbContext db, AlertaService alertas) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegistroLecturaRespuesta>> Registrar(LecturaDto datos, CancellationToken ct)
    {
        var sensor = await db.Sensores.FindAsync([datos.SensorId], ct)
            ?? throw new ApiException(404, "El sensor no existe.");
        if (!sensor.Estado)
            throw new ApiException(409, "El sensor está inactivo.");
        var lectura = new LecturaAire
        {
            SensorId = datos.SensorId, PM2_5 = datos.PM2_5!.Value, PM10 = datos.PM10!.Value,
            CO2 = datos.CO2!.Value, FechaHora = datos.FechaHora!.Value.UtcDateTime
        };
        db.Lecturas.Add(lectura);
        var nivel = alertas.Evaluar(lectura.PM2_5, lectura.PM10, lectura.CO2);
        AlertaAire? alerta = null;
        if (nivel is not null)
        {
            alerta = new AlertaAire { SensorId = lectura.SensorId, Nivel = nivel.Nivel,
                Mensaje = nivel.Mensaje, FechaHora = lectura.FechaHora };
            db.Alertas.Add(alerta);
        }
        await db.SaveChangesAsync(ct);
        var resultado = new RegistroLecturaRespuesta(LecturaRespuesta.De(lectura), alerta is null ? null : AlertaRespuesta.De(alerta));
        return CreatedAtAction(nameof(Obtener), new { id = lectura.Id }, resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LecturaRespuesta>> Obtener(int id, CancellationToken ct)
    {
        var lectura = await db.Lecturas.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new ApiException(404, "La lectura no existe.");
        return Ok(LecturaRespuesta.De(lectura));
    }

    [HttpGet]
    public async Task<IActionResult> Listar(DateTimeOffset? desde, DateTimeOffset? hasta,
        string? contaminante, [Range(1, int.MaxValue)] int? sensorId, CancellationToken ct,
        [Range(1, 1000000)] int pagina = 1, [Range(1, 100)] int tamano = 50)
    {
        if ((desde.HasValue && (desde.Value.UtcDateTime.Ticks % 10 != 0 || desde.Value.UtcDateTime.Year < 1000)) ||
            (hasta.HasValue && (hasta.Value.UtcDateTime.Ticks % 10 != 0 || hasta.Value.UtcDateTime.Year < 1000)))
            throw new ApiException(400, "Las fechas deben estar en el rango de MySQL y admitir hasta seis decimales de segundo.");
        if (desde.HasValue && hasta.HasValue && desde > hasta)
            throw new ApiException(400, "La fecha inicial no puede ser posterior a la final.");
        contaminante = contaminante?.Trim().ToUpperInvariant().Replace("PM2.5", "PM2_5");
        if (contaminante is not null and not "PM2_5" and not "PM10" and not "CO2")
            throw new ApiException(400, "Usa PM2_5, PM10 o CO2 como contaminante.");

        var consulta = db.Lecturas.AsNoTracking();
        if (sensorId.HasValue)
            consulta = consulta.Where(l => l.SensorId == sensorId.Value);
        if (desde.HasValue)
        {
            var inicio = desde.Value.UtcDateTime;
            consulta = consulta.Where(l => l.FechaHora >= inicio);
        }
        if (hasta.HasValue)
        {
            var fin = hasta.Value.UtcDateTime;
            consulta = consulta.Where(l => l.FechaHora <= fin);
        }
        var total = await consulta.CountAsync(ct);
        var lecturas = await consulta.OrderByDescending(l => l.FechaHora).ThenByDescending(l => l.Id)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return Ok(new { total, pagina, tamano, datos = lecturas.Select(l => LecturaRespuesta.De(l, contaminante)) });
    }

}
