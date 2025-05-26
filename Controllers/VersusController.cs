using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]
public class VersusController : ControllerBase
{
  public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";

  [HttpPost("crear-juego/{liderIdCreador}")] // TODO: Validacion con IActionResult
  public IActionResult PostCrearJuego([FromRoute] int liderIdCreador, [FromQuery] int puntosMeta)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand crearJuegoCmd = new MySqlCommand("INSERT INTO juegos (puntos_meta, tipo_juego) VALUES (@puntosMeta, 'versus')", connection);
    crearJuegoCmd.Parameters.AddWithValue("@puntosMeta", puntosMeta);
    crearJuegoCmd.ExecuteNonQuery();

    int juegoId = Convert.ToInt32(crearJuegoCmd.LastInsertedId);

    MySqlCommand crearJugadorCmd = new MySqlCommand("insert into jugadores (lider_id, juego_id) values (@liderId, @juegoId)", connection);
    crearJugadorCmd.Parameters.AddWithValue("@liderId", liderIdCreador);
    crearJugadorCmd.Parameters.AddWithValue("@juegoId", juegoId);
    crearJugadorCmd.ExecuteNonQuery();

    int jugadorId = Convert.ToInt32(crearJugadorCmd.LastInsertedId);

    MySqlCommand updateCreadorCmd = new MySqlCommand("update juegos set creador = @jugadorId where juego_id = @juegoId", connection);
    updateCreadorCmd.Parameters.AddWithValue("@jugadorId", jugadorId);
    updateCreadorCmd.Parameters.AddWithValue("@juegoId", juegoId);
    updateCreadorCmd.ExecuteNonQuery();

