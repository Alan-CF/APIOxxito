public class Pregunta
{
  public int? PreguntaId { get; set; }
  public required string PreguntaTexto { get; set; }
  public required string Justificacion { get; set; }
  public required string OpcionCorrecta { get; set; }
  public required string Opcion2 { get; set; }
  public required string Opcion3 { get; set; }
  public required string Opcion4 { get; set; }
  public int Puntos { get; set; }
  public int Tiempo { get; set; }
}
