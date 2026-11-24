using System;
using System.Collections.Generic;

// Núcleo independiente de Unity y de los scripts originales.
namespace KakuroNuevo
{
    public enum EstadoSolucion
    {
        TableroInvalido,
        SinSolucion,
        SolucionUnica,
        VariasSoluciones,
        LimiteAlcanzado
    }

    public sealed class ResultadoSolucion
    {
        public EstadoSolucion Estado { get; internal set; }
        public int SolucionesEncontradas { get; internal set; }
        public int NodosExplorados { get; internal set; }
        public string Detalle { get; internal set; }
        // Solo se proporciona cuando se ha encontrado al menos una solución.
        public int[,] PrimeraSolucion { get; internal set; }
    }

    // Las dimensiones incluyen la fila y la columna de casillas de pistas.
    // blanca[f,c] == true: casilla jugable. Las sumas se guardan en
    // casillas negras; horizontal va hacia la derecha, vertical hacia abajo.
    public sealed class TableroKakuro
    {
        internal readonly bool[,] Blancas;
        internal readonly int[,] Horizontales;
        internal readonly int[,] Verticales;
        public int Filas { get { return Blancas.GetLength(0); } }
        public int Columnas { get { return Blancas.GetLength(1); } }

        public TableroKakuro(bool[,] blancas, int[,] horizontales, int[,] verticales)
        {
            if (blancas == null || horizontales == null || verticales == null)
                throw new ArgumentNullException("Los arrays del tablero no pueden ser nulos.");
            int filas = blancas.GetLength(0), columnas = blancas.GetLength(1);
            if (filas == 0 || columnas == 0 ||
                horizontales.GetLength(0) != filas || horizontales.GetLength(1) != columnas ||
                verticales.GetLength(0) != filas || verticales.GetLength(1) != columnas)
                throw new ArgumentException("Los tres arrays deben tener las mismas dimensiones no vacías.");
            Blancas = (bool[,])blancas.Clone();
            Horizontales = (int[,])horizontales.Clone();
            Verticales = (int[,])verticales.Clone();
        }

        public bool EsBlanca(int fila, int columna) { return Blancas[fila, columna]; }
        public int PistaHorizontal(int fila, int columna) { return Horizontales[fila, columna]; }
        public int PistaVertical(int fila, int columna) { return Verticales[fila, columna]; }
    }

    // Resuelve usando exclusivamente las pistas: nunca consulta una solución oculta.
    // La búsqueda se detiene al encontrar dos soluciones o al agotar el presupuesto.
    public sealed class SolucionadorKakuro
    {
        private sealed class Grupo
        {
            public int Objetivo;
            public int Suma;
            public int Mascara;
            public int Restantes;
            public readonly List<int> Casillas = new List<int>();
        }
        private sealed class Casilla
        {
            public int Fila, Columna, Horizontal = -1, Vertical = -1, Valor;
        }

        private readonly List<Grupo> grupos = new List<Grupo>();
        private readonly List<Casilla> casillas = new List<Casilla>();
        private TableroKakuro tablero;
        private int limiteNodos, nodos, soluciones;
        private bool agotado;
        private int[,] primeraSolucion;

        // Una instancia no debe utilizarse simultáneamente desde varios hilos.
        public ResultadoSolucion Analizar(TableroKakuro entrada, int maxNodos = 200000)
        {
            if (entrada == null) throw new ArgumentNullException("entrada");
            if (maxNodos < 1) throw new ArgumentOutOfRangeException("maxNodos");
            tablero = entrada; limiteNodos = maxNodos; nodos = 0; soluciones = 0;
            agotado = false; primeraSolucion = null; grupos.Clear(); casillas.Clear();
            string error;
            if (!ConstruirGrupos(out error))
                return new ResultadoSolucion { Estado = EstadoSolucion.TableroInvalido, Detalle = error };
            Buscar();
            EstadoSolucion estado = soluciones >= 2 ? EstadoSolucion.VariasSoluciones :
                agotado ? EstadoSolucion.LimiteAlcanzado :
                soluciones == 1 ? EstadoSolucion.SolucionUnica : EstadoSolucion.SinSolucion;
            return new ResultadoSolucion { Estado = estado, SolucionesEncontradas = soluciones,
                NodosExplorados = nodos, PrimeraSolucion = primeraSolucion,
                Detalle = agotado && soluciones < 2 ? "Búsqueda incompleta: no se puede concluir si la solución es única." : null };
        }

