using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class StoreController : ControllerBase
{
  public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";


  [HttpGet()]
  public InfoUsuario GetInfoUsuario([FromQuery] int liderId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand command = new MySqlCommand("SELECT j.puntos_actuales,p.monedas,p.imagen_url FROM jugadores j join lideres l on j.lider_id = l.lider_id join personajes p on l.lider_id = p.lider_id where l.lider_id = @liderid;", connection);
    command.Parameters.AddWithValue("@liderid", liderId);
    InfoUsuario usuario = new InfoUsuario();

    MySqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            usuario.puntos = Convert.ToInt32(reader["puntos_actuales"]);
            usuario.monedas = Convert.ToInt32(reader["monedas"]);
            usuario.imagen_url = reader["imagen_url"].ToString();
        }

    return usuario;
  }
}