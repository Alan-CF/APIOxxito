using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class PersonajeController : ControllerBase
{
  public string ConnectionString = "Server=127.0.0.1;Port=3306;Database=mi_oxxito;Uid=root;password=root;";

  [HttpGet("monedas/{liderId}")]
  public IActionResult GetMonedas(int liderId)
  {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand getMonedasCmd = new(@"
    select p.monedas from personajes p
    join lideres l on l.lider_id = p.lider_id
    where l.lider_id = @liderId;
    ", connection);

    getMonedasCmd.Parameters.AddWithValue("liderId", liderId);

    int puntos = 0;
    using (var reader = getMonedasCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        puntos = Convert.ToInt32(reader["monedas"]);
      }
    }
    return Ok(new { Puntos = puntos });
  }
}