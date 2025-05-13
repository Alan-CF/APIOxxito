namespace APIOxxito;
public class Pregunta
{
  public int? PreguntaId { get; set; }
  public string? PreguntaTexto { get; set; }
  public string? Justificacion { get; set; }
  public string? OpcionCorrecta { get; set; }
  public string? Opcion2 { get; set; }
  public string? Opcion3 { get; set; }
  public string? Opcion4 { get; set; }
  public int Puntos { get; set; }
  public int Tiempo { get; set; }
}
