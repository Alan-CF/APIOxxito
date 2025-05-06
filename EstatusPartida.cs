namespace APIOxxito;

public class JuegoCreador
{
  public int JuegoId { get; set; }
  public int Creador { get; set; }

  public JuegoCreador(int juegoId, int creador)
  {
    JuegoId = juegoId;
    Creador = creador;
  }
}

public class EstatusPartidas
{
  public List<int> Turno { get; set; } = new();
  public List<int> NoTurno { get; set; } = new();
  public List<int> NoIniciadoMios { get; set; } = new();
  public List<int> Terminado { get; set; } = new();
  public List<JuegoCreador> NoIniciadoOtros { get; set; } = new();
}