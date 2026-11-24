using System;
using System.Collections.Generic;
using System.Text;

namespace KakuroNuevo
{
    public sealed class ResultadoGeneracion
    {
        public TableroKakuro Tablero { get; internal set; }
        public int CandidatosProbados { get; internal set; }
        public int CambiosAceptados { get; internal set; }
        public bool IgualAlInicial { get; internal set; }
        public int Familia { get; internal set; }
    }

    // Prototipo: variaciones de plantillas para 7, 9 y 12; no generación libre.
    // No utiliza API de Unity. No compartir una instancia entre hilos.
    public sealed class GeneradorKakuro
    {
        private readonly int Tamano;
        private readonly Random aleatorio;
        private readonly SolucionadorKakuro solver = new SolucionadorKakuro();
        private readonly bool[,] blancas;
        private readonly int[,] valores;
        private readonly List<Tuple<int, int>> casillas = new List<Tuple<int, int>>();

        public GeneradorKakuro(int tamano, int semilla)
        {
            if (tamano != 7 && tamano != 9 && tamano != 12) throw new ArgumentOutOfRangeException("tamano");
            Tamano = tamano; aleatorio = new Random(semilla);
            blancas = new bool[Tamano, Tamano]; valores = new int[Tamano, Tamano];
        }

        public ResultadoGeneracion Generar(int maxCandidatos = 60, int maxNodosSolucionador = 3000, int? familia = null)
        {
            if (maxCandidatos < 0) throw new ArgumentOutOfRangeException("maxCandidatos");
            if (maxNodosSolucionador < 1) throw new ArgumentOutOfRangeException("maxNodosSolucionador");
            int familiasDisponibles = 3;
            if (familia.HasValue && (familia.Value < 1 || familia.Value > familiasDisponibles))
                throw new ArgumentOutOfRangeException("familia");
            int familiaElegida = familia ?? aleatorio.Next(1, familiasDisponibles + 1);
            PrepararPlantilla(familiaElegida);
            TableroKakuro inicial = CrearTablero();
            if ((Tamano != 7 && !PistasHastaVeinte(inicial)) ||
                solver.Analizar(inicial, 50000).Estado != EstadoSolucion.SolucionUnica)
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
                if ((Tamano == 7 || PistasHastaVeinte(candidato)) &&
                    solver.Analizar(candidato, maxNodosSolucionador).Estado == EstadoSolucion.SolucionUnica)
                    aceptados++;
                else
                    valores[f, c] = anterior;
            }
            TableroKakuro final = CrearTablero();
            return new ResultadoGeneracion { Tablero = final, CandidatosProbados = probados,
                CambiosAceptados = aceptados, IgualAlInicial = Firma(final) == firmaInicial, Familia = familiaElegida };
        }

