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

    MySqlCommand command = new MySqlCommand("SELECT p.nombre_personaje ,p.monedas,p.imagen_url FROM personajes p where p.lider_id = @liderid;", connection);
    command.Parameters.AddWithValue("@liderid", liderId);
    InfoUsuario usuario = new InfoUsuario();

    MySqlDataReader reader = command.ExecuteReader();

    if (reader.Read())
    {
      usuario.nombre = reader["nombre_personaje"].ToString();
      usuario.monedas = Convert.ToInt32(reader["monedas"]);
      usuario.imagen_url = reader["imagen_url"].ToString();
    }

    return usuario;
  }

  [HttpPut]
  public void RestarMonedas(int liderId, int CantidadRestar)
  {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand updatemonedas = new(@"
    update personajes p
    set monedas = monedas- @CantidadRestar
    where lider_id= @liderId;
    ", connection);

    updatemonedas.Parameters.AddWithValue("liderId", liderId);
    updatemonedas.Parameters.AddWithValue("CantidadRestar", CantidadRestar);
    updatemonedas.ExecuteNonQuery();

  }
  [HttpGet("ListaMuebles")]
  public List<Mueble> GetListaMuebles([FromQuery] int liderId)
  {
    List<Mueble> myListaMueble = new List<Mueble>();
    MySqlConnection conexion = new MySqlConnection(ConnectionString);
    conexion.Open();
    /*************************************/
    MySqlCommand cmd = new MySqlCommand();
    cmd.CommandText = @"select om.oxxito_mobiliario_id,m.mueble_id ,m.precio,om.estado_desbloqueado,om.posicionVJ from oxxito_mobiliario om join oxxitos o on om.oxxito_id = o.oxxito_id  join mobiliario m on om.mueble_id = m.mueble_id where o.lider_id = @liderid";
    /**********************************/
    cmd.Parameters.AddWithValue("@liderid", liderId);
    cmd.Connection = conexion;
    Mueble mueble1 = new Mueble();
    using (var reader = cmd.ExecuteReader())
    {
      while (reader.Read())
      {
        mueble1 = new Mueble();
        mueble1.oxxito_mobiliario_id = Convert.ToInt32(reader["oxxito_mobiliario_id"]);
        mueble1.mueble_id = Convert.ToInt32(reader["mueble_id"]);
        mueble1.precio = Convert.ToInt32(reader["precio"]);
        mueble1.estado_desbloqueado = Convert.ToBoolean(reader["estado_desbloqueado"]);
        mueble1.posicionVJ = Convert.ToInt32(reader["posicionVJ"]);
        myListaMueble.Add(mueble1);
      }
    }
    conexion.Close();


    return myListaMueble;
  }
  [HttpPut("ActualizarEstadoMueble")]
  public void UpdateEstadoMueble(int oxxito_mobiliario_id,bool estado_desbloqueado)
  {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand updatemonedas = new(@"
    update oxxito_mobiliario om 
    set estado_desbloqueado = @estado_desbloqueado 
    where oxxito_mobiliario_id = @mueble_id
    ", connection);

    updatemonedas.Parameters.AddWithValue("@mueble_id", oxxito_mobiliario_id);
    updatemonedas.Parameters.AddWithValue("@estado_desbloqueado", estado_desbloqueado);
    updatemonedas.ExecuteNonQuery();
  }
  
}
