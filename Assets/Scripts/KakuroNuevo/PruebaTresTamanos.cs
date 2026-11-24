using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KakuroNuevo;
using Debug = UnityEngine.Debug;

public sealed class PruebaTresTamanos : MonoBehaviour
{
    private sealed class Informe { public string Texto; public bool Correcto; }

    [ContextMenu("Probar tres tamanos")]
    private void Probar() { StartCoroutine(Ejecutar()); }

    private IEnumerator Ejecutar()
    {
        Debug.Log("Kakuro: comprobando 7x7, 9x9 y 12x12 en segundo plano...");
        int semilla = Environment.TickCount;
        Task<Informe> tarea = Task.Run(() => EjecutarPruebas(semilla));
        while (!tarea.IsCompleted) yield return null;
        if (tarea.IsFaulted) { Debug.LogError("Kakuro: error en la prueba. " + tarea.Exception); yield break; }
        if (tarea.Result.Correcto) Debug.Log(tarea.Result.Texto); else Debug.LogError(tarea.Result.Texto);
    }

    private static Informe EjecutarPruebas(int semilla)
    {
        int[] tamanos = { 7, 7, 7, 9, 9, 9, 12, 12, 12 };
        int[] familias = { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
        int[] intentos = { 20, 20, 20, 12, 12, 12, 12, 12, 12 };
        var log = new StringBuilder();
        var firmas = new HashSet<string>();
        bool correcto = true;
        for (int i = 0; i < tamanos.Length; i++)
        {
            var reloj = Stopwatch.StartNew();
            int semillaActual = unchecked(semilla + i);
            var resultado = new GeneradorKakuro(tamanos[i], semillaActual).Generar(intentos[i], 3000, familias[i]);
            var verificacion = new SolucionadorKakuro().Analizar(resultado.Tablero, 50000);
            reloj.Stop();
            int blancas = 0;
            for (int f = 0; f < resultado.Tablero.Filas; f++)
                for (int c = 0; c < resultado.Tablero.Columnas; c++)
                    if (resultado.Tablero.EsBlanca(f, c)) blancas++;
            if (verificacion.Estado != EstadoSolucion.SolucionUnica) correcto = false;
            firmas.Add(tamanos[i] + ":" + GeneradorKakuro.Firma(resultado.Tablero));
            log.AppendLine(tamanos[i] + "x" + tamanos[i] + " familia " + resultado.Familia +
                ": " + verificacion.Estado + "; blancas=" + blancas + "; candidatos=" + resultado.CandidatosProbados +
                "; cambios=" + resultado.CambiosAceptados + "; igualAlInicial=" + resultado.IgualAlInicial +
                "; tiempoEditor=" + reloj.ElapsedMilliseconds + "ms; semilla=" + semillaActual);
        }
        log.AppendLine("Tableros distintos por tamaño, pistas y posición: " + firmas.Count + "/" + tamanos.Length + ".");
        return new Informe { Correcto = correcto, Texto = log.ToString() };
    }
}
