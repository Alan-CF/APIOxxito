using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
namespace  APIOxxito.Controllers;

[ApiController]
[Route("[controller]")]

public class PerfilRankingController : ControllerBase
{
    public string ConnectionString = "Server=mysql-373b7fe1-danielara071-6268.g.aivencloud.com;Port=24232;Database=mi_oxxito;Uid=avnadmin;Pwd=AVNS_ZJOL4SKtMmgE-f7N-_W;SslMode=none;";

    [HttpGet("GetRankings")]
    public List<PerfilRanking> GetRankings()
    {
        List<PerfilRanking> rankings = new List<PerfilRanking>();
        MySqlConnection conexion = new MySqlConnection(ConnectionString);
        conexion.Open();
        MySqlCommand cmd = new MySqlCommand(@"SELECT 
                    l.lider_id,
                    l.nombre AS lider_name,
                    SUM(j.puntos_actuales) AS puntaje_total,
                    ROW_NUMBER() OVER (ORDER BY SUM(j.puntos_actuales) DESC) AS global_rank,
                    s.nombre AS sede_name,
                    a.asesor_id as asesor
                    FROM 
                        lideres l
                    LEFT JOIN 
                        jugadores j ON l.lider_id = j.lider_id
                    INNER JOIN 
                        sedes s ON l.sede_id = s.sede_id
                    INNER JOIN
                    	asesores a on a.asesor_id = s.asesor_id
                    GROUP BY 
                        l.lider_id, l.nombre, s.nombre
                    ORDER BY 
                        puntaje_total DESC
                    LIMIT 10;", conexion);
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                PerfilRanking perfil = new PerfilRanking();
                perfil.Nombre = reader["lider_name"].ToString();
                perfil.Sede = reader["sede_name"].ToString();
                perfil.PuntosTotales = reader["puntaje_total"] is DBNull ? 0 : Convert.ToInt32(reader["puntaje_total"]);
                perfil.asesor = reader["asesor"] is DBNull ? 0 : Convert.ToInt32(reader["asesor"]);
                perfil.Ranking = reader["global_rank"] is DBNull ? 0 : Convert.ToInt32(reader["global_rank"]);
                rankings.Add(perfil);
            }
        }
        conexion.Close();
        return rankings;
    }

    [HttpGet("GetUserRank/{id_Lider}")]
    public UserRank GetRank(int? id_Lider)
    {
        UserRank rank = new UserRank();
        MySqlConnection conexion = new MySqlConnection(ConnectionString);
        conexion.Open();
        MySqlCommand cmd = new MySqlCommand(@"WITH leader_scores AS (
                        SELECT 
                            l.lider_id,
                            SUM(j.puntos_actuales) AS puntaje_total,
                            ROW_NUMBER() OVER (ORDER BY SUM(j.puntos_actuales) DESC) AS global_rank
                        FROM 
                            lideres l
                        LEFT JOIN 
                            jugadores j ON l.lider_id = j.lider_id
                        GROUP BY 
                            l.lider_id
                    )
                    SELECT 
                        global_rank,
                        puntaje_total AS puntos_totales
                    FROM 
                        leader_scores
                    WHERE 
                        lider_id = @id_lider", conexion);
        cmd.Parameters.AddWithValue("@id_lider", id_Lider);

        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                rank.userRank = reader["global_rank"] is DBNull ? 0 : Convert.ToInt32(reader["global_rank"]);
                rank.puntosTotales = reader["puntos_totales"] is DBNull ? 0 : Convert.ToInt32(reader["puntos_totales"]);
            }
        }
        conexion.Close();
        return rank;

    }
}