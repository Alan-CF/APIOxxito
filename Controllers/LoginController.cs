using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
  public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";


  [HttpGet()]
  public IActionResult Login([FromQuery] string usuario, [FromQuery] string contrasena)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand command = new MySqlCommand("SELECT l.lider_id,a.ingreso_id FROM lideres l left join actividadjuego a on l.lider_id=a.lider_id WHERE usuario = @usuario AND contrasena = @contrasena", connection);
    command.Parameters.AddWithValue("@usuario", usuario);
    command.Parameters.AddWithValue("@contrasena", contrasena);

    MySqlDataReader reader = command.ExecuteReader();

    if (reader.Read())
  {
    var liderId = reader["lider_id"];
    var actividadJuego = reader["ingreso_id"]== DBNull.Value ? 0 : Convert.ToInt32(reader["ingreso_id"]);

    return Ok(new { liderId, actividadJuego });
  }


    return NotFound();
  }
}