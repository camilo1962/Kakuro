using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KakuroNuevo;

// Escena de prueba autoconstruida: no modifica escenas ni scripts existentes.
public sealed class DemoKakuroMovil : MonoBehaviour
{
    [Header("Sprites de pistas (Texture Type: Sprite)")]
    [SerializeField] private Sprite pistaColumna; // triangle superior derecho
    [SerializeField] private Sprite pistaFila;    // triangle inferior izquierdo
    [SerializeField] private Sprite casillaLlena;
    [Header("Interfaz editable (opcional)")]
    [Tooltip("Prefab creado con Kakuro > Crear plantilla editable de juego. Sin asignar se usa la interfaz anterior.")]
    [SerializeField] private KakuroPlantillaInterfaz plantillaInterfaz;
    [Header("Prefabs de casillas (opcionales; asignar los cinco)")]
    [SerializeField] private KakuroCasillaEditable casillaJugablePrefab;
    [SerializeField] private KakuroCasillaEditable casillaGrisPrefab;
    [SerializeField] private KakuroCasillaEditable pistaFilaPrefab;
    [SerializeField] private KakuroCasillaEditable pistaColumnaPrefab;
    [SerializeField] private KakuroCasillaEditable pistaDoblePrefab;
    [Header("Paneles editables (opcionales)")]
    [SerializeField] private KakuroPanelEditable panelPausaPrefab;
    [SerializeField] private KakuroPanelEditable panelResetPrefab;
    [SerializeField] private KakuroPanelEditable panelConfirmarRendicionPrefab;
    [SerializeField] private KakuroPanelEditable panelResultadoRendicionPrefab;
    [SerializeField] private KakuroPanelEditable panelVictoriaPrefab;
    [SerializeField] private KakuroPanelEditable panelCombinacionesPrefab;
    [Header("Panel de dificultad editable (opcional)")]
    [SerializeField] private KakuroPanelDificultad panelDificultadPrefab;
    [Header("Selector, diálogos y deshacer (opcionales)")]
    [SerializeField] private KakuroDialogoExtra selectorNumerosPrefab;
    [SerializeField] private KakuroDialogoExtra dialogoSalidaPrefab;
    [SerializeField] private KakuroDialogoExtra dialogoCambioPrefab;
    [SerializeField] private Button botonDeshacerPrefab;
    [Tooltip("Posición normalizada del botón sobre el tablero.")]
    [SerializeField] private Vector2 anclaMinDeshacer = new Vector2(.66f, .765f);
    [SerializeField] private Vector2 anclaMaxDeshacer = new Vector2(.965f, .81f);
    [Header("Voz (opcional)")]
    [Tooltip("Prefab editable del botón Voz. Sin asignar se usa un botón predeterminado.")]
    [SerializeField] private Button botonVozPrefab;
    [SerializeField] private VozKakuro voz;
    [SerializeField] private Vector2 anclaMinVoz = new Vector2(.34f, .765f);
    [SerializeField] private Vector2 anclaMaxVoz = new Vector2(.64f, .81f);
    [Header("Escena principal")]
    [SerializeField] private string nombreEscenaMenu = "MainMenu";
    [Header("Fuentes TMP opcionales (si no se asignan se usa la fuente predeterminada)")]
    [SerializeField] private TMP_FontAsset fuenteInterfaz;
    [SerializeField] private TMP_FontAsset fuenteNumeros;
    [SerializeField] private TMP_FontAsset fuentePistas;
    [SerializeField] private TMP_FontAsset fuenteTiempo;

    private RectTransform raiz, zonaTablero, zonaAcciones, zonaDeshacer, zonaVoz;
    private GridLayoutGroup rejillaTablero, rejillaAcciones;
    private GameObject panelNumeros;
    private RectTransform rectPanelNumeros;
    private TMP_Text estado;
    private TMP_Text textoTiempo, textoDificultadNivel, rotuloTiempo, avisoGuardado;
    private double ultimoIntentoGuardado;
    private Button botonPistas;
    private Button botonDeshacer, botonVoz;
    private TMP_Text etiquetaVoz;
    private KakuroIconoVoz iconoVoz;
    private KakuroDialogoExtra selectorNumerosActivo;
    private const int MaximoDeshacer = 200;
    private readonly List<RegistroDeshacer> historialDeshacer = new List<RegistroDeshacer>();
    private TMP_Text textoPistas;
    private GameObject ventanaVictoria, ventanaMenu, ventanaPausa, ventanaSalida, ventanaConfirmacion, ventanaReset, ventanaRendicion, ventanaCombinaciones;
    private TMP_Text textoCombinaciones;
    private bool consultaUsaPrefab;
    private KakuroPlantillaInterfaz interfazActiva;
    private volatile bool aspectoConsultaPendiente;
    private DatosPartida partidaPendiente;
    private bool guardadoNoLegible;
    private bool saliendo;
    private int nivelActual = 1;
    private TableroKakuro tablero;
    private int[,] solucion;
    private int[,] entradas;
    private bool[,] casillasConError;
    private bool[,] casillasReveladas;
    private int pistasRestantes;
    private int victoriasEnEsteModo;
    private bool progresoGuardado;
    private bool tiempoGuardado;
    private double segundosJugados;
    private long ultimoSegundoMostrado = -1;
    private bool aplicacionEnPausa;
    private bool aplicacionConFoco = true;
    private bool ignorarSiguienteDelta;
    private Image[,] fondos;
    private Color[,] coloresNormalesCasillas;
    private TMP_Text[,] textos;
    private int seleccionFila = -1, seleccionColumna = -1;
    private int tamano = 7;
    private bool generando, terminado;
    private int pantallaAncho, pantallaAlto;
    [Header("Consulta de combinaciones")]
    [SerializeField] private Color fondoConsulta = Color.white;
    [SerializeField] private Color colorTextoConsulta = new Color32(35, 46, 61, 255);
    [SerializeField, Range(16, 60)] private int tamanoTituloConsulta = 44;
    [SerializeField, Range(12, 48)] private int tamanoTextoConsulta = 29;
    [SerializeField, Range(0.02f, 0.2f)] private float margenHorizontalConsulta = 0.05f;
    [SerializeField, Range(0.04f, 0.3f)] private float margenVerticalConsulta = 0.08f;

    [Header("Colores de la interfaz y el tablero")]
    [SerializeField] private Color fondo = new Color32(239, 241, 245, 255);
    [SerializeField] private Color acento = new Color32(38, 78, 127, 255);
    [SerializeField] private Color seleccionado = new Color32(178, 216, 246, 255);
    [SerializeField] private Color colorError = new Color32(255, 190, 190, 255);
    [SerializeField] private Color colorErrorSeleccionado = new Color32(244, 145, 145, 255);
    [SerializeField] private Color colorPista = new Color32(205, 235, 205, 255);

    // El tablero y su solución se preparan en el mismo hilo de trabajo.
    private sealed class PuzzlePreparado
    {
        public TableroKakuro Tablero;
        public int[,] Solucion;
        public int Nivel;
        public string Diagnostico;
    }

    private void Start()
    {
        if (pistaColumna == null || pistaFila == null)
        {
            Debug.LogError("DemoKakuroMovil: asigna los sprites column y row en el Inspector.");
            enabled = false;
            return;
        }
        if (voz == null) voz = GetComponent<VozKakuro>();
        if (voz == null) voz = gameObject.AddComponent<VozKakuro>();
        voz.Inicializar(PuedeUsarVoz, CasillaValidaParaVoz, AplicarNumeroPorVoz,
            mensaje => { if (estado != null) estado.text = mensaje; }, ActualizarBotonVoz);
        CrearInterfaz();
        RevisarPartidaPendiente();
        estado.text = "Elige una dificultad para comenzar.";
        AbrirMenu();
    }

    // OnValidate puede ejecutarse fuera del hilo principal: solo marca el cambio.
    // Update aplica los cambios de Unity UI con seguridad durante Play.
    private void OnValidate()
    {
        aspectoConsultaPendiente = true;
    }

    private void Update()
    {
        if (aspectoConsultaPendiente)
        {
            aspectoConsultaPendiente = false;
            ActualizarAspectoConsulta();
            if (zonaDeshacer != null) Anclar(zonaDeshacer, anclaMinDeshacer, anclaMaxDeshacer);
            if (zonaVoz != null) Anclar(zonaVoz, anclaMinVoz, anclaMaxVoz);
            ActualizarBotonVoz();
            if (tablero != null && fondos != null && casillasConError != null && casillasReveladas != null)
                ActualizarTodosLosColores();
        }
        if (voz != null && voz.Activa && !PuedeUsarVoz() && !voz.SolicitandoPermiso)
            voz.Desactivar(false);
        ActualizarBotonVoz();
        if (saliendo) return;
        if (pantallaAncho != Screen.width || pantallaAlto != Screen.height) AjustarDistribucion();
        ActualizarDistribucionAcciones();
        if (Input.GetKeyDown(KeyCode.Escape)) GestionarAtras();
        if (tablero == null || generando || terminado || ventanaVictoria != null || ventanaMenu != null ||
            ventanaPausa != null || ventanaSalida != null || ventanaReset != null || ventanaRendicion != null ||
            aplicacionEnPausa || !aplicacionConFoco) return;
        if (ignorarSiguienteDelta) ignorarSiguienteDelta = false;
        else segundosJugados += Math.Max(0.0, Time.unscaledDeltaTime);
        ActualizarTiempoUI();
        if (segundosJugados - ultimoIntentoGuardado >= 5.0) GuardarPartidaActual();
    }

    private void OnApplicationPause(bool enPausa)
    {
        aplicacionEnPausa = enPausa;
        if (enPausa && voz != null && !voz.SolicitandoPermiso) voz.Desactivar(false);
        if (enPausa && !saliendo) GuardarPartidaActual();
        else ignorarSiguienteDelta = true;
    }

    private void OnApplicationFocus(bool conFoco)
    {
        aplicacionConFoco = conFoco;
        if (!conFoco && voz != null && !voz.SolicitandoPermiso) voz.Desactivar(false);
        if (!conFoco && !saliendo) GuardarPartidaActual();
        else ignorarSiguienteDelta = true;
    }

    private void OnApplicationQuit()
    {
        if (!saliendo) GuardarPartidaActual();
    }

    private void ActualizarTiempoUI()
    {
        if (textoTiempo == null) return;
        long segundosEnteros = (long)Math.Floor(Math.Max(0.0, segundosJugados));
        if (segundosEnteros == ultimoSegundoMostrado) return;
        ultimoSegundoMostrado = segundosEnteros;
        if (textoDificultadNivel != null && rotuloTiempo != null)
        {
            textoDificultadNivel.text = NombreModo(tamano) + " · Nivel " + nivelActual;
            textoTiempo.text = FormatearTiempo(segundosJugados);
        }
        else
        {
            // Compatibilidad con prefabs anteriores a la cabecera separada.
            textoTiempo.text = NombreModo(tamano) + " · Nivel " + nivelActual + "  |  " +
                "Tiempo: " + FormatearTiempo(segundosJugados);
        }
    }

    private static string FormatearTiempo(double segundos)
    {
        long total = (long)Math.Floor(Math.Max(0.0, segundos));
        long horas = total / 3600;
        long minutos = (total / 60) % 60;
        long restantes = total % 60;
        return horas > 0
            ? string.Format("{0:00}:{1:00}:{2:00}", horas, minutos, restantes)
            : string.Format("{0:00}:{1:00}", minutos, restantes);
    }

    private void CrearInterfaz()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem (demo)", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasObj = new GameObject("Canvas Kakuro (demo)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
        raiz = Rect("Zona segura", canvasObj.transform);
        ActualizarZonaSegura();
        Image imagenFondo = raiz.gameObject.AddComponent<Image>(); imagenFondo.color = fondo;

        bool usaPlantilla = CrearInterfazDesdePlantilla();
        if (!usaPlantilla) CrearCabeceraPredeterminada();

