using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Clasificación local: 8 tiempos por tamaño, sin aplicar ventajas ni penalizaciones por pistas.
public static class KakuroRanking
{
    private const string ClaveRanking = "Kakuro.V1.RankingTop8";
    private const int MaximosPorModo = 8;

    [Serializable]
    private sealed class DatosRanking
    {
        public int version = 1;
        public long[] facil = new long[0];
        public long[] medio = new long[0];
        public long[] dificil = new long[0];
    }

    // Formato del historial anterior. El archivo original no se modifica.
    [Serializable]
    private sealed class RegistroAnterior
    {
        public int version;
        public int tamano;
        public long tiempoMilisegundos;
    }

    public static bool Obtener(int tamano, out List<long> tiempos, out string error)
    {
        tiempos = new List<long>(); error = null;
        try
        {
            DatosRanking datos = CargarDatos();
            tiempos.AddRange(ArregloPorTamano(datos, tamano));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool Agregar(int tamano, long milisegundos, out string error)
    {
        error = null;
        try
        {
            if (milisegundos < 0) throw new ArgumentOutOfRangeException("milisegundos");
            DatosRanking datos = CargarDatos();
            var lista = new List<long>(ArregloPorTamano(datos, tamano));
            lista.Add(milisegundos);
            lista.Sort();
            if (lista.Count > MaximosPorModo) lista.RemoveRange(MaximosPorModo, lista.Count - MaximosPorModo);
            if (tamano == 7) datos.facil = lista.ToArray();
            else if (tamano == 9) datos.medio = lista.ToArray();
            else if (tamano == 12) datos.dificil = lista.ToArray();
            PlayerPrefs.SetString(ClaveRanking, JsonUtility.ToJson(datos));
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static long[] ArregloPorTamano(DatosRanking datos, int tamano)
    {
        if (tamano == 7) return datos.facil;
        if (tamano == 9) return datos.medio;
        if (tamano == 12) return datos.dificil;
        throw new ArgumentOutOfRangeException("tamano");
    }

    private static DatosRanking CargarDatos()
    {
        if (PlayerPrefs.HasKey(ClaveRanking))
        {
            string json = PlayerPrefs.GetString(ClaveRanking, "");
            if (!json.Contains("\"version\"") || !json.Contains("\"facil\"") ||
                !json.Contains("\"medio\"") || !json.Contains("\"dificil\""))
                throw new InvalidDataException("Faltan datos en el ranking; no se ha sobrescrito.");
            var datos = JsonUtility.FromJson<DatosRanking>(json);
            if (datos == null || datos.version != 1 || datos.facil == null || datos.medio == null || datos.dificil == null ||
                datos.facil.Length > MaximosPorModo || datos.medio.Length > MaximosPorModo || datos.dificil.Length > MaximosPorModo)
                throw new InvalidDataException("El archivo de ranking no tiene un formato válido; no se ha sobrescrito.");
            RevisarTiempos(datos.facil); RevisarTiempos(datos.medio); RevisarTiempos(datos.dificil);
            Array.Sort(datos.facil); Array.Sort(datos.medio); Array.Sort(datos.dificil);
            return datos;
        }
        return ImportarHistorialAnterior();
    }

    private static void RevisarTiempos(long[] tiempos)
    {
        foreach (long valor in tiempos)
            if (valor < 0) throw new InvalidDataException("El ranking contiene un tiempo inválido; no se ha sobrescrito.");
    }

    private static DatosRanking ImportarHistorialAnterior()
    {
        var faciles = new List<long>(); var medios = new List<long>(); var dificiles = new List<long>();
        string ruta = Path.Combine(Application.persistentDataPath, "Kakuro_partidas_v1.jsonl");
        int ignorados = 0;
        if (File.Exists(ruta))
            foreach (string linea in File.ReadLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                try
                {
                    if (!linea.Contains("\"tiempoMilisegundos\"")) { ignorados++; continue; }
                    var dato = JsonUtility.FromJson<RegistroAnterior>(linea);
                    if (dato == null || dato.version != 1 || dato.tiempoMilisegundos < 0 ||
                        (dato.tamano != 7 && dato.tamano != 9 && dato.tamano != 12)) { ignorados++; continue; }
                    List<long> lista = dato.tamano == 7 ? faciles : (dato.tamano == 9 ? medios : dificiles);
                    lista.Add(dato.tiempoMilisegundos);
                }
                catch (Exception) { ignorados++; }
            }
        if (ignorados > 0) Debug.LogWarning("Kakuro: líneas inválidas ignoradas al leer el historial anterior: " + ignorados);
        return new DatosRanking { version = 1, facil = Mejores(faciles), medio = Mejores(medios), dificil = Mejores(dificiles) };
    }

    private static long[] Mejores(List<long> lista)
    {
        lista.Sort();
        if (lista.Count > MaximosPorModo) lista.RemoveRange(MaximosPorModo, lista.Count - MaximosPorModo);
        return lista.ToArray();
    }

    public static string FormatearTiempo(long milisegundos)
    {
        long total = Math.Max(0, milisegundos) / 1000;
        long horas = total / 3600;
        long minutos = (total / 60) % 60;
        long segundos = total % 60;
        long milis = Math.Max(0, milisegundos) % 1000;
        return horas > 0 ? string.Format("{0:00}:{1:00}:{2:00}.{3:000}", horas, minutos, segundos, milis)
                         : string.Format("{0:00}:{1:00}.{2:000}", minutos, segundos, milis);
    }
}
