using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
  public string ConnectionString = "Server=127.0.0.1;Port=3306;Database=mi_oxxito;Uid=root;password=root;";

  [HttpGet()]
  public IActionResult Login([FromQuery] string usuario, [FromQuery] string contrasena)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand command = new MySqlCommand("SELECT lider_id FROM lideres WHERE usuario = @usuario AND contrasena = @contrasena", connection);
    command.Parameters.AddWithValue("@usuario", usuario);
    command.Parameters.AddWithValue("@contrasena", contrasena);

    MySqlDataReader reader = command.ExecuteReader();

    if (reader.Read())
    {
      return Ok(new { liderId = reader["lider_id"] });
    }

    return NotFound();
  }
}