    return Ok(new { JuegoId = juegoId });
  }

  [HttpGet("estatus-juegos/{liderId}")]
  public EstatusPartidas GetEstatusJuegos([FromRoute] int liderId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectEstatusCmd = new MySqlCommand(@"
    select 
      j2.juego_id, 
      j2.tipo_juego, 
      j2.jugador_en_turno, 
      j.jugador_id, 
      j2.creador, 
      j2.ganador
    from lideres l 
    join jugadores j on j.lider_id = l.lider_id
    join juegos j2 on j.juego_id = j2.juego_id 
    where l.lider_id = @liderId 
    ", connection);
    selectEstatusCmd.Parameters.AddWithValue("@liderId", liderId);

    EstatusPartidas estatusPartidas = new EstatusPartidas();

    using (var reader = selectEstatusCmd.ExecuteReader())
    {
      while (reader.Read())
      {
        int juegoId = Convert.ToInt32(reader["juego_id"]);

        int? jugadorEnTurno = reader["jugador_en_turno"] == DBNull.Value
        ? (int?)null
        : Convert.ToInt32(reader["jugador_en_turno"]);

        int jugadorId = Convert.ToInt32(reader["jugador_id"]);

        int creador = Convert.ToInt32(reader["creador"]);

        int? ganador = reader.IsDBNull(reader.GetOrdinal("ganador"))
        ? (int?)null
        : reader.GetInt32(reader.GetOrdinal("ganador"));

        string? tipoJuego = reader["tipo_juego"].ToString();

        if (tipoJuego != "versus") { continue; }

        if (ganador != null)
        {
          estatusPartidas.Terminado.Add(juegoId);
        }
        else if (jugadorEnTurno == jugadorId)
        {
          estatusPartidas.Turno.Add(juegoId);
        }
        else if (jugadorEnTurno != null && jugadorEnTurno != jugadorId)
        {
          estatusPartidas.NoTurno.Add(juegoId);
        }
        else if (jugadorEnTurno == null && jugadorId == creador)
        {
          estatusPartidas.NoIniciadoMios.Add(juegoId);
        }
        else if (jugadorEnTurno == null && jugadorId != creador)
        {
          estatusPartidas.NoIniciadoOtros.Add(new JuegoCreador(juegoId, creador));
        }
      }
    }
    connection.Close();
    return estatusPartidas;
  }

  [HttpPost("unirse-juego/{liderId}")]
  public IActionResult PostUnirseJuego([FromRoute] int liderId, [FromQuery] int juegoId)
  {
    // try
    // {
    using var connection = new MySqlConnection(ConnectionString);
    connection.Open();

    // Verificar existencia y duplicados
    var checkCmd = new MySqlCommand(@"
        SELECT 
          (SELECT COUNT(*) FROM lideres WHERE lider_id = @liderId) as liderExists,
          (SELECT COUNT(*) FROM juegos WHERE juego_id = @juegoId) as juegoExists,
          (SELECT COUNT(*) FROM jugadores WHERE lider_id = @liderId AND juego_id = @juegoId) as jugadorExists", connection);
    checkCmd.Parameters.AddWithValue("@liderId", liderId);
    checkCmd.Parameters.AddWithValue("@juegoId", juegoId);

    using (var reader = checkCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        if (reader.GetInt32("liderExists") == 0) return NotFound("El líder no existe");
        if (reader.GetInt32("juegoExists") == 0) return NotFound("El juego no existe");
        if (reader.GetInt32("jugadorExists") > 0) return BadRequest("Ya estas unido a este juego");
      }
    }


    // Unir jugador al juego
    var unirseCmd = new MySqlCommand("INSERT INTO jugadores (lider_id, juego_id) VALUES (@liderId, @juegoId)", connection);
    unirseCmd.Parameters.AddWithValue("@liderId", liderId);
    unirseCmd.Parameters.AddWithValue("@juegoId", juegoId);
    unirseCmd.ExecuteNonQuery();

    return Ok("Jugador unido exitosamente al juego");
    // }
    // catch (Exception ex)
    // {
    //   return StatusCode(500, $"Error interno del servidor: {ex.Message}");
    // }
  }

  [HttpPost("iniciar-juego/{juegoId}")]
  public void PostIniciarJuego(int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand selectJugadoresCmd = new MySqlCommand(@"
    select jugador_id from jugadores where juego_id = @juegoId;", connection);
    selectJugadoresCmd.Parameters.AddWithValue("juegoId", juegoId);

    List<int> jugadores = new List<int>();

    using (var reader = selectJugadoresCmd.ExecuteReader())
    {
      while (reader.Read())
      {
        jugadores.Add(Convert.ToInt32(reader["jugador_id"]));
      }
    }

    var random = new Random();
    var ordenNumeros = Enumerable.Range(1, jugadores.Count)
        .OrderBy(_ => random.Next())
        .ToList();

    var ordenJugadores = ordenNumeros
        .Zip(jugadores, (turno, jugadorId) => new { turno, jugadorId })
        .ToDictionary(x => x.turno, x => x.jugadorId);

    foreach (KeyValuePair<int, int> jugador in ordenJugadores)
    {
      MySqlCommand updateOrderCmd = new MySqlCommand("UPDATE jugadores SET turno = @turno WHERE jugador_id = @jugadorId", connection);
      updateOrderCmd.Parameters.AddWithValue("@turno", jugador.Key);
      updateOrderCmd.Parameters.AddWithValue("@jugadorId", jugador.Value);
      updateOrderCmd.ExecuteNonQuery();
    }

    var primerJugador = ordenJugadores[ordenJugadores.Keys.Min()];
    MySqlCommand updatePrimerJugador = new MySqlCommand(@"
    UPDATE juegos SET jugador_en_turno = @primerJugador WHERE juego_id = @juegoId;
    ", connection);
    updatePrimerJugador.Parameters.AddWithValue("@primerJugador", primerJugador);
    updatePrimerJugador.Parameters.AddWithValue("@juegoId", juegoId);
    updatePrimerJugador.ExecuteNonQuery();
  }


  [HttpPost("siguiente-turno/{juegoId}")]
  public void PostSiguienteTurno(int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand jugadorEnTurnoCmd = new MySqlCommand("SELECT jugador_en_turno FROM juegos WHERE juego_id = @juegoId", connection);
    jugadorEnTurnoCmd.Parameters.AddWithValue("juegoId", juegoId);

    int jugadorTurno = -1;
    using (var reader = jugadorEnTurnoCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        jugadorTurno = Convert.ToInt32(reader["jugador_en_turno"]);
      }
    }

    MySqlCommand jugadoresTurnosCmd = new MySqlCommand("SELECT jugador_id FROM jugadores WHERE juego_id = @juegoId ORDER BY turno ASC", connection);
    jugadoresTurnosCmd.Parameters.AddWithValue("juegoId", juegoId);

    List<int> jugadores = new List<int>();
    using (var reader = jugadoresTurnosCmd.ExecuteReader())
    {
      while (reader.Read())
      {
        jugadores.Add(Convert.ToInt32(reader["jugador_id"]));
      }
    }

    int JugadorTurnoIdx = jugadores.FindIndex(j => j == jugadorTurno);

    int sigJugadorId;
    if (JugadorTurnoIdx == jugadores.Count - 1)
    {
      sigJugadorId = jugadores[0];
    }
    else
    {
      sigJugadorId = jugadores[JugadorTurnoIdx + 1];
    }

    MySqlCommand updateJugadorTurnoCmd = new MySqlCommand(@"UPDATE juegos SET jugador_en_turno = @siguienteJugadorId WHERE juego_id = @juegoId", connection);
    updateJugadorTurnoCmd.Parameters.AddWithValue("siguienteJugadorId", sigJugadorId);
    updateJugadorTurnoCmd.Parameters.AddWithValue("juegoId", juegoId);
    updateJugadorTurnoCmd.ExecuteNonQuery();
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
    updateJugadorCmd.Parameters.AddWithValue("jugadorId", selectJugador(liderId, juegoId));
    updateJugadorCmd.Parameters.AddWithValue("aumentoMultiplicador", aumentoMultiplicador);
    updateJugadorCmd.ExecuteNonQuery();


    return Ok();
  }

  [HttpPost("respuesta-incorrecta/{liderId}")]
  public IActionResult RespuestaIncorrecta([FromRoute] int liderId, [FromQuery] int juegoId, [FromQuery] int preguntaId)
  {
    int jugador = selectJugador(liderId, juegoId);

    updateEstatusPregunta(jugador, preguntaId, false);

    return Ok();
  }


  private int _EstatusJuego(int juegoId)
  {
    MySqlConnection connection = new MySqlConnection(ConnectionString);
    connection.Open();

    MySqlCommand ganadoresCmd = new(@"
    select j.jugador_id from jugadores j
    join juegos j2 on j.juego_id = j2.juego_id
    where j2.juego_id = @juegoId and j.puntos_actuales >= 
    (select puntos_meta from juegos where juego_id = @juegoId)
    ", connection);
    ganadoresCmd.Parameters.AddWithValue("juegoId", juegoId);

    int ganador = -1;
    using (var reader = ganadoresCmd.ExecuteReader())
    {
      if (reader.Read())
      {
        ganador = Convert.ToInt32(reader["jugador_id"]);
      }
    }

    return ganador;
  }

  [HttpGet("estatus-juego/{juegoId}")]
  public IActionResult EstatusJuego(int juegoId)
  {
    return Ok(new { ganador = _EstatusJuego(juegoId) });
  }
}
