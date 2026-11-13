using System.ComponentModel.DataAnnotations;
using CalidadAire.Api.Data;
using CalidadAire.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalidadAire.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/alertas")]
public class AlertasController(AireDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AlertaRespuesta>>> Listar([Range(1, int.MaxValue)] int? sensorId,
        CancellationToken ct, [Range(1, 1000000)] int pagina = 1, [Range(1, 100)] int tamano = 50)
    {
        var consulta = db.Alertas.AsNoTracking();
        if (sensorId.HasValue)
            consulta = consulta.Where(a => a.SensorId == sensorId.Value);
        var alertas = await consulta.OrderByDescending(a => a.FechaHora).ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return Ok(alertas.Select(AlertaRespuesta.De));
    }
}