        zonaDeshacer = Rect("Zona deshacer", raiz);
        Anclar(zonaDeshacer, anclaMinDeshacer, anclaMaxDeshacer);
        if (botonDeshacerPrefab != null)
        {
            botonDeshacer = Instantiate(botonDeshacerPrefab, zonaDeshacer, false);
            if (botonDeshacer.transform is RectTransform)
                Anclar((RectTransform)botonDeshacer.transform, Vector2.zero, Vector2.one);
            botonDeshacer.onClick.AddListener(Deshacer);
        }
        else
        {
            botonDeshacer = CrearBoton("Deshacer", zonaDeshacer, Deshacer);
            Anclar((RectTransform)botonDeshacer.transform, Vector2.zero, Vector2.one);
        }
        zonaVoz = Rect("Zona voz", raiz);
        Anclar(zonaVoz, anclaMinVoz, anclaMaxVoz);
        if (botonVozPrefab != null)
        {
            botonVoz = Instantiate(botonVozPrefab, zonaVoz, false);
            Anclar((RectTransform)botonVoz.transform, Vector2.zero, Vector2.one);
            botonVoz.onClick.AddListener(AlternarVoz);
        }
        else
        {
            botonVoz = CrearBoton("Voz", zonaVoz, AlternarVoz);
            Anclar((RectTransform)botonVoz.transform, Vector2.zero, Vector2.one);
        }
        etiquetaVoz = botonVoz.GetComponentInChildren<TMP_Text>(true);
        iconoVoz = botonVoz.GetComponent<KakuroIconoVoz>();
        ActualizarBotonVoz();
        zonaTablero = Rect("Tablero", raiz); Anclar(zonaTablero, new Vector2(.035f, .18f), new Vector2(.965f, .76f));
        rejillaTablero = zonaTablero.gameObject.AddComponent<GridLayoutGroup>();
        rejillaTablero.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rejillaTablero.childAlignment = TextAnchor.MiddleCenter;
        rejillaTablero.spacing = new Vector2(2, 2);
        if (!usaPlantilla) CrearAccionesPredeterminadas();
        ActualizarPistasUI();
        Canvas.ForceUpdateCanvases(); AjustarDistribucion();
    }

    private bool CrearInterfazDesdePlantilla()
    {
        if (plantillaInterfaz == null) return false;
        KakuroPlantillaInterfaz ui = Instantiate(plantillaInterfaz, raiz, false);
        if (!ui.ReferenciasValidas() || !(ui.transform is RectTransform))
        {
            Debug.LogError("Kakuro: la plantilla tiene referencias incompletas. Se usa la interfaz anterior; revisa el componente KakuroPlantillaInterfaz del prefab.");
            ui.gameObject.SetActive(false); Destroy(ui.gameObject);
            return false;
        }
        Anclar((RectTransform)ui.transform, Vector2.zero, Vector2.one);
        interfazActiva = ui;
        estado = ui.estado; textoTiempo = ui.tiempo; avisoGuardado = ui.avisoGuardado;
        textoDificultadNivel = ui.dificultadNivel; rotuloTiempo = ui.rotuloTiempo;
        zonaAcciones = ui.zonaAcciones; rejillaAcciones = ui.distribucionAcciones;
        botonPistas = ui.pistas; textoPistas = ui.textoPistas;
        ui.salir.onClick.AddListener(MostrarSalida);
        ui.pausa.onClick.AddListener(AbrirPausa);
        ui.menu.onClick.AddListener(AbrirMenu);
        ui.nuevo.onClick.AddListener(() => SolicitarTablero(tamano));
        ui.reset.onClick.AddListener(ConfirmarReset);
        ui.pistas.onClick.AddListener(PedirPista);
        ui.comprobar.onClick.AddListener(Comprobar);
        ui.rendirse.onClick.AddListener(ConfirmarRendicion);
        return true;
    }

    private void CrearCabeceraPredeterminada()
    {
        CrearTexto("Título", raiz, "KAKURO", new Vector2(.28f, .925f), new Vector2(.72f, .985f), 55, acento, TextAlignmentOptions.Center);
        RectTransform salir = Rect("Zona salir", raiz);
        Anclar(salir, new Vector2(.035f, .925f), new Vector2(.25f, .985f));
        Anclar(CrearBoton("Salir", salir, MostrarSalida).GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        RectTransform pausa = Rect("Zona pausa", raiz);
        Anclar(pausa, new Vector2(.75f, .925f), new Vector2(.965f, .985f));
        Anclar(CrearBoton("Pausa", pausa, AbrirPausa).GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        estado = CrearTexto("Estado", raiz, "Preparando…", new Vector2(.04f, .865f), new Vector2(.96f, .92f), 30, Color.black, TextAlignmentOptions.Center);
        textoDificultadNivel = CrearTexto("Dificultad y nivel", raiz, "Fácil · Nivel 1", new Vector2(.04f, .825f), new Vector2(.65f, .855f), 27, acento, TextAlignmentOptions.Center);
        rotuloTiempo = CrearTexto("Rótulo tiempo", raiz, "TIEMPO", new Vector2(.68f, .855f), new Vector2(.96f, .885f), 27, acento, TextAlignmentOptions.Center);
        textoTiempo = CrearTexto("Tiempo", raiz, "00:00", new Vector2(.68f, .825f), new Vector2(.96f, .855f), 27, acento, TextAlignmentOptions.Center);
    }

    private void CrearAccionesPredeterminadas()
    {
        zonaAcciones = Rect("Acciones", raiz); Anclar(zonaAcciones, new Vector2(.035f, .035f), new Vector2(.965f, .17f));
        rejillaAcciones = zonaAcciones.gameObject.AddComponent<GridLayoutGroup>();
        rejillaAcciones.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rejillaAcciones.constraintCount = 3; rejillaAcciones.spacing = new Vector2(8, 8);
        rejillaAcciones.childAlignment = TextAnchor.MiddleCenter;
        CrearBoton("Menú", zonaAcciones, AbrirMenu);
        CrearBoton("Nuevo", zonaAcciones, () => SolicitarTablero(tamano));
        CrearBoton("Reset", zonaAcciones, ConfirmarReset);
        botonPistas = CrearBoton("Pistas (0)", zonaAcciones, PedirPista);
        textoPistas = botonPistas.GetComponentInChildren<TMP_Text>();
        CrearBoton("Comprobar", zonaAcciones, Comprobar);
        CrearBoton("Me rindo", zonaAcciones, ConfirmarRendicion);
        avisoGuardado = CrearTexto("Aviso guardado", raiz, "", new Vector2(.035f, .003f), new Vector2(.965f, .03f), 24, Color.red, TextAlignmentOptions.Center);
    }

    // Cada panel puede usar su prefab; si no existe o está incompleto se usa el original.
    private KakuroPanelEditable InstanciarPanel(KakuroPanelEditable prefab, bool esConsulta)
    {
        if (prefab == null) return null;
        KakuroPanelEditable panel = Instantiate(prefab, raiz, false);
        if (!panel.ReferenciasValidas(esConsulta))
        {
            Debug.LogWarning("Kakuro: faltan referencias en el prefab " + prefab.name + ". Se usa el panel anterior.");
            panel.gameObject.SetActive(false); Destroy(panel.gameObject); return null;
        }
        Anclar((RectTransform)panel.transform, Vector2.zero, Vector2.one);
        panel.transform.SetAsLastSibling();
        return panel;
    }

    private KakuroDialogoExtra InstanciarDialogoExtra(KakuroDialogoExtra prefab, Transform padre, int tipo)
    {
        if (prefab == null) return null;
        KakuroDialogoExtra ui = Instantiate(prefab, padre, false);
        if (!ui.ReferenciasValidas(tipo))
        {
            Debug.LogWarning("Kakuro: faltan referencias en " + prefab.name + "; se usa el diálogo anterior.");
            ui.gameObject.SetActive(false); Destroy(ui.gameObject); return null;
        }
        Anclar((RectTransform)ui.transform, Vector2.zero, Vector2.one);
        ui.transform.SetAsLastSibling();
        return ui;
    }

    private bool PuedeUsarVoz()
    {
        return !saliendo && !generando && !terminado && tablero != null &&
            ventanaMenu == null && ventanaPausa == null && ventanaSalida == null &&
            ventanaVictoria == null && ventanaConfirmacion == null && ventanaReset == null &&
            ventanaRendicion == null && ventanaCombinaciones == null &&
            !aplicacionEnPausa && aplicacionConFoco;
    }
    private bool CasillaValidaParaVoz(int f, int c)
    {
        return PuedeUsarVoz() && f >= 0 && c >= 0 && f < tamano && c < tamano &&
            seleccionFila == f && seleccionColumna == c && tablero.EsBlanca(f, c) &&
            casillasReveladas != null && !casillasReveladas[f, c];
    }
    private void AplicarNumeroPorVoz(int f, int c, int numero)
    {
        if (numero < 0 || numero > 9 || !CasillaValidaParaVoz(f, c)) return;
        Poner(numero); // Exactamente el mismo historial y guardado que el selector manual.
    }
    private void AlternarVoz()
    {
        if (voz == null) return;
        CerrarPanelNumeros();
        voz.Alternar();
    }
    private void ActualizarBotonVoz()
    {
        if (botonVoz != null) botonVoz.interactable = PuedeUsarVoz();
        bool iconosListos = iconoVoz != null && iconoVoz.Configurado;
        if (iconoVoz != null) iconoVoz.Actualizar(voz != null && voz.Activa);
        if (etiquetaVoz != null)
        {
            if (etiquetaVoz.gameObject.activeSelf == iconosListos) etiquetaVoz.gameObject.SetActive(!iconosListos);
            if (!iconosListos && voz != null && etiquetaVoz.text != voz.TextoBoton)
                etiquetaVoz.text = voz.TextoBoton;
        }
    }

    private void ActualizarDeshacerUI()
    {
        if (botonDeshacer != null)
            botonDeshacer.interactable = !generando && !terminado && tablero != null && historialDeshacer.Count > 0;
    }

    private void Deshacer()
    {
        if (voz != null) voz.CancelarEscucha();
        if (generando || terminado || tablero == null || historialDeshacer.Count == 0) return;
        RegistroDeshacer ultimo = historialDeshacer[historialDeshacer.Count - 1];
        if (ultimo.fila < 0 || ultimo.fila >= tamano || ultimo.columna < 0 || ultimo.columna >= tamano ||
            !tablero.EsBlanca(ultimo.fila, ultimo.columna) || casillasReveladas[ultimo.fila, ultimo.columna] ||
            entradas[ultimo.fila, ultimo.columna] != ultimo.posterior)
        {
            historialDeshacer.Clear(); ActualizarDeshacerUI();
            estado.text = "No se pudo deshacer: el historial no coincide con el tablero.";
            GuardarPartidaActual(); return;
        }
        historialDeshacer.RemoveAt(historialDeshacer.Count - 1);
        entradas[ultimo.fila, ultimo.columna] = ultimo.anterior;
        textos[ultimo.fila, ultimo.columna].text = ultimo.anterior == 0 ? "" : ultimo.anterior.ToString();
        CerrarPanelNumeros(); LimpiarErrores(); Seleccionar(ultimo.fila, ultimo.columna);
        ActualizarDeshacerUI(); estado.text = "Última entrada deshecha.";
        GuardarPartidaActual();
    }

    // Una partida anterior sin historial sigue siendo válida. Un historial dañado se descarta,
    // sin descartar la partida. Se comprueba la cadena hacia atrás antes de utilizarla.
    private void RestaurarHistorialDeshacer(RegistroDeshacer[] datos)
    {
        historialDeshacer.Clear();
        if (datos == null || datos.Length == 0) return;
        if (datos.Length > MaximoDeshacer) { Debug.LogWarning("Kakuro: historial de deshacer demasiado largo; se conservó la partida."); return; }
        int[,] copia = (int[,])entradas.Clone();
        for (int i = datos.Length - 1; i >= 0; i--)
        {
            RegistroDeshacer paso = datos[i];
            if (paso == null || paso.fila < 0 || paso.fila >= tamano || paso.columna < 0 || paso.columna >= tamano ||
                !tablero.EsBlanca(paso.fila, paso.columna) || casillasReveladas[paso.fila, paso.columna] ||
                paso.anterior < 0 || paso.anterior > 9 || paso.posterior < 0 || paso.posterior > 9 ||
                paso.anterior == paso.posterior || copia[paso.fila, paso.columna] != paso.posterior)
            { Debug.LogWarning("Kakuro: historial de deshacer inválido; se conservó la partida."); return; }
            copia[paso.fila, paso.columna] = paso.anterior;
        }
        historialDeshacer.AddRange(datos);
    }

    private void AbrirPausa()
    {
        if (saliendo || ventanaPausa != null || ventanaSalida != null || generando) return;
        if (tablero == null || terminado)
        {
            estado.text = "No hay una partida activa que pausar.";
            return;
        }
        KakuroPanelEditable editable = InstanciarPanel(panelPausaPrefab, false);
        if (editable != null)
        {
            ventanaPausa = editable.gameObject;
            editable.botonPrincipal.onClick.AddListener(CerrarPausa);
            editable.botonSecundario.onClick.AddListener(MostrarSalida);
            GuardarPartidaActual();
            return;
        }
        RectTransform velo = Rect("Ventana pausa", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaPausa = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel pausa", velo);
        Anclar(panel, new Vector2(.08f, .32f), new Vector2(.92f, .68f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título pausa", panel, "Partida en pausa", new Vector2(.05f, .70f),
            new Vector2(.95f, .93f), 54, acento, TextAlignmentOptions.Center);
        CrearTexto("Detalle pausa", panel, "El cronómetro está detenido.", new Vector2(.06f, .48f),
            new Vector2(.94f, .69f), 32, Color.black, TextAlignmentOptions.Center);
        RectTransform acciones = Rect("Acciones pausa", panel);
        Anclar(acciones, new Vector2(.06f, .12f), new Vector2(.94f, .38f));
        var layout = acciones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Continuar", acciones, CerrarPausa);
        CrearBoton("Salir", acciones, MostrarSalida);
        GuardarPartidaActual();
    }

    private void CerrarPausa()
    {
        if (ventanaPausa == null) return;
        ventanaPausa.SetActive(false); Destroy(ventanaPausa); ventanaPausa = null;
        ignorarSiguienteDelta = true;
    }

    private void MostrarSalida()
    {
        if (saliendo || ventanaSalida != null) return;
        if (generando)
        {
            estado.text = "Espera a que termine la generación o recuperación.";
            return;
        }
        bool partidaActiva = tablero != null && !terminado;
        KakuroDialogoExtra ui = InstanciarDialogoExtra(dialogoSalidaPrefab, raiz, 1);
        if (ui != null)
        {
            ventanaSalida = ui.gameObject;
            ui.pregunta.text = partidaActiva ? "¿Quieres guardar la partida actual?" : "¿Quieres salir del juego?";
            ui.aviso.text = partidaActiva ? "Salir sin guardar descarta esta partida." : "";
            ui.guardarSalir.gameObject.SetActive(partidaActiva);
            ui.descartarSalir.gameObject.SetActive(partidaActiva);
            ui.salir.gameObject.SetActive(!partidaActiva);
            ui.guardarSalir.onClick.AddListener(() =>
            {
                if (!GuardarPartidaActual()) { ui.aviso.text = "No se pudo guardar. Revisa la consola."; return; }
                SalirAplicacion();
            });
            ui.descartarSalir.onClick.AddListener(() =>
            {
                if (!DescartarPartidaGuardada()) { ui.aviso.text = "No se pudo descartar. Revisa la consola."; return; }
                SalirAplicacion();
            });
            ui.salir.onClick.AddListener(SalirAplicacion);
            ui.cancelar.onClick.AddListener(CerrarSalida);
            return;
        }
        RectTransform velo = Rect("Ventana salir", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaSalida = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel salir", velo);
        Anclar(panel, new Vector2(.08f, .25f), new Vector2(.92f, .75f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título salir", panel, "Salir del juego", new Vector2(.05f, .82f),
            new Vector2(.95f, .96f), 48, acento, TextAlignmentOptions.Center);
        CrearTexto("Pregunta salir", panel,
            partidaActiva ? "¿Quieres guardar la partida actual?" : "¿Quieres salir del juego?",
            new Vector2(.05f, .69f), new Vector2(.95f, .82f), 32, Color.black, TextAlignmentOptions.Center);
        TMP_Text aviso = CrearTexto("Aviso salida", panel,
            partidaActiva ? "Salir sin guardar descarta esta partida." : "", new Vector2(.05f, .59f),
            new Vector2(.95f, .69f), 25, Color.red, TextAlignmentOptions.Center);
        RectTransform botones = Rect("Opciones de salida", panel);
        Anclar(botones, new Vector2(.10f, .06f), new Vector2(.90f, .58f));
        var layout = botones.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        if (partidaActiva)
        {
            CrearBoton("Guardar y salir", botones, () =>
            {
                if (!GuardarPartidaActual())
                {
                    aviso.text = "No se pudo guardar. Revisa la consola.";
                    return;
                }
                SalirAplicacion();
            });
            CrearBoton("Salir sin guardar", botones, () =>
            {
                if (!DescartarPartidaGuardada())
                {
                    aviso.text = "No se pudo descartar. Revisa la consola.";
                    return;
                }
                SalirAplicacion();
            });
        }
        else CrearBoton("Salir", botones, SalirAplicacion);
        CrearBoton("Cancelar", botones, CerrarSalida);
    }

    private bool DescartarPartidaGuardada()
    {
        try
        {
            PlayerPrefs.DeleteKey(ClavePartidaActiva);
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError("Kakuro: no se pudo descartar la partida guardada. " + error);
            // Se sigue en el juego. Intentar conservar la partida ante el fallo.
            GuardarPartidaActual();
            return false;
        }
    }

    private void CerrarSalida()
    {
        if (ventanaSalida == null) return;
        ventanaSalida.SetActive(false); Destroy(ventanaSalida); ventanaSalida = null;
        ignorarSiguienteDelta = true;
    }

    private void SalirAplicacion()
    {
        // Impedir que OnApplicationPause/Focus/Quit vuelva a guardar una partida descartada.
        saliendo = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Botón físico «Atrás» de Android y Escape en el Editor.
    private void GestionarAtras()
    {
        if (ventanaSalida != null) { CerrarSalida(); return; }
        if (ventanaCombinaciones != null) { CerrarConsultaCombinaciones(); return; }
        if (ventanaRendicion != null) { CerrarRendicion(); return; }
        if (ventanaReset != null) { CerrarReset(); return; }
        if (ventanaConfirmacion != null) { CerrarConfirmacion(); return; }
        if (panelNumeros != null) { CerrarPanelNumeros(); return; }
        if (ventanaVictoria != null) { CerrarVentanaVictoria(); return; }
        if (ventanaMenu != null)
        {
            if (tablero == null) VolverAlMenuPrincipal(); else CerrarMenu();
            return;
        }
        MostrarSalida();
    }

    // Una única partida local. Los otros datos (victorias e historial) no se tocan.
    private const string ClavePartidaActiva = "Kakuro.V1.PartidaActiva";

    [Serializable]
    private sealed class RegistroDeshacer
    {
        public int fila, columna, anterior, posterior;
    }

    [Serializable]
    private sealed class DatosPartida
    {
        public int version, tamano, nivel, pistasRestantes, seleccionFila, seleccionColumna;
        public double segundos;
        public bool terminado;
        public bool[] blancas, reveladas;
        public int[] horizontales, verticales, entradas;
        public RegistroDeshacer[] historialDeshacer;
    }

    private sealed class PartidaValidada
    {
        public DatosPartida Datos;
        public TableroKakuro Tablero;
        public int[,] Solucion;
    }

    // El botón Continuar se ofrece solo para una partida sin terminar.
    // La validación completa y el solucionador se ejecutan al continuar.
    private void RevisarPartidaPendiente()
    {
        partidaPendiente = null; guardadoNoLegible = false;
        try
        {
            if (!PlayerPrefs.HasKey(ClavePartidaActiva)) return;
            string json = PlayerPrefs.GetString(ClavePartidaActiva, "");
            DatosPartida datos = JsonUtility.FromJson<DatosPartida>(json);
            if (datos == null || datos.version != 1 ||
                (datos.tamano != 7 && datos.tamano != 9 && datos.tamano != 12))
                throw new InvalidOperationException("Formato de partida no reconocido.");
            if (!datos.terminado) partidaPendiente = datos;
        }
        catch (Exception error)
        {
            guardadoNoLegible = true;
            Debug.LogError("Kakuro: no se pudo leer la partida guardada; se conserva sin cambios. " + error);
        }
    }

    private void ContinuarPartidaGuardada()
    {
        if (partidaPendiente == null || generando) return;
        DatosPartida datos = partidaPendiente;
        CerrarMenu();
        StartCoroutine(RestaurarPartida(datos));
    }

    private void VolverAlMenuPrincipal()
    {
        if (string.IsNullOrWhiteSpace(nombreEscenaMenu) || !Application.CanStreamedLevelBeLoaded(nombreEscenaMenu))
        {
            Debug.LogError("Kakuro: no se encuentra la escena principal '" + nombreEscenaMenu +
                "'. Revisa el Inspector y File > Build Settings.");
            if (textoAvisoMenu != null) textoAvisoMenu.text = "Revisa la escena principal en Build Settings.";
            return;
        }
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    private IEnumerator RestaurarPartida(DatosPartida datos)
    {
        generando = true;
        estado.text = "Recuperando partida…";
        ActualizarPistasUI();
        Task<PartidaValidada> tarea = Task.Run(() => ValidarPartida(datos));
        while (!tarea.IsCompleted) yield return null;
        generando = false;
        if (tarea.IsFaulted)
        {
            Debug.LogError("Kakuro: no se pudo validar la partida guardada; se conserva sin cambios. " + tarea.Exception);
            estado.text = "No se pudo recuperar la partida. Revisa la consola.";
            guardadoNoLegible = true;
            partidaPendiente = null;
            ActualizarPistasUI();
            AbrirMenu();
            if (textoAvisoMenu != null) textoAvisoMenu.text = "No se pudo recuperar la partida.";
            yield break;
        }
        PartidaValidada recuperada = tarea.Result;
        partidaPendiente = null; guardadoNoLegible = false;
        tamano = datos.tamano; nivelActual = datos.nivel;
        tablero = recuperada.Tablero; solucion = recuperada.Solucion;
        entradas = new int[tamano, tamano];
        casillasReveladas = new bool[tamano, tamano];
        casillasConError = new bool[tamano, tamano];
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
            {
                int i = f * tamano + c;
                entradas[f, c] = datos.entradas[i];
                casillasReveladas[f, c] = datos.reveladas[i];
            }
        RestaurarHistorialDeshacer(datos.historialDeshacer);
        pistasRestantes = datos.pistasRestantes;
        segundosJugados = datos.segundos; ultimoIntentoGuardado = segundosJugados;
        ultimoSegundoMostrado = -1; ignorarSiguienteDelta = true;
        seleccionFila = seleccionColumna = -1; terminado = false;
        DibujarTablero();
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c))
                {
                    textos[f, c].text = entradas[f, c] == 0 ? "" : entradas[f, c].ToString();
                    if (casillasReveladas[f, c]) textos[f, c].fontStyle = FontStyles.Bold;
                }
        ActualizarTodosLosColores();
        if (datos.seleccionFila >= 0 && datos.seleccionFila < tamano &&
            datos.seleccionColumna >= 0 && datos.seleccionColumna < tamano &&
            tablero.EsBlanca(datos.seleccionFila, datos.seleccionColumna))
            Seleccionar(datos.seleccionFila, datos.seleccionColumna);
        ActualizarTiempoUI(); ActualizarPistasUI();
        avisoGuardado.text = "";
        estado.text = "Partida recuperada. ¡Continúa jugando!";
        Debug.Log("Kakuro: partida recuperada, " + NombreModo(tamano) + " nivel " + nivelActual + ".");
    }

    // Sin API de Unity: validar el contenido y volver a comprobar solución única.
    private static PartidaValidada ValidarPartida(DatosPartida d)
    {
        int n = d.tamano, cantidad = n * n;
        int ayudas = n == 7 ? 5 : (n == 9 ? 3 : 1);
        if (d.nivel < 1 || d.nivel > 3 || d.pistasRestantes < 0 || d.pistasRestantes > ayudas ||
            double.IsNaN(d.segundos) || double.IsInfinity(d.segundos) || d.segundos < 0 ||
            d.blancas == null || d.reveladas == null || d.horizontales == null ||
            d.verticales == null || d.entradas == null || d.blancas.Length != cantidad ||
            d.reveladas.Length != cantidad || d.horizontales.Length != cantidad ||
            d.verticales.Length != cantidad || d.entradas.Length != cantidad)
            throw new InvalidOperationException("Los datos de la partida son incompletos o inválidos.");
        var blancas = new bool[n, n]; var h = new int[n, n]; var v = new int[n, n];
        int reveladas = 0;
        for (int f = 0; f < n; f++)
            for (int c = 0; c < n; c++)
            {
                int i = f * n + c;
                if (d.entradas[i] < 0 || d.entradas[i] > 9 || d.horizontales[i] < 0 ||
                    d.horizontales[i] > 45 || d.verticales[i] < 0 || d.verticales[i] > 45 ||
                    (d.blancas[i] && (d.horizontales[i] != 0 || d.verticales[i] != 0)) ||
                    (!d.blancas[i] && (d.entradas[i] != 0 || d.reveladas[i])) ||
                    (d.reveladas[i] && d.entradas[i] == 0))
                    throw new InvalidOperationException("Hay una casilla guardada inválida.");
                blancas[f, c] = d.blancas[i]; h[f, c] = d.horizontales[i]; v[f, c] = d.verticales[i];
                if (d.reveladas[i]) reveladas++;
            }
        if (reveladas != ayudas - d.pistasRestantes)
            throw new InvalidOperationException("Las pistas guardadas no coinciden con el contador.");
        var tableroRecuperado = new TableroKakuro(blancas, h, v);
        ResultadoSolucion analisis = new SolucionadorKakuro().Analizar(tableroRecuperado, 50000);
        if (analisis.Estado != EstadoSolucion.SolucionUnica || analisis.PrimeraSolucion == null)
            throw new InvalidOperationException("No se pudo confirmar la solución única de la partida guardada: " + analisis.Estado);
        for (int f = 0; f < n; f++)
            for (int c = 0; c < n; c++)
            {
                int i = f * n + c;
                if (d.reveladas[i] && d.entradas[i] != analisis.PrimeraSolucion[f, c])
                    throw new InvalidOperationException("Una pista guardada no coincide con la solución.");
            }
        return new PartidaValidada { Datos = d, Tablero = tableroRecuperado, Solucion = analisis.PrimeraSolucion };
    }

    private bool GuardarPartidaActual()
    {
        if (tablero == null || entradas == null || casillasReveladas == null || generando) return false;
        ultimoIntentoGuardado = segundosJugados;
        try
        {
            int cantidad = tamano * tamano;
            var d = new DatosPartida { version = 1, tamano = tamano, nivel = nivelActual,
                pistasRestantes = pistasRestantes, segundos = segundosJugados, terminado = terminado,
                seleccionFila = seleccionFila, seleccionColumna = seleccionColumna,
                blancas = new bool[cantidad], reveladas = new bool[cantidad],
                horizontales = new int[cantidad], verticales = new int[cantidad], entradas = new int[cantidad],
                historialDeshacer = historialDeshacer.ToArray() };
            for (int f = 0; f < tamano; f++)
                for (int c = 0; c < tamano; c++)
                {
                    int i = f * tamano + c;
                    d.blancas[i] = tablero.EsBlanca(f, c);
                    d.horizontales[i] = tablero.PistaHorizontal(f, c);
                    d.verticales[i] = tablero.PistaVertical(f, c);
                    d.entradas[i] = entradas[f, c]; d.reveladas[i] = casillasReveladas[f, c];
                }
            PlayerPrefs.SetString(ClavePartidaActiva, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
            if (avisoGuardado != null) avisoGuardado.text = "";
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError("Kakuro: no se pudo guardar la partida. " + error);
            if (avisoGuardado != null) avisoGuardado.text = "No se pudo guardar la partida en este dispositivo.";
            return false;
        }
    }

    // Cada modo usa su propio contador; las victorias anteriores también cuentan.
    private static int NivelSegunVictorias(int tamanoTablero)
    {
        int victorias = Mathf.Max(0, PlayerPrefs.GetInt(ClaveVictorias(tamanoTablero), 0));
        return NivelParaVictorias(victorias);
    }

    private static int NivelParaVictorias(int victorias)
    {
        return victorias < 3 ? 1 : (victorias < 6 ? 2 : 3);
    }

    private static string NombreModo(int tamanoTablero)
    {
        return tamanoTablero == 7 ? "Fácil" : (tamanoTablero == 9 ? "Medio" : "Difícil");
    }

    [ContextMenu("Consultar niveles guardados")]
    private void ConsultarNivelesGuardados()
    {
        Debug.Log("Kakuro - Nivel para el próximo puzzle: Fácil=" + NivelSegunVictorias(7) +
            "; Medio=" + NivelSegunVictorias(9) + "; Difícil=" + NivelSegunVictorias(12) +
            ". Umbrales: 0–2, 3–5 y 6+ victorias en cada modo.");
    }

    private sealed class CandidatoNivel
    {
        public TableroKakuro Tablero;
        public int Puntuacion;
        public int Semilla;
    }

    private static int IndiceParaNivel(int nivel, int cantidad)
    {
        return nivel == 1 ? 0 : (nivel == 2 ? (cantidad - 1) / 2 : cantidad - 1);
    }

    // Se comparte con la prueba para que evalúe exactamente la selección del juego.
    private static List<CandidatoNivel> CrearCandidatos(int tamanoTablero, int semilla)
    {
        int cantidad = tamanoTablero == 7 ? 6 : 3;
        var candidatos = new List<CandidatoNivel>();
        for (int i = 0; i < cantidad; i++)
        {
            int semillaCandidato = unchecked(semilla + i * 7919);
            TableroKakuro candidato = tamanoTablero == 7
                ? new GeneradorKakuroFacil(semillaCandidato).Generar(20, 3000).Tablero
                : new GeneradorKakuro(tamanoTablero, semillaCandidato).Generar(12, 3000).Tablero;
            candidatos.Add(new CandidatoNivel { Tablero = candidato,
                Puntuacion = MedirAmbiguedad(candidato), Semilla = semillaCandidato });
        }
        candidatos.Sort((a, b) => a.Puntuacion.CompareTo(b.Puntuacion));
        return candidatos;
    }

    private bool pruebaProgresionEnCurso;

    // Solo diagnóstico: no cambia el tablero, las victorias ni el historial.
    [ContextMenu("Probar niveles 9x9 y 12x12")]
    private void ProbarNiveles()
    {
        if (!Application.isPlaying || raiz == null)
        {
            Debug.LogWarning("Kakuro: entra en Play para probar los niveles.");
            return;
        }
        if (generando || pruebaProgresionEnCurso)
        {
            Debug.LogWarning("Kakuro: espera a que termine la generación o la prueba anterior.");
            return;
        }
        StartCoroutine(EjecutarPruebaNiveles());
    }

    private IEnumerator EjecutarPruebaNiveles()
    {
        pruebaProgresionEnCurso = true;
        Debug.Log("Kakuro: probando niveles de 9x9 y 12x12 en segundo plano; no se cambia la partida ni las victorias.");
        int semilla = Environment.TickCount;
        Task<string> prueba = Task.Run(() =>
        {
            var informe = new System.Text.StringBuilder();
            foreach (int tamanoPrueba in new[] { 9, 12 })
            {
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                int semillaBase = unchecked(semilla + tamanoPrueba * 100003);
                List<CandidatoNivel> candidatos = CrearCandidatos(tamanoPrueba, semillaBase);
                var firmas = new HashSet<string>();
                var lineas = new System.Text.StringBuilder();
                for (int nivel = 1; nivel <= 3; nivel++)
                {
                    CandidatoNivel candidato = candidatos[IndiceParaNivel(nivel, candidatos.Count)];
                    ResultadoSolucion analisis = new SolucionadorKakuro().Analizar(candidato.Tablero, 50000);
                    bool unica = analisis.Estado == EstadoSolucion.SolucionUnica && analisis.PrimeraSolucion != null;
                    firmas.Add(GeneradorKakuro.Firma(candidato.Tablero));
                    lineas.Append("\n  Nivel ").Append(nivel)
                        .Append(": verificación=").Append(analisis.Estado)
                        .Append(unica ? " (solución obtenida)" : " (NO CONFIRMADA)")
                        .Append("; ambigüedad=").Append(candidato.Puntuacion)
                        .Append("; semilla=").Append(candidato.Semilla);
                }
                reloj.Stop();
                informe.Append("\n").Append(tamanoPrueba).Append("x").Append(tamanoPrueba)
                    .Append(": ").Append(firmas.Count).Append("/3 tableros distintos; tiempo total de prueba=")
                    .Append(reloj.ElapsedMilliseconds).Append(" ms.").Append(lineas);
            }
            return informe.ToString();
        });
        while (!prueba.IsCompleted) yield return null;
        pruebaProgresionEnCurso = false;
        if (prueba.IsFaulted)
            Debug.LogError("Kakuro: error en la prueba de niveles. " + prueba.Exception);
        else
            Debug.Log("Kakuro - Prueba de niveles:" + prueba.Result +
                "\nLa ambigüedad es orientativa; no mide por sí sola la dificultad humana.");
    }

    // Para cada grupo se enumeran combinaciones de cifras distintas que cumplen
    // la suma. Como inicialmente no hay cifras fijadas, cada cifra de cualquier
    // combinación puede ocupar cualquier posición del grupo. Se intersectan las
    // opciones horizontal y vertical. Es una aproximación, no dificultad humana.
    private static int MedirAmbiguedad(TableroKakuro t)
    {
        int[,] horizontal = new int[t.Filas, t.Columnas];
        int[,] vertical = new int[t.Filas, t.Columnas];
        for (int f = 0; f < t.Filas; f++)
            for (int c = 0; c < t.Columnas; c++)
            {
                if (t.EsBlanca(f, c)) continue;
                int h = t.PistaHorizontal(f, c), v = t.PistaVertical(f, c);
                if (h > 0) OpcionesGrupo(t, f, c, 0, 1, h, horizontal);
                if (v > 0) OpcionesGrupo(t, f, c, 1, 0, v, vertical);
            }
        int puntuacion = 0, blancas = 0;
        for (int f = 0; f < t.Filas; f++)
            for (int c = 0; c < t.Columnas; c++)
                if (t.EsBlanca(f, c))
                {
                    blancas++;
                    int mascara = horizontal[f, c] & vertical[f, c];
                    for (int n = 1; n <= 9; n++)
                        if ((mascara & (1 << n)) != 0) puntuacion++;
                }
        return blancas == 0 ? 0 : (puntuacion * 1000) / blancas;
    }

    private static void OpcionesGrupo(TableroKakuro t, int f, int c, int df, int dc,
        int suma, int[,] opciones)
    {
        var posiciones = new List<Tuple<int, int>>();
        f += df; c += dc;
        while (f < t.Filas && c < t.Columnas && t.EsBlanca(f, c))
        {
            posiciones.Add(Tuple.Create(f, c));
            f += df; c += dc;
        }
        int mascara = MascaraCombinaciones(1, posiciones.Count, suma, 0);
        foreach (var posicion in posiciones)
            opciones[posicion.Item1, posicion.Item2] = mascara;
    }

    // Visita combinaciones, no permutaciones; hay como máximo 512 subconjuntos.
    private static int MascaraCombinaciones(int desde, int cantidad, int restante, int usados)
    {
        if (cantidad == 0) return restante == 0 ? usados : 0;
        if (restante <= 0 || 10 - desde < cantidad) return 0;
        int mascara = 0;
        for (int n = desde; n <= 9 && n <= restante; n++)
            mascara |= MascaraCombinaciones(n + 1, cantidad - 1, restante - n, usados | (1 << n));
        return mascara;
    }

    private TMP_Text textoAvisoMenu;

    private void AbrirMenu()
    {
        if (generando) { estado.text = "Espera a que termine la generación."; return; }
        if (ventanaMenu != null) return;
        if (panelDificultadPrefab != null)
        {
            KakuroPanelDificultad ui = Instantiate(panelDificultadPrefab, raiz, false);
            if (ui.ReferenciasValidas())
            {
                Anclar((RectTransform)ui.transform, Vector2.zero, Vector2.one);
                ventanaMenu = ui.gameObject; ui.transform.SetAsLastSibling();
                textoAvisoMenu = ui.aviso;
                textoAvisoMenu.text = guardadoNoLegible ? "No se pudo leer la partida guardada." : "";
                ui.continuar.gameObject.SetActive(tablero == null && partidaPendiente != null);
                ui.menuPrincipal.gameObject.SetActive(tablero == null);
                ui.volverTablero.gameObject.SetActive(tablero != null);
                ui.facil.onClick.AddListener(() => ElegirModo(7));
                ui.medio.onClick.AddListener(() => ElegirModo(9));
                ui.dificil.onClick.AddListener(() => ElegirModo(12));
                ui.continuar.onClick.AddListener(ContinuarPartidaGuardada);
                ui.menuPrincipal.onClick.AddListener(VolverAlMenuPrincipal);
                ui.volverTablero.onClick.AddListener(CerrarMenu);
                return;
            }
            Debug.LogWarning("Kakuro: faltan referencias en el prefab de dificultad. Se usa el panel anterior.");
            ui.gameObject.SetActive(false); Destroy(ui.gameObject);
        }
        RectTransform velo = Rect("Menú de modos", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaMenu = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0, 0, 0, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel menú", velo);
        Anclar(panel, new Vector2(.08f, .23f), new Vector2(.92f, .77f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título menú", panel, "Elige una dificultad", new Vector2(.05f, .83f),
            new Vector2(.95f, .97f), 45, acento, TextAlignmentOptions.Center);
        textoAvisoMenu = CrearTexto("Aviso menú", panel,
            guardadoNoLegible ? "No se pudo leer la partida guardada." : "",
            new Vector2(.05f, .75f), new Vector2(.95f, .83f), 25, Color.red, TextAlignmentOptions.Center);
        RectTransform modos = Rect("Opciones de dificultad", panel);
        Anclar(modos, new Vector2(.10f, .06f), new Vector2(.90f, .74f));
        var layout = modos.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Fácil 7×7", modos, () => ElegirModo(7));
        CrearBoton("Medio 9×9", modos, () => ElegirModo(9));
        CrearBoton("Difícil 12×12", modos, () => ElegirModo(12));
        if (tablero == null)
        {
            if (partidaPendiente != null)
                CrearBoton("Continuar partida", modos, ContinuarPartidaGuardada);
            CrearBoton("Menú principal", modos, VolverAlMenuPrincipal);
        }
        else CrearBoton("Volver al tablero", modos, CerrarMenu);
    }

    private void ElegirModo(int nuevoTamano)
    {
        bool hayPartidaActual = tablero != null && !terminado;
        bool hayGuardadoPendiente = tablero == null && (partidaPendiente != null || guardadoNoLegible);
        if (!hayPartidaActual && !hayGuardadoPendiente)
        {
            CerrarMenu(); SolicitarTablero(nuevoTamano); return;
        }
        // El guardado previo se conserva hasta que se genere el tablero nuevo.
        KakuroDialogoExtra ui = InstanciarDialogoExtra(dialogoCambioPrefab, ventanaMenu.transform, 2);
        if (ui != null)
        {
            ventanaConfirmacion = ui.gameObject;
            ui.pregunta.text = hayGuardadoPendiente ? "Se sobrescribirá la partida guardada. ¿Continuar?" : "Se perderá la partida actual. ¿Continuar?";
            ui.cancelar.onClick.AddListener(CerrarConfirmacion);
            ui.cambiar.onClick.AddListener(() => { CerrarMenu(); SolicitarTablero(nuevoTamano); });
            return;
        }
        RectTransform aviso = Rect("Confirmar cambio", ventanaMenu.transform);
        ventanaConfirmacion = aviso.gameObject;
        Anclar(aviso, Vector2.zero, Vector2.one); aviso.SetAsLastSibling();
        Image sombra = aviso.gameObject.AddComponent<Image>();
        sombra.color = new Color(0, 0, 0, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel confirmación", aviso);
        Anclar(panel, new Vector2(.08f, .36f), new Vector2(.92f, .64f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Aviso", panel,
            hayGuardadoPendiente ? "Se sobrescribirá la partida guardada. ¿Continuar?" : "Se perderá la partida actual. ¿Continuar?",
            new Vector2(.05f, .53f), new Vector2(.95f, .92f), 35, Color.black, TextAlignmentOptions.Center);
        RectTransform botones = Rect("Acciones confirmación", panel);
        Anclar(botones, new Vector2(.06f, .10f), new Vector2(.94f, .42f));
        var layout = botones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Cancelar", botones, CerrarConfirmacion);
        CrearBoton("Cambiar", botones, () => { CerrarMenu(); SolicitarTablero(nuevoTamano); });
    }

    private void CerrarConfirmacion()
    {
        if (ventanaConfirmacion == null) return;
        ventanaConfirmacion.SetActive(false); Destroy(ventanaConfirmacion); ventanaConfirmacion = null;
    }

    private void CerrarMenu()
    {
        if (ventanaMenu == null) return;
        ventanaConfirmacion = null; textoAvisoMenu = null;
        ventanaMenu.SetActive(false); Destroy(ventanaMenu); ventanaMenu = null;
        ignorarSiguienteDelta = true;
    }

    private void SolicitarTablero(int nuevoTamano)
    {
        if (voz != null) voz.Desactivar(false);
        if (generando) { estado.text = "Espera a que termine la generación."; return; }
        StartCoroutine(Generar(nuevoTamano));
    }

    private IEnumerator Generar(int nuevoTamano)
    {
        generando = true; estado.text = "Generando " + nuevoTamano + " × " + nuevoTamano + "…";
        ActualizarPistasUI();
        int semilla = Environment.TickCount;
        int nivel = NivelSegunVictorias(nuevoTamano);
        Task<PuzzlePreparado> tarea = Task.Run(() =>
        {
            var reloj = System.Diagnostics.Stopwatch.StartNew();
            List<CandidatoNivel> candidatos = CrearCandidatos(nuevoTamano, semilla);
            int cantidad = candidatos.Count;
            int indice = IndiceParaNivel(nivel, cantidad);
            TableroKakuro generado = candidatos[indice].Tablero;
            ResultadoSolucion analisis = new SolucionadorKakuro().Analizar(generado, 50000);
            if (analisis.Estado != EstadoSolucion.SolucionUnica || analisis.PrimeraSolucion == null)
                throw new InvalidOperationException("No se pudo verificar la solución única para las pistas: " + analisis.Estado);
            reloj.Stop();
            string diagnostico = NombreModo(nuevoTamano) + " nivel " + nivel +
                "; ambigüedad inicial seleccionada=" + candidatos[indice].Puntuacion +
                "; intervalo de " + cantidad + " candidatos=" + candidatos[0].Puntuacion + "–" +
                candidatos[cantidad - 1].Puntuacion + "; generación=" + reloj.ElapsedMilliseconds + " ms.";
            return new PuzzlePreparado { Tablero = generado, Solucion = analisis.PrimeraSolucion,
                Nivel = nivel, Diagnostico = diagnostico };
        });
        while (!tarea.IsCompleted) yield return null;
        generando = false;
        if (tarea.IsFaulted)
        {
            estado.text = "No se pudo generar el tablero. Revisa la consola.";
            Debug.LogError("Demo Kakuro: " + tarea.Exception);
            ActualizarPistasUI();
            if (tablero == null)
            {
                AbrirMenu();
                if (textoAvisoMenu != null) textoAvisoMenu.text = "No se pudo generar. Inténtalo de nuevo.";
            }
            yield break;
        }
        tamano = nuevoTamano; tablero = tarea.Result.Tablero; solucion = tarea.Result.Solucion;
        historialDeshacer.Clear();
        nivelActual = tarea.Result.Nivel;
        if (tarea.Result.Diagnostico != null) Debug.Log("Kakuro: " + tarea.Result.Diagnostico);
        entradas = new int[tamano, tamano];
        casillasConError = new bool[tamano, tamano];
        casillasReveladas = new bool[tamano, tamano];
        pistasRestantes = tamano == 7 ? 5 : (tamano == 9 ? 3 : 1);
        segundosJugados = 0; ultimoSegundoMostrado = -1;
        ignorarSiguienteDelta = true;
        seleccionFila = seleccionColumna = -1; terminado = false;
        ActualizarTiempoUI();
        DibujarTablero();
        ActualizarPistasUI();
        estado.text = "Selecciona una casilla y elige un número.";
        if (GuardarPartidaActual()) { partidaPendiente = null; guardadoNoLegible = false; }
    }

    private void DibujarTablero()
    {
        // Desactivar antes de destruir evita que el layout cuente hijos antiguos este frame.
        foreach (Transform hijo in zonaTablero) { hijo.gameObject.SetActive(false); Destroy(hijo.gameObject); }
        fondos = new Image[tamano, tamano]; textos = new TMP_Text[tamano, tamano];
        coloresNormalesCasillas = new Color[tamano, tamano];
        bool usarPrefabs = PrefabsCasillasValidos();
        rejillaTablero.constraintCount = tamano;
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
            {
                int fila = f, columna = c;
                if (usarPrefabs)
                {
                    DibujarCasillaPrefab(fila, columna);
                    continue;
                }
                RectTransform celda = Rect("Casilla " + f + "," + c, zonaTablero);
                Image imagen = celda.gameObject.AddComponent<Image>(); fondos[f, c] = imagen;
                if (tablero.EsBlanca(f, c))
                {
                    imagen.color = Color.white;
                    coloresNormalesCasillas[f, c] = imagen.color;
                    Button boton = celda.gameObject.AddComponent<Button>(); boton.targetGraphic = imagen;
                    boton.transition = Selectable.Transition.None;
                    boton.onClick.AddListener(() => TocarCasilla(fila, columna));
                    textos[f, c] = CrearTexto("Número", celda, "", Vector2.zero, Vector2.one, 52, Color.black, TextAlignmentOptions.Center);
                }
                else
                {
                    int h = tablero.PistaHorizontal(f, c), v = tablero.PistaVertical(f, c);
                    if (h > 0 || v > 0)
                    {
                        Button botonPista = celda.gameObject.AddComponent<Button>();
                        botonPista.targetGraphic = imagen;
                        botonPista.transition = Selectable.Transition.None;
                        botonPista.onClick.AddListener(() => AbrirConsultaCombinaciones(fila, columna));
                    }
                    if (h == 0 && v == 0)
                    {
                        imagen.sprite = casillaLlena;
                        imagen.color = Color.white;
                    }
                    else if (h != 0 && v != 0)
                    {
                        imagen.sprite = casillaLlena;
                        imagen.color = Color.white;
                        RectTransform diagonal = Rect("Diagonal", celda);
                        diagonal.anchorMin = diagonal.anchorMax = new Vector2(.5f, .5f);
                        diagonal.pivot = new Vector2(.5f, .5f); diagonal.anchoredPosition = Vector2.zero;
                        // Longitud relativa al ancho de la casilla: se actualiza en AjustarDistribucion.
                        Image linea = diagonal.gameObject.AddComponent<Image>(); linea.color = Color.black; linea.raycastTarget = false;
                        diagonal.localRotation = Quaternion.Euler(0, 0, -45);
                    }
                    else { imagen.sprite = h != 0 ? pistaFila : pistaColumna; imagen.color = Color.white; }
                    if (v != 0)
                        CrearTexto(
                            "Pista columna", celda, v.ToString(),
                            new Vector2(.02f, .02f), new Vector2(.48f, .48f),
                            32, Color.black, TextAlignmentOptions.Center
                        );

                    if (h != 0)
                        CrearTexto(
                            "Pista fila", celda, h.ToString(),
                            new Vector2(.52f, .52f), new Vector2(.98f, .98f),
                            32, Color.black, TextAlignmentOptions.Center
                        );
                }
            }
        Canvas.ForceUpdateCanvases(); AjustarDistribucion();
    }

    // Se usan las cinco plantillas juntas; si falta alguna, se conserva el tablero anterior.
    private bool PrefabsCasillasValidos()
    {
        bool alguno = casillaJugablePrefab != null || casillaGrisPrefab != null ||
            pistaFilaPrefab != null || pistaColumnaPrefab != null || pistaDoblePrefab != null;
        if (!alguno) return false;
        bool valido = casillaJugablePrefab != null && casillaJugablePrefab.transform is RectTransform &&
            casillaJugablePrefab.fondo != null && casillaJugablePrefab.boton != null && casillaJugablePrefab.numero != null &&
            casillaGrisPrefab != null && casillaGrisPrefab.transform is RectTransform && casillaGrisPrefab.fondo != null &&
            pistaFilaPrefab != null && pistaFilaPrefab.transform is RectTransform && pistaFilaPrefab.fondo != null &&
            pistaFilaPrefab.boton != null && pistaFilaPrefab.pistaHorizontal != null &&
            pistaColumnaPrefab != null && pistaColumnaPrefab.transform is RectTransform && pistaColumnaPrefab.fondo != null &&
            pistaColumnaPrefab.boton != null && pistaColumnaPrefab.pistaVertical != null &&
            pistaDoblePrefab != null && pistaDoblePrefab.transform is RectTransform && pistaDoblePrefab.fondo != null &&
            pistaDoblePrefab.boton != null && pistaDoblePrefab.pistaHorizontal != null && pistaDoblePrefab.pistaVertical != null;
        if (!valido) Debug.LogWarning("Kakuro: faltan prefabs o referencias de casillas. Se utilizará el tablero anterior.");
        return valido;
    }

    private void DibujarCasillaPrefab(int fila, int columna)
    {
        bool blanca = tablero.EsBlanca(fila, columna);
        int horizontal = blanca ? 0 : tablero.PistaHorizontal(fila, columna);
        int vertical = blanca ? 0 : tablero.PistaVertical(fila, columna);
        KakuroCasillaEditable plantilla = blanca ? casillaJugablePrefab :
            horizontal > 0 && vertical > 0 ? pistaDoblePrefab :
            horizontal > 0 ? pistaFilaPrefab : vertical > 0 ? pistaColumnaPrefab : casillaGrisPrefab;
        KakuroCasillaEditable celda = Instantiate(plantilla, zonaTablero, false);
        celda.name = "Casilla " + fila + "," + columna;
        fondos[fila, columna] = celda.fondo;
        if (blanca)
        {
            coloresNormalesCasillas[fila, columna] = celda.fondo.color;
            textos[fila, columna] = celda.numero;
            celda.numero.text = "";
            celda.boton.onClick.AddListener(() => TocarCasilla(fila, columna));
        }
        else if (horizontal > 0 || vertical > 0)
        {
            if (horizontal > 0) celda.pistaHorizontal.text = horizontal.ToString();
            if (vertical > 0) celda.pistaVertical.text = vertical.ToString();
            celda.boton.onClick.AddListener(() => AbrirConsultaCombinaciones(fila, columna));
        }
    }

    // Consulta teórica: no usa entradas, solución ni pistas; el reloj sigue funcionando.
    private void AbrirConsultaCombinaciones(int fila, int columna)
    {
        if (generando || tablero == null || ventanaCombinaciones != null) return;
        int horizontal = tablero.PistaHorizontal(fila, columna);
        int vertical = tablero.PistaVertical(fila, columna);
        if (horizontal <= 0 && vertical <= 0) return;
        CerrarPanelNumeros();
        KakuroPanelEditable editable = InstanciarPanel(panelCombinacionesPrefab, true);
        if (editable != null)
        {
            consultaUsaPrefab = true;
            ventanaCombinaciones = editable.gameObject;
            textoCombinaciones = editable.detalle;
            editable.botonFila.gameObject.SetActive(horizontal > 0);
            editable.botonColumna.gameObject.SetActive(vertical > 0);
            if (horizontal > 0)
            {
                editable.textoFila.text = "Fila → " + horizontal;
                editable.botonFila.onClick.AddListener(() => MostrarCombinaciones(fila, columna, 0, 1, horizontal, "Fila"));
            }
            if (vertical > 0)
            {
                editable.textoColumna.text = "Columna ↓ " + vertical;
                editable.botonColumna.onClick.AddListener(() => MostrarCombinaciones(fila, columna, 1, 0, vertical, "Columna"));
            }
            editable.botonPrincipal.onClick.AddListener(CerrarConsultaCombinaciones);
            if (horizontal > 0) MostrarCombinaciones(fila, columna, 0, 1, horizontal, "Fila");
            else MostrarCombinaciones(fila, columna, 1, 0, vertical, "Columna");
            return;
        }
        consultaUsaPrefab = false;
        RectTransform velo = Rect("Consulta de combinaciones", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaCombinaciones = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel consulta", velo);
        float mx = Mathf.Clamp(margenHorizontalConsulta, .02f, .2f);
        float my = Mathf.Clamp(margenVerticalConsulta, .04f, .3f);
        Anclar(panel, new Vector2(mx, my), new Vector2(1f - mx, 1f - my));
        panel.gameObject.AddComponent<Image>().color = fondoConsulta;
        CrearTexto("Título consulta", panel, "Combinaciones", new Vector2(.05f, .85f),
            new Vector2(.95f, .98f), tamanoTituloConsulta, acento, TextAlignmentOptions.Center);
        RectTransform direcciones = Rect("Direcciones", panel);
        Anclar(direcciones, new Vector2(.05f, .71f), new Vector2(.95f, .84f));
        var layout = direcciones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        if (horizontal > 0)
            CrearBoton("Fila → " + horizontal, direcciones,
                () => MostrarCombinaciones(fila, columna, 0, 1, horizontal, "Fila"));
        if (vertical > 0)
            CrearBoton("Columna ↓ " + vertical, direcciones,
                () => MostrarCombinaciones(fila, columna, 1, 0, vertical, "Columna"));

        // Viewport sin barra: se desplaza con el dedo sin ocupar espacio horizontal.
        RectTransform viewport = Rect("Área desplazable", panel);
        Anclar(viewport, new Vector2(.05f, .17f), new Vector2(.95f, .69f));
        Image area = viewport.gameObject.AddComponent<Image>();
        area.color = new Color(1f, 1f, 1f, .01f); area.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35f;
        RectTransform contenido = Rect("Contenido consulta", viewport);
        contenido.anchorMin = new Vector2(0f, 1f); contenido.anchorMax = new Vector2(1f, 1f);
        contenido.pivot = new Vector2(.5f, 1f); contenido.offsetMin = new Vector2(12f, 0f);
        contenido.offsetMax = new Vector2(-12f, 0f); contenido.anchoredPosition = Vector2.zero;
        textoCombinaciones = contenido.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuenteInterfaz != null) textoCombinaciones.font = fuenteInterfaz;
        textoCombinaciones.color = colorTextoConsulta;
        textoCombinaciones.fontSize = Mathf.Clamp(tamanoTextoConsulta, 12, 48);
        textoCombinaciones.enableAutoSizing = false; textoCombinaciones.enableWordWrapping = true;
        textoCombinaciones.alignment = TextAlignmentOptions.TopLeft;
        textoCombinaciones.raycastTarget = false; textoCombinaciones.richText = false;
        ContentSizeFitter ajuste = contenido.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contenido;
        RectTransform zonaCerrar = Rect("Cerrar consulta", panel);
        Anclar(zonaCerrar, new Vector2(.20f, .025f), new Vector2(.80f, .14f));
        Anclar(CrearBoton("Cerrar", zonaCerrar, CerrarConsultaCombinaciones).GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one);
        if (horizontal > 0) MostrarCombinaciones(fila, columna, 0, 1, horizontal, "Fila");
        else MostrarCombinaciones(fila, columna, 1, 0, vertical, "Columna");
        ActualizarAspectoConsulta();
    }

    // Solo actualiza el panel abierto; no recrea el tablero ni modifica la partida.
    private void ActualizarAspectoConsulta()
    {
        if (ventanaCombinaciones == null || consultaUsaPrefab) return;
        Transform panel = ventanaCombinaciones.transform.Find("Panel consulta");
        if (panel == null) return;
        var rectPanel = panel as RectTransform;
        float mx = Mathf.Clamp(margenHorizontalConsulta, .02f, .2f);
        float my = Mathf.Clamp(margenVerticalConsulta, .04f, .3f);
        if (rectPanel != null) Anclar(rectPanel, new Vector2(mx, my), new Vector2(1f - mx, 1f - my));
        Image imagenPanel = panel.GetComponent<Image>();
        if (imagenPanel != null) imagenPanel.color = fondoConsulta;
        TMP_FontAsset fuente = fuenteInterfaz != null ? fuenteInterfaz : TMP_Settings.defaultFontAsset;
        Transform tituloObj = panel.Find("Título consulta");
        TMP_Text titulo = tituloObj != null ? tituloObj.GetComponent<TMP_Text>() : null;
        if (titulo != null)
        {
            titulo.color = acento;
            titulo.fontSizeMax = Mathf.Clamp(tamanoTituloConsulta, 16, 60);
            if (fuente != null) titulo.font = fuente;
        }
        if (textoCombinaciones != null)
        {
            textoCombinaciones.color = colorTextoConsulta;
            textoCombinaciones.fontSize = Mathf.Clamp(tamanoTextoConsulta, 12, 48);
            if (fuente != null) textoCombinaciones.font = fuente;
            LayoutRebuilder.MarkLayoutForRebuild(textoCombinaciones.rectTransform);
        }
        foreach (Button boton in panel.GetComponentsInChildren<Button>(true))
        {
            Image imagenBoton = boton.GetComponent<Image>();
            if (imagenBoton != null) imagenBoton.color = acento;
            TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null && fuente != null) etiqueta.font = fuente;
        }
    }

    private void MostrarCombinaciones(int fila, int columna, int df, int dc, int suma, string direccion)
    {
        if (tablero == null || textoCombinaciones == null) return;
        int cantidad = 0, f = fila + df, c = columna + dc;
        while (f < tablero.Filas && c < tablero.Columnas && tablero.EsBlanca(f, c))
        { cantidad++; f += df; c += dc; }
        var combinaciones = new List<string>();
        int union = 0, interseccion = 0;
        for (int mascara = 1; mascara < (1 << 9); mascara++)
        {
            int cuenta = 0, total = 0;
            var numeros = new List<string>();
            for (int i = 0; i < 9; i++)
                if ((mascara & (1 << i)) != 0)
                { cuenta++; total += i + 1; numeros.Add((i + 1).ToString()); }
            if (cuenta != cantidad || total != suma) continue;
            union |= mascara;
            interseccion = combinaciones.Count == 0 ? mascara : interseccion & mascara;
            combinaciones.Add(string.Join(" + ", numeros.ToArray()));
        }
        if (combinaciones.Count == 0)
        {
            textoCombinaciones.text = direccion + " · " + cantidad + " casillas · suma " + suma +
                "\n\nNo hay combinaciones válidas para esta pista.";
        }
        else
        {
            var informe = new System.Text.StringBuilder();
            informe.Append(direccion).Append(" · ").Append(cantidad).Append(" casillas · suma ").Append(suma);
            informe.Append("\n\nSiempre: ").Append(CifrasDeMascara(interseccion));
            informe.Append("\nNunca: ").Append(CifrasDeMascara((~union) & 511));
            informe.Append("\n\nCombinaciones (").Append(combinaciones.Count).Append("):");
            foreach (string combinacion in combinaciones) informe.Append("\n").Append(combinacion);
            informe.Append("\n\nEl orden puede variar. No se tienen en cuenta las cifras escritas ni los cruces.");
            textoCombinaciones.text = informe.ToString();
        }
        RectTransform contenido = textoCombinaciones.rectTransform;
        contenido.anchoredPosition = Vector2.zero;
        LayoutRebuilder.MarkLayoutForRebuild(contenido);
    }

    private static string CifrasDeMascara(int mascara)
    {
        var cifras = new List<string>();
        for (int i = 0; i < 9; i++)
            if ((mascara & (1 << i)) != 0) cifras.Add((i + 1).ToString());
        return cifras.Count == 0 ? "Ninguna" : string.Join(", ", cifras.ToArray());
    }

    private void CerrarConsultaCombinaciones()
    {
        if (ventanaCombinaciones == null) return;
        ventanaCombinaciones.SetActive(false); Destroy(ventanaCombinaciones);
        ventanaCombinaciones = null; textoCombinaciones = null; consultaUsaPrefab = false;
    }

    private void TocarCasilla(int f, int c)
    {
        if (generando || terminado || tablero == null) return;
        if (voz != null && voz.Activa) voz.CancelarEscucha();
        Seleccionar(f, c);
        if (casillasReveladas[f, c])
        {
            estado.text = "Una casilla revelada por una pista no se puede modificar.";
            return;
        }
        if (voz != null && voz.Activa) voz.EscucharCasilla(f, c);
        else AbrirPanelNumeros();
    }

    // El velo transparente cierra el panel al tocar fuera sin activar otros botones.
    // El cronómetro sigue contando mientras se elige un número.
    private void AbrirPanelNumeros()
    {
        CerrarPanelNumeros();
        KakuroDialogoExtra ui = InstanciarDialogoExtra(selectorNumerosPrefab, raiz, 0);
        if (ui != null)
        {
            selectorNumerosActivo = ui;
            panelNumeros = ui.gameObject; rectPanelNumeros = ui.panelNumerico;
            ui.cerrarExterior.onClick.AddListener(CerrarPanelNumeros);
            for (int n = 1; n <= 9; n++)
            {
                int valor = n;
                ui.numeros[n - 1].onClick.AddListener(() => Poner(valor));
            }
            ui.borrar.onClick.AddListener(() => Poner(0));
            AjustarPanelNumeros();
            return;
        }
        RectTransform velo = Rect("Selector de números", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        panelNumeros = velo.gameObject; velo.SetAsLastSibling();
        Image areaExterior = velo.gameObject.AddComponent<Image>();
        areaExterior.color = new Color(0f, 0f, 0f, 0f);
        Button cerrar = velo.gameObject.AddComponent<Button>();
        cerrar.targetGraphic = areaExterior;
        cerrar.transition = Selectable.Transition.None;
        cerrar.onClick.AddListener(CerrarPanelNumeros);

        rectPanelNumeros = Rect("Panel numérico", velo);
        rectPanelNumeros.anchorMin = rectPanelNumeros.anchorMax = new Vector2(.5f, .495f);
        rectPanelNumeros.pivot = new Vector2(.5f, .5f);
        rectPanelNumeros.anchoredPosition = Vector2.zero;
        Image fondoPanel = rectPanelNumeros.gameObject.AddComponent<Image>();
        fondoPanel.color = acento; // También intercepta toques entre las teclas.

        RectTransform numeros = Rect("Números", rectPanelNumeros);
        Anclar(numeros, new Vector2(.02f, .255f), new Vector2(.98f, .985f));
        var rejilla = numeros.gameObject.AddComponent<GridLayoutGroup>();
        rejilla.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rejilla.constraintCount = 3; rejilla.childAlignment = TextAnchor.MiddleCenter;
        rejilla.spacing = new Vector2(4, 4);
        // El tamaño de las teclas se ajusta junto al panel, también al rotar.
        for (int n = 1; n <= 9; n++)
        {
            int valor = n;
            CrearBotonNumero(n.ToString(), numeros, () => Poner(valor));
        }
        RectTransform borrar = Rect("Zona borrar", rectPanelNumeros);
        Anclar(borrar, new Vector2(.34f, .015f), new Vector2(.66f, .245f));
        Anclar(CrearBotonNumero("X", borrar, () => Poner(0)).GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        AjustarPanelNumeros();
    }

    private Button CrearBotonNumero(string texto, Transform padre, UnityEngine.Events.UnityAction accion)
    {
        Button boton = CrearBoton(texto, padre, accion);
        boton.GetComponent<Image>().color = Color.black;
        TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>();
        if (fuenteNumeros != null) etiqueta.font = fuenteNumeros;
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.fontSizeMax = 64;
        return boton;
    }

    private void AjustarPanelNumeros()
    {
        if (rectPanelNumeros == null || raiz == null) return;
        if (selectorNumerosActivo != null && !selectorNumerosActivo.ajustarTamanoAutomaticamente) return;
        float lado = Mathf.Max(1f, Mathf.Min(raiz.rect.width * .12f, raiz.rect.height * .075f));
        rectPanelNumeros.sizeDelta = new Vector2(lado * 3f, lado * 4f);
        GridLayoutGroup rejilla = selectorNumerosActivo != null ? selectorNumerosActivo.rejillaNumeros :
            rectPanelNumeros.Find("Números").GetComponent<GridLayoutGroup>();
        // El área ocupa el 96 % del ancho del panel y tiene dos separaciones.
        float ladoTecla = Mathf.Max(1f, (lado * 3f * .96f - 2f * rejilla.spacing.x) / 3f);
        rejilla.cellSize = new Vector2(ladoTecla, ladoTecla);
    }

    private void CerrarPanelNumeros()
    {
        if (panelNumeros == null) return;
        panelNumeros.SetActive(false);
        Destroy(panelNumeros);
        panelNumeros = null; rectPanelNumeros = null; selectorNumerosActivo = null;
    }

    private void Seleccionar(int f, int c)
    {
        if (generando || terminado) return;
        int anteriorFila = seleccionFila, anteriorColumna = seleccionColumna;
        seleccionFila = f; seleccionColumna = c;
        if (anteriorFila >= 0) ActualizarColorCasilla(anteriorFila, anteriorColumna);
        ActualizarColorCasilla(f, c);
    }

    private void Poner(int numero)
    {
        if (generando || terminado || seleccionFila < 0) return;
        if (casillasReveladas[seleccionFila, seleccionColumna])
        {
            estado.text = "Una casilla revelada por una pista no se puede modificar.";
            return;
        }
        CerrarPanelNumeros();
        int anterior = entradas[seleccionFila, seleccionColumna];
        if (numero != anterior)
        {
            historialDeshacer.Add(new RegistroDeshacer { fila = seleccionFila, columna = seleccionColumna, anterior = anterior, posterior = numero });
            if (historialDeshacer.Count > MaximoDeshacer) historialDeshacer.RemoveAt(0);
        }
        entradas[seleccionFila, seleccionColumna] = numero;
        textos[seleccionFila, seleccionColumna].text = numero == 0 ? "" : numero.ToString();
        LimpiarErrores();
        estado.text = "Completa las sumas sin repetir cifras.";
        ActualizarDeshacerUI();
        GuardarPartidaActual();
    }

    private void ConfirmarReset()
    {
        if (generando || terminado || tablero == null || ventanaReset != null) return;
        bool hayNumeros = false;
        for (int f = 0; f < tamano && !hayNumeros; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c) && !casillasReveladas[f, c] && entradas[f, c] != 0)
                { hayNumeros = true; break; }
        if (!hayNumeros)
        {
            estado.text = "No hay números introducidos para borrar.";
            return;
        }
        KakuroPanelEditable editable = InstanciarPanel(panelResetPrefab, false);
        if (editable != null)
        {
            ventanaReset = editable.gameObject;
            editable.botonPrincipal.onClick.AddListener(CerrarReset);
            editable.botonSecundario.onClick.AddListener(AplicarReset);
            return;
        }
        RectTransform velo = Rect("Confirmar reset", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaReset = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel reset", velo);
        Anclar(panel, new Vector2(.08f, .32f), new Vector2(.92f, .68f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título reset", panel, "¿Borrar tus números?",
            new Vector2(.05f, .70f), new Vector2(.95f, .94f), 44, acento, TextAlignmentOptions.Center);
        CrearTexto("Detalle reset", panel, "Se conservan las pistas y el tiempo.",
            new Vector2(.05f, .46f), new Vector2(.95f, .67f), 30, Color.black, TextAlignmentOptions.Center);
        RectTransform botones = Rect("Acciones reset", panel);
        Anclar(botones, new Vector2(.06f, .10f), new Vector2(.94f, .38f));
        var layout = botones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Cancelar", botones, CerrarReset);
        CrearBoton("Borrar números", botones, AplicarReset);
    }

    private void CerrarReset()
    {
        if (ventanaReset == null) return;
        ventanaReset.SetActive(false); Destroy(ventanaReset); ventanaReset = null;
        ignorarSiguienteDelta = true;
    }

    private void AplicarReset()
    {
        if (tablero == null || terminado || generando) { CerrarReset(); return; }
        historialDeshacer.Clear(); // Reset es una acción irreversible, previa confirmación.
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c) && !casillasReveladas[f, c])
                {
                    entradas[f, c] = 0;
                    textos[f, c].text = "";
                }
        CerrarReset();
        LimpiarErrores();
        estado.text = "Números borrados. Se conservan las pistas y el tiempo.";
        ActualizarDeshacerUI();
        GuardarPartidaActual();
    }

    private void ConfirmarRendicion()
    {
        if (generando || terminado || tablero == null || solucion == null || ventanaRendicion != null) return;
        KakuroPanelEditable editable = InstanciarPanel(panelConfirmarRendicionPrefab, false);
        if (editable != null)
        {
            ventanaRendicion = editable.gameObject;
            editable.botonPrincipal.onClick.AddListener(CerrarRendicion);
            editable.botonSecundario.onClick.AddListener(AplicarRendicion);
            return;
        }
        RectTransform velo = Rect("Confirmar rendición", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaRendicion = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel rendición", velo);
        Anclar(panel, new Vector2(.08f, .30f), new Vector2(.92f, .70f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título rendición", panel, "¿Quieres ver la solución?",
            new Vector2(.05f, .75f), new Vector2(.95f, .95f), 44, acento, TextAlignmentOptions.Center);
        CrearTexto("Detalle rendición", panel, "Se contará como no resuelto.\nNo entrará en el ranking.",
            new Vector2(.05f, .45f), new Vector2(.95f, .72f), 32, Color.black, TextAlignmentOptions.Center);
        RectTransform botones = Rect("Acciones rendición", panel);
        Anclar(botones, new Vector2(.06f, .09f), new Vector2(.94f, .36f));
        var layout = botones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Cancelar", botones, CerrarRendicion);
        CrearBoton("Me rindo", botones, AplicarRendicion);
    }

    private void CerrarRendicion()
    {
        if (ventanaRendicion == null) return;
        ventanaRendicion.SetActive(false); Destroy(ventanaRendicion); ventanaRendicion = null;
        ignorarSiguienteDelta = true;
    }

    private void AplicarRendicion()
    {
        if (tablero == null || solucion == null || terminado || generando) { CerrarRendicion(); return; }
        // La solución se comprobó al generar o restaurar. Se revisa antes de usarla.
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c) && (solucion[f, c] < 1 || solucion[f, c] > 9))
                {
                    CerrarRendicion();
                    estado.text = "No se pudo mostrar la solución. Revisa la consola.";
                    Debug.LogError("Kakuro: falta una solución válida para la casilla " + f + "," + c);
                    return;
                }
        // Marca la partida como finalizada antes de registrar el resultado: no se
        // podrá reabrir como activa ni registrar una victoria al mostrar la solución.
        terminado = true;
        if (!GuardarPartidaActual())
        {
            terminado = false;
            CerrarRendicion();
            estado.text = "No se pudo guardar la partida. Reintenta Me rindo.";
            ActualizarPistasUI();
            return;
        }
        int noResueltos;
        bool estadisticaGuardada = RegistrarNoResuelto(tamano, out noResueltos);
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c))
                {
                    entradas[f, c] = solucion[f, c];
                    textos[f, c].text = solucion[f, c].ToString();
                }
        CerrarRendicion();
        LimpiarErrores(); ActualizarPistasUI();
        estado.text = "Solución mostrada. Partida no resuelta.";
        MostrarResultadoRendicion(estadisticaGuardada, noResueltos);
    }

    private void MostrarResultadoRendicion(bool estadisticaGuardada, int noResueltos)
    {
        CerrarVentanaVictoria();
        KakuroPanelEditable editable = InstanciarPanel(panelResultadoRendicionPrefab, false);
        if (editable != null)
        {
            ventanaVictoria = editable.gameObject;
            editable.detalle.text = NombreModo(tamano) + " · Nivel " + nivelActual + "\nNo se añadirá al ranking." +
                (estadisticaGuardada ? "\nNo resueltos en este modo: " + noResueltos : "\nNo se pudo guardar la estadística.");
            editable.botonPrincipal.onClick.AddListener(() => { CerrarVentanaVictoria(); SolicitarTablero(tamano); });
            editable.botonSecundario.onClick.AddListener(CerrarVentanaVictoria);
            return;
        }
        RectTransform velo = Rect("Resultado rendición", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaVictoria = velo.gameObject; velo.SetAsLastSibling();
        Image sombra = velo.gameObject.AddComponent<Image>();
        sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        RectTransform panel = Rect("Panel resultado", velo);
        Anclar(panel, new Vector2(.08f, .31f), new Vector2(.92f, .69f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        CrearTexto("Título resultado", panel, "Partida no resuelta",
            new Vector2(.05f, .72f), new Vector2(.95f, .94f), 48, acento, TextAlignmentOptions.Center);
        string detalle = NombreModo(tamano) + " · Nivel " + nivelActual + "\nNo se añadirá al ranking." +
            (estadisticaGuardada ? "\nNo resueltos en este modo: " + noResueltos : "\nNo se pudo guardar la estadística.");
        CrearTexto("Detalle resultado", panel, detalle,
            new Vector2(.05f, .39f), new Vector2(.95f, .70f), 34, Color.black, TextAlignmentOptions.Center);
        RectTransform botones = Rect("Acciones resultado", panel);
        Anclar(botones, new Vector2(.06f, .10f), new Vector2(.94f, .32f));
        var layout = botones.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        CrearBoton("Nuevo puzzle", botones, () => { CerrarVentanaVictoria(); SolicitarTablero(tamano); });
        CrearBoton("Ver solución", botones, CerrarVentanaVictoria);
    }

    private void PedirPista()
    {
        if (voz != null) voz.CancelarEscucha();
        if (generando || terminado || tablero == null || solucion == null || pistasRestantes <= 0) return;
        int f = -1, c = -1;
        // Si la casilla seleccionada está vacía, se usa esa; si no, una vacía al azar.
        if (seleccionFila >= 0 && entradas[seleccionFila, seleccionColumna] == 0)
        { f = seleccionFila; c = seleccionColumna; }
        else
        {
            var vacias = new List<Tuple<int, int>>();
            for (int fila = 0; fila < tamano; fila++)
                for (int columna = 0; columna < tamano; columna++)
                    if (tablero.EsBlanca(fila, columna) && entradas[fila, columna] == 0)
                        vacias.Add(Tuple.Create(fila, columna));
            if (vacias.Count == 0)
            {
                estado.text = "No hay casillas vacías. Borra una para usar una pista.";
                return;
            }
            var elegida = vacias[UnityEngine.Random.Range(0, vacias.Count)];
            f = elegida.Item1; c = elegida.Item2;
        }
        int valor = solucion[f, c];
        if (valor < 1 || valor > 9)
        {
            Debug.LogError("La solución no contiene un número válido para la casilla " + f + "," + c);
            estado.text = "No se pudo obtener la pista. Revisa la consola.";
            return; // No consume una ayuda si no puede mostrarla.
        }
        // Una casilla revelada no puede ser modificada ni restaurada por Deshacer.
        historialDeshacer.RemoveAll(x => x.fila == f && x.columna == c);
        entradas[f, c] = valor;
        casillasReveladas[f, c] = true;
        textos[f, c].text = valor.ToString();
        textos[f, c].fontStyle = FontStyles.Bold;
        pistasRestantes--;
        LimpiarErrores();
        Seleccionar(f, c);
        ActualizarPistasUI();
        estado.text = "Pista revelada. Pulsa Comprobar cuando termines.";
        GuardarPartidaActual();
    }

    private void ActualizarPistasUI()
    {
        ActualizarDeshacerUI();
        if (textoPistas != null) textoPistas.text = "Pistas (" + pistasRestantes + ")";
        if (botonPistas != null)
            botonPistas.interactable = !generando && !terminado && tablero != null && pistasRestantes > 0;
    }

    private void Comprobar()
    {
        if (voz != null) voz.CancelarEscucha();
        if (generando || tablero == null || terminado) return;
        Array.Clear(casillasConError, 0, casillasConError.Length);
        bool hayVacias = false, hayErrores = false;
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
            {
                if (tablero.EsBlanca(f, c))
                {
                    if (entradas[f, c] == 0) hayVacias = true;
                    continue;
                }
                int h = tablero.PistaHorizontal(f, c), v = tablero.PistaVertical(f, c);
                if (h > 0 && GrupoTieneError(f, c, 0, 1, h))
                { MarcarGrupo(f, c, 0, 1); hayErrores = true; }
                if (v > 0 && GrupoTieneError(f, c, 1, 0, v))
                { MarcarGrupo(f, c, 1, 0); hayErrores = true; }
            }
        ActualizarTodosLosColores();
        if (hayErrores)
        {
            estado.text = "Revisa los grupos en rojo: hay una suma imposible o cifras repetidas.";
            return;
        }
        if (hayVacias)
        {
            estado.text = "No se detectan errores en las sumas revisadas; quedan casillas vacías.";
            return;
        }
        // Guardar primero el estado final evita restaurar como activa una victoria
        // ya registrada. Si no se puede guardar, permitir reintentar sin contarla.
        terminado = true;
        if (!GuardarPartidaActual())
        {
            terminado = false;
            estado.text = "Puzzle correcto, pero no se pudo guardar. Reintenta Comprobar.";
            ActualizarPistasUI();
            return;
        }
        ActualizarPistasUI();
        progresoGuardado = RegistrarVictoria(tamano, out victoriasEnEsteModo);
        tiempoGuardado = GuardarResultadoLocal();
        estado.text = "¡Enhorabuena! Puzzle completado.";
        MostrarVentanaVictoria(false);
    }

    // En un grupo incompleto solo se marca error si ninguna combinación de cifras
    // distintas puede alcanzar la suma. No revela cuál es la casilla equivocada.
    private bool GrupoTieneError(int f, int c, int df, int dc, int objetivo)
    {
        int suma = 0, mascara = 0, vacias = 0;
        f += df; c += dc;
        while (f < tamano && c < tamano && tablero.EsBlanca(f, c))
        {
            int n = entradas[f, c];
            if (n == 0) vacias++;
            else
            {
                if (n < 1 || n > 9 || (mascara & (1 << n)) != 0) return true;
                mascara |= 1 << n; suma += n;
            }
            f += df; c += dc;
        }
        return !ExisteCombinacion(1, vacias, objetivo - suma, mascara);
    }

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

    private void MarcarGrupo(int f, int c, int df, int dc)
    {
        f += df; c += dc;
        while (f < tamano && c < tamano && tablero.EsBlanca(f, c))
        {
            casillasConError[f, c] = true;
            f += df; c += dc;
        }
    }

    private void LimpiarErrores()
    {
        Array.Clear(casillasConError, 0, casillasConError.Length);
        ActualizarTodosLosColores();
    }

    private void ActualizarTodosLosColores()
    {
        for (int f = 0; f < tamano; f++)
            for (int c = 0; c < tamano; c++)
                if (tablero.EsBlanca(f, c)) ActualizarColorCasilla(f, c);
    }

    private void ActualizarColorCasilla(int f, int c)
    {
        bool estaSeleccionada = f == seleccionFila && c == seleccionColumna;
        if (casillasConError[f, c]) fondos[f, c].color = estaSeleccionada ? colorErrorSeleccionado : colorError;
        else if (estaSeleccionada) fondos[f, c].color = seleccionado;
        else if (casillasReveladas[f, c]) fondos[f, c].color = colorPista;
        else fondos[f, c].color = coloresNormalesCasillas != null ? coloresNormalesCasillas[f, c] : Color.white;
    }

    // Los contadores son locales al dispositivo y están separados por tamaño.
    private static string ClaveVictorias(int tamanoTablero)
    {
        return "Kakuro.V1.Victorias." + tamanoTablero + "x" + tamanoTablero;
    }

    private static bool RegistrarVictoria(int tamanoTablero, out int total)
    {
        total = 0;
        try
        {
            string clave = ClaveVictorias(tamanoTablero);
            int anterior = Mathf.Max(0, PlayerPrefs.GetInt(clave, 0));
            total = anterior < int.MaxValue ? anterior + 1 : int.MaxValue;
            PlayerPrefs.SetInt(clave, total);
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError("Kakuro: no se pudo guardar el progreso. " + error);
            return false;
        }
    }

    private static string ClaveNoResueltos(int tamanoTablero)
    {
        return "Kakuro.V1.NoResueltos." + tamanoTablero + "x" + tamanoTablero;
    }

    private static bool RegistrarNoResuelto(int tamanoTablero, out int total)
    {
        total = 0;
        try
        {
            string clave = ClaveNoResueltos(tamanoTablero);
            int anterior = Mathf.Max(0, PlayerPrefs.GetInt(clave, 0));
            total = anterior < int.MaxValue ? anterior + 1 : int.MaxValue;
            PlayerPrefs.SetInt(clave, total); PlayerPrefs.Save();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError("Kakuro: no se pudo guardar la estadística de no resueltos. " + error);
            return false;
        }
    }

    [ContextMenu("Mostrar no resueltos guardados")]
    private void MostrarNoResueltosGuardados()
    {
        Debug.Log("Kakuro - No resueltos guardados: Fácil=" + PlayerPrefs.GetInt(ClaveNoResueltos(7), 0) +
            "; Medio=" + PlayerPrefs.GetInt(ClaveNoResueltos(9), 0) +
            "; Difícil=" + PlayerPrefs.GetInt(ClaveNoResueltos(12), 0));
    }

    // Consulta de prueba sin modificar los contadores.
    [ContextMenu("Mostrar progreso guardado")]
    private void MostrarProgresoGuardado()
    {
        try
        {
            Debug.Log("Kakuro - Victorias guardadas: Fácil=" + PlayerPrefs.GetInt(ClaveVictorias(7), 0) +
                "; Medio=" + PlayerPrefs.GetInt(ClaveVictorias(9), 0) +
                "; Difícil=" + PlayerPrefs.GetInt(ClaveVictorias(12), 0));
        }
        catch (Exception error)
        {
            Debug.LogError("Kakuro: no se pudo consultar el progreso. " + error);
        }
    }

    // Los resultados nuevos se incorporan al top 8; el historial anterior se conserva.
    private bool GuardarResultadoLocal()
    {
        string error;
        long milisegundos = (long)Math.Round(Math.Max(0.0, segundosJugados) * 1000.0);
        if (KakuroRanking.Agregar(tamano, milisegundos, out error)) return true;
        Debug.LogError("Kakuro: no se pudo guardar el tiempo en el ranking. " + error);
        return false;
    }

    [ContextMenu("Mostrar tiempos guardados")]
    private void MostrarTiemposGuardados()
    {
        var mensaje = new System.Text.StringBuilder("Kakuro - Tiempos en el top 8:");
        foreach (int medida in new[] { 7, 9, 12 })
        {
            List<long> tiempos; string error;
            if (!KakuroRanking.Obtener(medida, out tiempos, out error))
            {
                Debug.LogError("Kakuro: no se pudo leer el ranking. " + error);
                return;
            }
            mensaje.Append(" ").Append(NombreModo(medida)).Append("=").Append(tiempos.Count).Append(";");
        }
        Debug.Log(mensaje.ToString());
    }

    private void MostrarVentanaVictoria(bool vistaPrevia)
    {
        CerrarVentanaVictoria();
        KakuroPanelEditable editable = InstanciarPanel(panelVictoriaPrefab, false);
        if (editable != null)
        {
            ventanaVictoria = editable.gameObject;
            if (vistaPrevia) editable.titulo.text = "Vista previa";
            int totalPistasPrefab = tamano == 7 ? 5 : (tamano == 9 ? 3 : 1);
            editable.detalle.text = vistaPrevia
                ? "Esta es una vista previa.\nEl puzzle actual no se ha completado."
                : "¡Enhorabuena!\n" + NombreModo(tamano) + " · " + tamano + " × " + tamano +
                  " · Nivel " + nivelActual + "\nTiempo: " + FormatearTiempo(segundosJugados) +
                  "\nPistas utilizadas: " + (totalPistasPrefab - pistasRestantes) + " / " + totalPistasPrefab +
                  (progresoGuardado ? "\nVictorias en este modo: " + victoriasEnEsteModo : "\nNo se pudo guardar el progreso.") +
                  (tiempoGuardado ? "" : "\nNo se pudo guardar el tiempo.");
            editable.botonPrincipal.onClick.AddListener(() => { CerrarVentanaVictoria(); SolicitarTablero(tamano); });
            editable.botonSecundario.onClick.AddListener(CerrarVentanaVictoria);
            if (vistaPrevia)
            {
                TMP_Text textoBoton = editable.botonSecundario.GetComponentInChildren<TMP_Text>();
                if (textoBoton != null) textoBoton.text = "Seguir jugando";
            }
            Canvas.ForceUpdateCanvases();
            return;
        }

        RectTransform velo = Rect("Ventana Victoria", raiz);
        Anclar(velo, Vector2.zero, Vector2.one);
        ventanaVictoria = velo.gameObject;
        velo.SetAsLastSibling();
        Image oscurecer = velo.gameObject.AddComponent<Image>();
        oscurecer.color = new Color(0f, 0f, 0f, .65f);
        // El velo bloquea los botones del tablero mientras el panel está abierto.
        oscurecer.raycastTarget = true;

        RectTransform panel = Rect("Panel Victoria", velo);
        Anclar(panel, new Vector2(.08f, .31f), new Vector2(.92f, .69f));
        Image fondoPanel = panel.gameObject.AddComponent<Image>();
        fondoPanel.color = Color.white;

        string titulo = vistaPrevia ? "Vista previa" : "¡Puzzle completado!";
        TMP_Text tituloTexto = CrearTexto("Título victoria", panel, titulo,
            new Vector2(.06f, .70f), new Vector2(.94f, .93f), 62, acento, TextAlignmentOptions.Center);
        tituloTexto.fontStyle = FontStyles.Bold;

        int totalPistas = tamano == 7 ? 5 : (tamano == 9 ? 3 : 1);
        string modo = NombreModo(tamano);
        string detalle = vistaPrevia
            ? "Esta es una vista previa.\nEl puzzle actual no se ha completado."
            : "¡Enhorabuena!\n" + modo + " · " + tamano + " × " + tamano +
              " · Nivel " + nivelActual +
              "\nTiempo: " + FormatearTiempo(segundosJugados) +
              "\nPistas utilizadas: " + (totalPistas - pistasRestantes) + " / " + totalPistas +
              (progresoGuardado ? "\nVictorias en este modo: " + victoriasEnEsteModo : "\nNo se pudo guardar el progreso.") +
              (tiempoGuardado ? "" : "\nNo se pudo guardar el tiempo.");
        CrearTexto("Detalle victoria", panel, detalle,
            new Vector2(.06f, .37f), new Vector2(.94f, .70f), 38, Color.black, TextAlignmentOptions.Center);

        RectTransform accionesVictoria = Rect("Acciones victoria", panel);
        Anclar(accionesVictoria, new Vector2(.06f, .10f), new Vector2(.94f, .32f));
        var distribucion = accionesVictoria.gameObject.AddComponent<HorizontalLayoutGroup>();
        distribucion.spacing = 12;
        distribucion.childForceExpandWidth = true;
        distribucion.childForceExpandHeight = true;
        CrearBoton("Nuevo puzzle", accionesVictoria, () =>
        {
            CerrarVentanaVictoria();
            SolicitarTablero(tamano);
        });
        CrearBoton(vistaPrevia ? "Seguir jugando" : "Ver tablero", accionesVictoria, CerrarVentanaVictoria);
        Canvas.ForceUpdateCanvases();
    }

    private void CerrarVentanaVictoria()
    {
        if (ventanaVictoria == null) return;
        // Desactivar inmediatamente evita que bloquee toques durante el frame de destrucción.
        ventanaVictoria.SetActive(false);
        Destroy(ventanaVictoria);
        ventanaVictoria = null;
    }

    // Solo muestra la interfaz de prueba: no cambia el estado del puzzle ni concede una victoria.
    [ContextMenu("Previsualizar ventana de victoria")]
    private void PrevisualizarVentanaVictoria()
    {
        if (!Application.isPlaying || raiz == null || tablero == null || generando)
        {
            Debug.LogWarning("Entra en Play y espera a que termine la generación para previsualizar la victoria.");
            return;
        }
        MostrarVentanaVictoria(true);
    }

    private void ActualizarZonaSegura()
    {
        Rect zona = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        raiz.anchorMin = new Vector2(zona.xMin / Screen.width, zona.yMin / Screen.height);
        raiz.anchorMax = new Vector2(zona.xMax / Screen.width, zona.yMax / Screen.height);
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;
    }
    private void AjustarDistribucion()
    {
        pantallaAncho = Screen.width; pantallaAlto = Screen.height;
        if (raiz == null || rejillaTablero == null) return;
        ActualizarZonaSegura(); Canvas.ForceUpdateCanvases();
        int columnas = tablero != null ? tablero.Columnas : tamano;
        float ancho = Mathf.Max(1, zonaTablero.rect.width), alto = Mathf.Max(1, zonaTablero.rect.height);
        float lado = Mathf.Max(1, Mathf.Min((ancho - 2 * (columnas - 1)) / columnas, (alto - 2 * (columnas - 1)) / columnas));
        rejillaTablero.cellSize = new Vector2(lado, lado);
        ActualizarDistribucionAcciones();
        AjustarPanelNumeros();
        foreach (Transform celda in zonaTablero)
        {
            KakuroCasillaEditable editable = celda.GetComponent<KakuroCasillaEditable>();
            if (editable != null)
            {
                if (editable.diagonal != null && editable.ajustarDiagonalAutomaticamente)
                    editable.diagonal.sizeDelta = new Vector2(lado * 1.4142f, Mathf.Max(1, lado * Mathf.Clamp(editable.grosorDiagonalRelativo, .001f, .2f)));
            }
            else
            {
                Transform diagonal = celda.Find("Diagonal");
                if (diagonal != null) ((RectTransform)diagonal).sizeDelta = new Vector2(lado * 1.4142f, Mathf.Max(1, lado * .025f));
            }
        }
    }
    // La rejilla se recalcula también si se edita durante Play.
    private void ActualizarDistribucionAcciones()
    {
        if (rejillaAcciones != null && zonaAcciones != null &&
            (interfazActiva == null || interfazActiva.ajustarBotonesAutomaticamente))
        {
            int columnasAcciones = rejillaAcciones.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, rejillaAcciones.constraintCount) : 3;
            int activos = 0;
            foreach (Transform hijo in zonaAcciones) if (hijo.gameObject.activeSelf) activos++;
            int filas = Mathf.Max(1, Mathf.CeilToInt(activos / (float)columnasAcciones));
            float anchoBoton = Mathf.Max(1f, (zonaAcciones.rect.width - rejillaAcciones.padding.horizontal -
                (columnasAcciones - 1) * rejillaAcciones.spacing.x) / columnasAcciones);
            float altoBoton = Mathf.Max(1f, (zonaAcciones.rect.height - rejillaAcciones.padding.vertical -
                (filas - 1) * rejillaAcciones.spacing.y) / filas);
            rejillaAcciones.cellSize = new Vector2(anchoBoton, altoBoton);
        }
    }
    private static RectTransform Rect(string nombre, Transform padre)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false); return obj.GetComponent<RectTransform>();
    }
    private static void Anclar(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private TMP_Text CrearTexto(string nombre, Transform padre, string texto, Vector2 min, Vector2 max, float tamMax, Color color, TextAlignmentOptions alineacion)
    {
        RectTransform rect = Rect(nombre, padre); Anclar(rect, min, max);
        var etiqueta = rect.gameObject.AddComponent<TextMeshProUGUI>(); etiqueta.text = texto; etiqueta.color = color;
        TMP_FontAsset fuente = fuenteInterfaz;
        if (nombre == "Número") fuente = fuenteNumeros != null ? fuenteNumeros : fuenteInterfaz;
        else if (nombre == "Pista columna" || nombre == "Pista fila") fuente = fuentePistas != null ? fuentePistas : fuenteInterfaz;
        else if (nombre == "Tiempo") fuente = fuenteTiempo != null ? fuenteTiempo : fuenteInterfaz;
        if (fuente != null) etiqueta.font = fuente;
        if (nombre == "Número" || nombre == "Pista columna" || nombre == "Pista fila" ||
            nombre == "Tiempo" || nombre == "Etiqueta" || nombre.StartsWith("Título"))
            etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.alignment = alineacion; etiqueta.enableAutoSizing = true; etiqueta.fontSizeMin = 8; etiqueta.fontSizeMax = tamMax;
        etiqueta.raycastTarget = false; etiqueta.enableWordWrapping = false;
        return etiqueta;
    }
    private Button CrearBoton(string texto, Transform padre, UnityEngine.Events.UnityAction accion)
    {
        RectTransform rect = Rect("Botón " + texto, padre);
        Image imagen = rect.gameObject.AddComponent<Image>(); imagen.color = acento;
        Button boton = rect.gameObject.AddComponent<Button>(); boton.targetGraphic = imagen; boton.onClick.AddListener(accion);
        CrearTexto("Etiqueta", rect, texto, Vector2.zero, Vector2.one, 34, Color.white, TextAlignmentOptions.Center);
        return boton;
    }
}
