namespace APIOxxito;

public class Mueble
{
    public int oxxito_mobiliario_id { get; set; } // 🔑 Lo necesitas para la API
    public int mueble_id { get; set; }            // Esto es el tipo de mueble
    public int precio { get; set; }
    public bool estado_desbloqueado { get; set; }
}
