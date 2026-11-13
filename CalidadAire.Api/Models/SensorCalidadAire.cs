namespace CalidadAire.Api.Models;

public class SensorCalidadAire
{
    public int Id { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string TipoGas { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;
    public List<LecturaAire> Lecturas { get; set; } = [];
    public List<AlertaAire> Alertas { get; set; } = [];
}
