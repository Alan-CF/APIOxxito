using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class PersonajeController : ControllerBase
{
  public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";
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

    int monedas = 0;
    using (var reader = getMonedasCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        monedas = Convert.ToInt32(reader["monedas"]);
      }
    }
    return Ok(new { Monedas = monedas });
  }
  [HttpPut]
  public void SumarMonedas(int liderId,int NuevaMoneda)
  {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand updatemonedas = new(@"
    update personajes p
    set monedas = monedas+ @NuevaMoneda
    where lider_id= @liderId;
    ", connection);

    updatemonedas.Parameters.AddWithValue("liderId", liderId);
    updatemonedas.Parameters.AddWithValue("NuevaMoneda", NuevaMoneda);
    updatemonedas.ExecuteNonQuery();
    
  }

}