        private bool ConstruirGrupos(out string error)
        {
            error = null;
            int filas = tablero.Filas, cols = tablero.Columnas;
            int[,] indice = new int[filas, cols];
            for (int f = 0; f < filas; f++)
                for (int c = 0; c < cols; c++)
                {
                    indice[f, c] = -1;
                    int h = tablero.Horizontales[f, c], v = tablero.Verticales[f, c];
                    if (h < 0 || v < 0) { error = "Una pista no puede ser negativa."; return false; }
                    if (tablero.Blancas[f, c])
                    {
                        if (h != 0 || v != 0) { error = "Una casilla blanca no puede contener pistas."; return false; }
                        indice[f, c] = casillas.Count;
                        casillas.Add(new Casilla { Fila = f, Columna = c });
                    }
                }
            if (casillas.Count == 0) { error = "El tablero no tiene casillas jugables."; return false; }
            for (int f = 0; f < filas; f++)
                for (int c = 0; c < cols; c++)
                {
                    if (tablero.Blancas[f, c]) continue;
                    int h = tablero.Horizontales[f, c], v = tablero.Verticales[f, c];
                    bool tieneH = c + 1 < cols && tablero.Blancas[f, c + 1];
                    bool tieneV = f + 1 < filas && tablero.Blancas[f + 1, c];
                    if ((h > 0) != tieneH || (v > 0) != tieneV)
                    { error = "Falta una pista o apunta a una secuencia inexistente."; return false; }
                    if (tieneH && !AgregarGrupo(f, c, 0, 1, h, indice, out error)) return false;
                    if (tieneV && !AgregarGrupo(f, c, 1, 0, v, indice, out error)) return false;
                }
            foreach (Casilla casilla in casillas)
                if (casilla.Horizontal < 0 || casilla.Vertical < 0)
                { error = "Cada casilla blanca necesita una pista horizontal y otra vertical."; return false; }
            return true;
        }

        private bool AgregarGrupo(int f, int c, int df, int dc, int suma, int[,] indice, out string error)
        {
            error = null;
            Grupo grupo = new Grupo { Objetivo = suma };
            int r = f + df, col = c + dc;
            while (r < tablero.Filas && col < tablero.Columnas && tablero.Blancas[r, col])
            {
                grupo.Casillas.Add(indice[r, col]); r += df; col += dc;
            }
            int n = grupo.Casillas.Count;
            if (n < 2 || n > 9) { error = "Las secuencias deben tener entre 2 y 9 casillas."; return false; }
            int minimo = n * (n + 1) / 2, maximo = n * (19 - n) / 2;
            if (suma < minimo || suma > maximo) { error = "Una pista está fuera del rango posible."; return false; }
            grupo.Restantes = n;
            int id = grupos.Count;
            grupos.Add(grupo);
            foreach (int i in grupo.Casillas)
            {
                if (dc == 1) casillas[i].Horizontal = id;
                else casillas[i].Vertical = id;
            }
            return true;
        }

        private void Buscar()
        {
            if (soluciones >= 2 || agotado) return;
            if (nodos >= limiteNodos) { agotado = true; return; }
            nodos++;
            int elegida = -1, mejorMascara = 0, menor = 10;
            for (int i = 0; i < casillas.Count; i++)
            {
                Casilla celda = casillas[i];
                if (celda.Valor != 0) continue;
                int mascara = 0, cantidad = 0;
                for (int valor = 1; valor <= 9; valor++)
                    if (Admite(grupos[celda.Horizontal], valor) && Admite(grupos[celda.Vertical], valor))
                    { mascara |= 1 << valor; cantidad++; }
                if (cantidad == 0) return;
                if (cantidad < menor) { elegida = i; mejorMascara = mascara; menor = cantidad; }
                if (menor == 1) break;
            }
            if (elegida == -1)
            {
                soluciones++;
                if (primeraSolucion == null)
                {
                    primeraSolucion = new int[tablero.Filas, tablero.Columnas];
                    foreach (Casilla casilla in casillas)
                        primeraSolucion[casilla.Fila, casilla.Columna] = casilla.Valor;
                }
                return;
            }
            Casilla seleccionada = casillas[elegida];
            Grupo h = grupos[seleccionada.Horizontal], v = grupos[seleccionada.Vertical];
            for (int valor = 1; valor <= 9 && soluciones < 2 && !agotado; valor++)
            {
                if ((mejorMascara & (1 << valor)) == 0) continue;
                seleccionada.Valor = valor;
                Aplicar(h, valor); Aplicar(v, valor);
                Buscar();
                Deshacer(h, valor); Deshacer(v, valor);
                seleccionada.Valor = 0;
            }
        }

        private static void Aplicar(Grupo grupo, int valor)
        { grupo.Suma += valor; grupo.Mascara |= 1 << valor; grupo.Restantes--; }
        private static void Deshacer(Grupo grupo, int valor)
        { grupo.Suma -= valor; grupo.Mascara &= ~(1 << valor); grupo.Restantes++; }
        private static bool Admite(Grupo grupo, int valor)
        {
            int bit = 1 << valor;
            if ((grupo.Mascara & bit) != 0) return false;
            int sumaPendiente = grupo.Objetivo - grupo.Suma - valor;
            return ExisteCombinacion(1, grupo.Restantes - 1, sumaPendiente, grupo.Mascara | bit);
        }
        // Basta comprobar combinaciones sin orden para descartar sumas imposibles.
        private static bool ExisteCombinacion(int desde, int cantidad, int suma, int usados)
        {
            if (cantidad == 0) return suma == 0;
            if (suma <= 0) return false;
            for (int n = desde; n <= 9; n++)
                if ((usados & (1 << n)) == 0 &&
                    ExisteCombinacion(n + 1, cantidad - 1, suma - n, usados | (1 << n)))
                    return true;
            return false;
        }
    }
}
