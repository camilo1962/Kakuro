using System;
using System.Collections.Generic;
using System.Text;

namespace KakuroNuevo
{
    public sealed class ResultadoGeneracionFacil
    {
        public TableroKakuro Tablero { get; internal set; }
        public int CandidatosProbados { get; internal set; }
        public int CambiosAceptados { get; internal set; }
        public bool IgualAlInicial { get; internal set; }
        public int Familia { get; internal set; }
    }

    // Modo fácil experimental: pistas de una cifra y solución única.
    // No utiliza API de Unity. No compartir una instancia entre hilos.
    public sealed class GeneradorKakuroFacil
    {
        private const int Tamano = 7;
        private readonly Random aleatorio;
        private readonly SolucionadorKakuro solver = new SolucionadorKakuro();
        private readonly bool[,] blancas = new bool[Tamano, Tamano];
        private readonly int[,] valores = new int[Tamano, Tamano];
        private readonly List<Tuple<int, int>> casillas = new List<Tuple<int, int>>();

        public GeneradorKakuroFacil(int semilla) { aleatorio = new Random(semilla); }

        public ResultadoGeneracionFacil Generar(int maxCandidatos = 60, int maxNodosSolucionador = 3000, int? familia = null)
        {
            if (maxCandidatos < 0) throw new ArgumentOutOfRangeException("maxCandidatos");
            if (maxNodosSolucionador < 1) throw new ArgumentOutOfRangeException("maxNodosSolucionador");
            if (familia.HasValue && (familia.Value < 1 || familia.Value > 4))
                throw new ArgumentOutOfRangeException("familia");
            int familiaElegida = familia ?? aleatorio.Next(1, 5);
            PrepararPlantilla(familiaElegida);
            TableroKakuro inicial = CrearTablero();
            if (!PistasDeUnaCifra(inicial) || solver.Analizar(inicial, 50000).Estado != EstadoSolucion.SolucionUnica)
                throw new InvalidOperationException("No se pudo confirmar la unicidad de la plantilla inicial.");
            string firmaInicial = Firma(inicial);
            int probados = 0, aceptados = 0;
            for (int intento = 0; intento < maxCandidatos; intento++)
            {
                var celda = casillas[aleatorio.Next(casillas.Count)];
                int f = celda.Item1, c = celda.Item2, anterior = valores[f, c];
                var opciones = new List<int>();
                for (int n = 1; n <= 9; n++)
                    if (n != anterior && Admite(f, c, n)) opciones.Add(n);
                if (opciones.Count == 0) continue;
                valores[f, c] = opciones[aleatorio.Next(opciones.Count)];
                TableroKakuro candidato = CrearTablero();
                probados++;
                if (PistasDeUnaCifra(candidato) && solver.Analizar(candidato, maxNodosSolucionador).Estado == EstadoSolucion.SolucionUnica)
                    aceptados++;
                else
                    valores[f, c] = anterior;
            }
            TableroKakuro final = CrearTablero();
            return new ResultadoGeneracionFacil { Tablero = final, CandidatosProbados = probados,
                CambiosAceptados = aceptados, IgualAlInicial = Firma(final) == firmaInicial, Familia = familiaElegida };
        }

        private void PrepararPlantilla(int familia)
        {
            Array.Clear(blancas, 0, blancas.Length); Array.Clear(valores, 0, valores.Length); casillas.Clear();
            string[][] plantillas = {
                new[] { "##25##", "#412##", "41#153", "521#14", "##512#", "##26##" },
                new[] { "##54##", "##134#", "412#16", "25#432", "#261##", "##23##" },
                new[] { "13##24", "72#432", "#421##", "##621#", "251#54", "12##21" },
                new[] { "43##35", "215#21", "##314#", "#512##", "52#431", "31##52" }
            };
            string[] filas = plantillas[familia - 1];
            bool intercambiar = aleatorio.Next(2) == 1, invertirF = aleatorio.Next(2) == 1;
            bool invertirC = aleatorio.Next(2) == 1;
            for (int f = 0; f < 6; f++)
                for (int c = 0; c < 6; c++)
                {
                    char ch = filas[f][c];
                    if (ch == '#') continue;
                    int r = intercambiar ? c : f, col = intercambiar ? f : c;
                    if (invertirF) r = 5 - r;
                    if (invertirC) col = 5 - col;
                    int numero = ch - '0';
                    blancas[r + 1, col + 1] = true;
                    valores[r + 1, col + 1] = numero;
                    casillas.Add(Tuple.Create(r + 1, col + 1));
                }
        }

        private bool Admite(int fila, int columna, int numero)
        {
            for (int c = columna - 1; c >= 0 && blancas[fila, c]; c--) if (valores[fila, c] == numero) return false;
            for (int c = columna + 1; c < Tamano && blancas[fila, c]; c++) if (valores[fila, c] == numero) return false;
            for (int f = fila - 1; f >= 0 && blancas[f, columna]; f--) if (valores[f, columna] == numero) return false;
            for (int f = fila + 1; f < Tamano && blancas[f, columna]; f++) if (valores[f, columna] == numero) return false;
            return true;
        }

        private TableroKakuro CrearTablero()
        {
            int[,] h = new int[Tamano, Tamano], v = new int[Tamano, Tamano];
            for (int f = 0; f < Tamano; f++)
                for (int c = 0; c < Tamano; c++)
                {
                    if (blancas[f, c]) continue;
                    for (int x = c + 1; x < Tamano && blancas[f, x]; x++) h[f, c] += valores[f, x];
                    for (int y = f + 1; y < Tamano && blancas[y, c]; y++) v[f, c] += valores[y, c];
                }
            return new TableroKakuro(blancas, h, v);
        }

        private static bool PistasDeUnaCifra(TableroKakuro tablero)
        {
            for (int f = 0; f < tablero.Filas; f++)
                for (int c = 0; c < tablero.Columnas; c++)
                    if (tablero.PistaHorizontal(f, c) > 9 || tablero.PistaVertical(f, c) > 9)
                        return false;
            return true;
        }

        // Firma de pistas, no de la solución: sirve para detectar tableros repetidos.
        public static string Firma(TableroKakuro tablero)
        {
            var texto = new StringBuilder();
            for (int f = 0; f < tablero.Filas; f++)
                for (int c = 0; c < tablero.Columnas; c++)
                    if (tablero.EsBlanca(f, c)) texto.Append("W;");
                    else texto.Append(tablero.PistaHorizontal(f, c)).Append(',').Append(tablero.PistaVertical(f, c)).Append(';');
            return texto.ToString();
        }
    }
}
