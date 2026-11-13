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
[Route("api/sensores")]
public class SensoresController(AireDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SensorRespuesta>> Crear(SensorDto datos, CancellationToken ct)
    {
        var sensor = new SensorCalidadAire { Ubicacion = datos.Ubicacion.Trim(), TipoGas = datos.TipoGas.Trim() };
        db.Sensores.Add(sensor);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = sensor.Id },
            new SensorRespuesta(sensor.Id, sensor.Ubicacion, sensor.TipoGas, sensor.Estado));
    }

    [HttpGet]
    public async Task<ActionResult<List<SensorRespuesta>>> Listar(bool? estado, CancellationToken ct)
    {
        var consulta = db.Sensores.AsNoTracking();
        if (estado.HasValue)
            consulta = consulta.Where(s => s.Estado == estado.Value);
        return Ok(await consulta.OrderBy(s => s.Id)
            .Select(s => new SensorRespuesta(s.Id, s.Ubicacion, s.TipoGas, s.Estado)).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SensorRespuesta>> Obtener(int id, CancellationToken ct)
    {
        var sensor = await db.Sensores.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new ApiException(404, "El sensor no existe.");
        return Ok(new SensorRespuesta(sensor.Id, sensor.Ubicacion, sensor.TipoGas, sensor.Estado));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoSensorDto datos, CancellationToken ct)
    {
        var sensor = await db.Sensores.FindAsync([id], ct)
            ?? throw new ApiException(404, "El sensor no existe.");
        sensor.Estado = datos.Estado!.Value;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