        private void PrepararPlantilla(int familia)
        {
            Array.Clear(blancas, 0, blancas.Length); Array.Clear(valores, 0, valores.Length); casillas.Clear();
            string[][] plantillas;
            if (Tamano == 7)
                plantillas = new[] {
                    new[] { "##97##", "#832##", "#9785#", "461325", "81#148", "####19" },
                    new[] { "##49##", "#716##", "#5241#", "297846", "165#98", "48####" },
                    new[] { "142###", "69752#", "##913#", "2568##", "698###", "125###" }
                };
            else if (Tamano == 9)
                plantillas = new[] {
                    new[] { "##12#153", "4139#241", "172#432#", "#3421###", "###621##", "9251#549", "412#4216", "#49#23##" },
                    new[] { "##91#71#", "4132#241", "172#4325", "##421#79", "63#621##", "8251#54#", "412##217", "#49##623" },
                    new[] { "21##639#", "942#2143", "4315#521", "###162##", "##432#17", "#43#1532", "521#3128", "617##97#" }
                };
            else
                plantillas = new[] {
                    new[] { "##91#71####", "4132#241###", "172#4325###", "##421#7931#", "63#621#4127", "8251#54##59", "412##217###", "#495#6231##", "##315##1532", "###3952#718", "####613####" },
                    new[] { "##91#71####", "4132#241###", "172#4325###", "##421#792##", "63#621#315#", "8251#54#592", "412##217#61", "#497#6239##", "##415##2751", "###495##312", "####21#####" },
                    new[] { "##91#71####", "4132#241###", "172#4325###", "##421#7931#", "63#621#4127", "8251#54##59", "412##217###", "#495#6231##", "##315##1532", "###3952#718", "####613#42#" }
                };
            string[] filas = plantillas[familia - 1];
            // Las reflexiones pueden cambiar qué casillas grises llevan pista.
            // Para 9 y 12 se selecciona solo una orientación con pocas vacías seguidas.
            for (int intento = 0; intento < 16; intento++)
            {
                bool intercambiar = aleatorio.Next(2) == 1;
                bool invertirF = aleatorio.Next(2) == 1;
                bool invertirC = aleatorio.Next(2) == 1;
                bool complementar = Tamano == 7 && aleatorio.Next(2) == 1;
                InstalarPlantilla(filas, intercambiar, invertirF, invertirC, complementar);
                if (Tamano == 7 || MaximoVaciasSeguidas(CrearTablero()) <= (Tamano == 9 ? 3 : 5)) return;
            }
            // La orientación original de cada plantilla se ha verificado como válida.
            InstalarPlantilla(filas, false, false, false, false);
            if (Tamano != 7 && MaximoVaciasSeguidas(CrearTablero()) > (Tamano == 9 ? 3 : 5))
                throw new InvalidOperationException("La plantilla no cumple el límite de casillas vacías.");
        }

        private void InstalarPlantilla(string[] filas, bool intercambiar, bool invertirF, bool invertirC, bool complementar)
        {
            Array.Clear(blancas, 0, blancas.Length); Array.Clear(valores, 0, valores.Length); casillas.Clear();
            int interior = Tamano - 1;
            for (int f = 0; f < interior; f++)
                for (int c = 0; c < interior; c++)
                {
                    char ch = filas[f][c];
                    if (ch == '#') continue;
                    int r = intercambiar ? c : f, col = intercambiar ? f : c;
                    if (invertirF) r = interior - 1 - r;
                    if (invertirC) col = interior - 1 - col;
                    int numero = ch - '0';
                    if (complementar) numero = 10 - numero;
                    blancas[r + 1, col + 1] = true;
                    valores[r + 1, col + 1] = numero;
                    casillas.Add(Tuple.Create(r + 1, col + 1));
                }
        }

        // Cuenta solo casillas negras/grises SIN ninguna pista, no las pistas grises.
        private static int MaximoVaciasSeguidas(TableroKakuro tablero)
        {
            int maximo = 0;
            for (int orientacion = 0; orientacion < 2; orientacion++)
            {
                int lineas = orientacion == 0 ? tablero.Filas : tablero.Columnas;
                int longitud = orientacion == 0 ? tablero.Columnas : tablero.Filas;
                for (int a = 0; a < lineas; a++)
                {
                    int consecutivas = 0;
                    for (int b = 0; b < longitud; b++)
                    {
                        int f = orientacion == 0 ? a : b;
                        int c = orientacion == 0 ? b : a;
                        bool vacia = !tablero.EsBlanca(f, c) &&
                            tablero.PistaHorizontal(f, c) == 0 && tablero.PistaVertical(f, c) == 0;
                        consecutivas = vacia ? consecutivas + 1 : 0;
                        if (consecutivas > maximo) maximo = consecutivas;
                    }
                }
            }
            return maximo;
        }

        private static bool PistasHastaVeinte(TableroKakuro tablero)
        {
            for (int f = 0; f < tablero.Filas; f++)
                for (int c = 0; c < tablero.Columnas; c++)
                    if (tablero.PistaHorizontal(f, c) > 20 || tablero.PistaVertical(f, c) > 20) return false;
            return true;
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
