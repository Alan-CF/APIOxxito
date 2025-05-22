using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class PracticaController : ControllerBase
{
  public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";
  // public string ConnectionString = "Server=127.0.0.1;Port=3306;Database=mi_oxxito;Uid=root;password=root;";


  [HttpPost("crear-practica/{liderIdCreador}")]
  public IActionResult PostCrearPractica([FromRoute] int liderIdCreador)
  {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    // Crear juego de práctica
    MySqlCommand crearJuegoCmd = new MySqlCommand(
        "INSERT INTO juegos (puntos_meta, tipo_juego) VALUES (0, 'practica')", connection);
    crearJuegoCmd.ExecuteNonQuery();

    int juegoId = Convert.ToInt32(crearJuegoCmd.LastInsertedId);

    // Crear jugador asociado al líder y al juego
    MySqlCommand crearJugadorCmd = new MySqlCommand(
        "INSERT INTO jugadores (lider_id, juego_id) VALUES (@liderId, @juegoId)", connection);
    crearJugadorCmd.Parameters.AddWithValue("@liderId", liderIdCreador);
    crearJugadorCmd.Parameters.AddWithValue("@juegoId", juegoId);
    crearJugadorCmd.ExecuteNonQuery();

    int jugadorId = Convert.ToInt32(crearJugadorCmd.LastInsertedId);

    // Establecer al jugador como creador del juego
    MySqlCommand updateCreadorCmd = new MySqlCommand(
        "UPDATE juegos SET creador = @jugadorId WHERE juego_id = @juegoId", connection);
    updateCreadorCmd.Parameters.AddWithValue("@jugadorId", jugadorId);
    updateCreadorCmd.Parameters.AddWithValue("@juegoId", juegoId);
    updateCreadorCmd.ExecuteNonQuery();

    // Iniciar práctica: asignar turno = 1
    MySqlCommand updateTurnoCmd = new MySqlCommand(
        "UPDATE jugadores SET turno = 1 WHERE jugador_id = @jugadorId", connection);
    updateTurnoCmd.Parameters.AddWithValue("@jugadorId", jugadorId);
    updateTurnoCmd.ExecuteNonQuery();

    // Establecer jugador en turno en la tabla juegos
    MySqlCommand updateJuegoCmd = new MySqlCommand(
        "UPDATE juegos SET jugador_en_turno = @jugadorId WHERE juego_id = @juegoId", connection);
    updateJuegoCmd.Parameters.AddWithValue("@jugadorId", jugadorId);
    updateJuegoCmd.Parameters.AddWithValue("@juegoId", juegoId);
    updateJuegoCmd.ExecuteNonQuery();

    return Ok(new { JuegoID = juegoId });
  }

  [HttpPost("asignar-pregunta/{liderId}")]
  public Pregunta GetPregunta([FromRoute] int liderId, [FromQuery] int juegoId, [FromQuery] string categoriaPregunta)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectJugadorIdCmd = new MySqlCommand(@"
    select jugador_id 
    from lideres l 
    join jugadores j on j.lider_id = l.lider_id 
    join juegos j2 on j.juego_id = j2.juego_id
    where j.juego_id = @juegoId and 
    l.lider_id = @liderId and 
    j2.jugador_en_turno = j.jugador_id
    ", connection);
    selectJugadorIdCmd.Parameters.AddWithValue("juegoId", juegoId);
    selectJugadorIdCmd.Parameters.AddWithValue("liderId", liderId);

    int jugadorId = -1;
    using (var reader = selectJugadorIdCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        jugadorId = Convert.ToInt32(reader["jugador_id"]);
      }
    }

    MySqlCommand selectPregRandCmd = new MySqlCommand(@"
    select 
      p.pregunta_id, 
      p.pregunta, 
      p.justificacion, 
      p.opcion_correcta, 
      p.opcion_2, 
      p.opcion_3, 
      p.opcion_4,
      n.puntos,
      n.tiempo
    from preguntas p
    join nivelespreguntas n on p.nivel_id = n.nivel_id
    join categoriaspreguntas c on c.categoria_id = p.categoria_id
    where p.pregunta_id not in ( 
      select pregunta_id  
      from pregunta_jugador pj
      join jugadores j on pj.jugador_id = j.jugador_id
      join lideres l on l.lider_id = j.lider_id
      where l.lider_id = @liderId and j.juego_id = @juegoId
    ) and 
    c.categoria = @categoriaPregunta
    ORDER BY RAND()
    LIMIT 1;
    ", connection);
    selectPregRandCmd.Parameters.AddWithValue("liderId", liderId);
    selectPregRandCmd.Parameters.AddWithValue("juegoId", juegoId);
    selectPregRandCmd.Parameters.AddWithValue("categoriaPregunta", categoriaPregunta);

    Pregunta pregunta = new();

    using (var reader = selectPregRandCmd.ExecuteReader())
    {
      if (reader.Read())
      {

        pregunta.PreguntaId = Convert.ToInt32(reader["pregunta_id"]);
        pregunta.PreguntaTexto = reader.GetString("pregunta");
        pregunta.Justificacion = reader.GetString("justificacion");
        pregunta.OpcionCorrecta = reader.GetString("opcion_correcta");
        pregunta.Opcion2 = reader.GetString("opcion_2");
        pregunta.Opcion3 = reader.GetString("opcion_3");
        pregunta.Opcion4 = reader.GetString("opcion_4");
        pregunta.Puntos = Convert.ToInt32(reader["puntos"]);
        pregunta.Tiempo = Convert.ToInt32(reader["tiempo"]);

      }
    }

    MySqlCommand insertPreguntaJugadorCmd = new("insert into pregunta_jugador (pregunta_id, jugador_id) values (@preguntaId, @jugadorId)", connection);
    insertPreguntaJugadorCmd.Parameters.AddWithValue("preguntaId", pregunta.PreguntaId);
    insertPreguntaJugadorCmd.Parameters.AddWithValue("jugadorId", jugadorId);
    insertPreguntaJugadorCmd.ExecuteNonQuery();

    return pregunta;
  }

  private int selectJugador(int liderId, int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectJugadorCmd = new(@"
    select j.jugador_id from lideres l
    join jugadores j on j.lider_id = l.lider_id
    join juegos j2 on j2.juego_id = j.juego_id
    where l.lider_id = @liderId and
    j2.juego_id = @juegoId
    ", connection);
    selectJugadorCmd.Parameters.AddWithValue("liderId", liderId);
    selectJugadorCmd.Parameters.AddWithValue("juegoId", juegoId);

    int jugador = -1;
    using (var reader = selectJugadorCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        jugador = Convert.ToInt32(reader["jugador_id"]);
      }
    }

    return jugador;
  }

  private void updateEstatusPregunta(int jugador, int preguntaId, bool correcta)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand updateStatusPreguntaCmd = new(@"
    update pregunta_jugador 
    set correcta = @correcta 
    where jugador_id = @jugadorId and
    pregunta_id = @preguntaId
    ", connection);
    updateStatusPreguntaCmd.Parameters.AddWithValue("jugadorId", jugador);
    updateStatusPreguntaCmd.Parameters.AddWithValue("preguntaId", preguntaId);
    updateStatusPreguntaCmd.Parameters.AddWithValue("correcta", correcta);
    updateStatusPreguntaCmd.ExecuteNonQuery();
  }

  // 1. Asigna la respuesta como correcta
  // 2. Añade los puntos y el aumento de multiplicador
  [HttpPost("respuesta-correcta/{liderId}")]
  public IActionResult PreguntaCorrecta([FromRoute] int liderId, [FromQuery] int juegoId, [FromQuery] int preguntaId, [FromQuery] float aumentoMultiplicador)
  {
    int jugador = selectJugador(liderId, juegoId);

    updateEstatusPregunta(jugador, preguntaId, true);

    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectPuntosPregunta = new(@"
    select n.puntos from preguntas p 
    join nivelespreguntas n on p.nivel_id = n.nivel_id
    where p.pregunta_id = @preguntaId;
    ", connection);
    selectPuntosPregunta.Parameters.AddWithValue("preguntaId", preguntaId);

    int puntos = 0;
    using (var reader = selectPuntosPregunta.ExecuteReader())
    {
      if (reader.Read())
      {
        puntos = Convert.ToInt32(reader["puntos"]);
      }
    }

    MySqlCommand updateJugadorCmd = new(@"
    update jugadores set 
      puntos_actuales = puntos_actuales + @puntos,
      multiplicador = multiplicador + @aumentoMultiplicador
    where jugador_id = @jugadorId;
    ", connection);
    updateJugadorCmd.Parameters.AddWithValue("puntos", puntos);
    updateJugadorCmd.Parameters.AddWithValue("aumentoMultiplicador", aumentoMultiplicador);
    updateJugadorCmd.Parameters.AddWithValue("jugadorId", jugador);
    updateJugadorCmd.ExecuteNonQuery();


    return Ok();
  }


  private IActionResult TerminarJuego(int jugador, int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand terminarJuegoCmd = new(@"update juegos set ganador = @jugador where juego_id = @juegoId;", connection);
    terminarJuegoCmd.Parameters.AddWithValue("jugador", jugador);
    terminarJuegoCmd.Parameters.AddWithValue("juegoId", juegoId);

    terminarJuegoCmd.ExecuteNonQuery();

    MySqlCommand putMonedasCmd = new(@"
    UPDATE personajes p
    join lideres l on l.lider_id = p.lider_id
    join jugadores j3 on j3.lider_id = l.lider_id
    SET monedas = monedas + (
        SELECT j.puntos_actuales
        FROM jugadores j
        JOIN juegos j2 ON j2.juego_id = j.juego_id
        WHERE j.jugador_id = @jugadorId
    ) / 10
    WHERE j3.jugador_id = @jugadorId;
    ", connection);

    putMonedasCmd.Parameters.AddWithValue("jugadorId", jugador);
    putMonedasCmd.ExecuteNonQuery();

    return Ok(new { StatusMsg = "Juego Terminado" });
  }


  [HttpPost("respuesta-incorrecta/{liderId}")]
  public IActionResult PreguntaIncorrecta([FromRoute] int liderId, [FromQuery] int juegoId, [FromQuery] int preguntaId)
  {
    int jugador = selectJugador(liderId, juegoId);

    updateEstatusPregunta(jugador, preguntaId, false);


    return TerminarJuego(jugador, juegoId);
  }

  [HttpGet("puntos-jugador/lider/{liderId}/juego/{juegoId}")]
  public IActionResult SelectPuntosJugador(int liderId, int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectJugadorCmd = new(@"
    select j.puntos_actuales from lideres l
    join jugadores j on j.lider_id = l.lider_id
    join juegos j2 on j2.juego_id = j.juego_id
    where l.lider_id = @liderId and
    j2.juego_id = @juegoId
    ", connection);
    selectJugadorCmd.Parameters.AddWithValue("liderId", liderId);
    selectJugadorCmd.Parameters.AddWithValue("juegoId", juegoId);

    int puntos = 0;
    using (var reader = selectJugadorCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        puntos = Convert.ToInt32(reader["puntos_actuales"]);
      }
    }

    return Ok(new { Puntos = puntos });
  }
}

