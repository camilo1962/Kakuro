using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuManager : MonoBehaviour
{
    [Serializable]
    private sealed class SeccionRegla
    {
        public string titulo;
        [TextArea(2, 8)] public string texto;
        public SeccionRegla(string titulo, string texto) { this.titulo = titulo; this.texto = texto; }
    }

    [Header("Menú editable (opcional)")]
    [Tooltip("Prefab creado con Kakuro > Crear prefab editable de MainMenu. Sin asignar se conserva UI Toolkit.")]
    [SerializeField] private KakuroMenuEditable plantillaMenu;
    private KakuroMenuEditable instanciaMenu;
    private GameObject eventSystemCreado;

    [Header("Imagen opcional en el menú")]
    [SerializeField] private Sprite imagenMenu;
    [SerializeField, Min(1)] private float anchoImagen = 160f;
    [SerializeField, Min(1)] private float altoImagen = 70f;
    [SerializeField] private bool mostrarTituloMenu = true;

    [Header("Contenido de las reglas")]
    [SerializeField] private List<SeccionRegla> secciones = CrearReglasPredeterminadas();
    [Header("Aspecto de las reglas")]
    [SerializeField] private bool mostrarBarraReglas = false;
    [SerializeField, Range(10, 40)] private int tamanoTituloRegla = 19;
    [SerializeField, Range(10, 40)] private int tamanoTextoRegla = 16;
    [SerializeField] private Color colorTituloRegla = new Color32(38, 78, 127, 255);
    [SerializeField] private Color colorTextoRegla = new Color32(35, 46, 61, 255);

    [Header("Textos y aspecto del ranking")]
    [SerializeField] private string tituloRanking = "Mejores tiempos";
    [SerializeField] private string subtituloRanking = "Top 8 por dificultad";
    [SerializeField] private bool mostrarBarraRanking = false;
    [SerializeField, Range(12, 40)] private int tamanoTituloRanking = 24;
    [SerializeField, Range(10, 28)] private int tamanoSubtituloRanking = 13;
    [SerializeField, Range(10, 28)] private int tamanoPestanasRanking = 15;
    [SerializeField, Range(10, 28)] private int tamanoPosicionesRanking = 16;
    [SerializeField, Range(10, 28)] private int tamanoTiemposRanking = 16;
    [SerializeField, Range(0, 12)] private int separacionFilasRanking = 3;
    [SerializeField, Range(10, 28)] private int tamanoVolverRanking = 17;

    private VisualElement home, reglas, ranking;
    private ScrollView contenidoReglas, contenidoRanking;
    private Button jugar, salir, abrirReglas, abrirRanking, volverReglas, volverRanking;
    private Button facil, medio, dificil;

    private void OnEnable()
    {
        var documento = GetComponent<UIDocument>();
        VisualElement root = documento.rootVisualElement;
        if (plantillaMenu != null)
        {
            KakuroMenuEditable instancia = Instantiate(plantillaMenu);
            if (instancia.ReferenciasValidas())
            {
                instanciaMenu = instancia;
                if (FindObjectOfType<EventSystem>() == null)
                    eventSystemCreado = new GameObject("EventSystem MainMenu", typeof(EventSystem), typeof(StandaloneInputModule));
                instancia.Inicializar();
                root.style.display = DisplayStyle.None;
                return;
            }
            Debug.LogWarning("Kakuro: faltan referencias en el prefab del menú. Se utilizará UI Toolkit.");
            instancia.gameObject.SetActive(false); Destroy(instancia.gameObject);
        }
        root.style.display = DisplayStyle.Flex;
        home = root.Q<VisualElement>("HomePanel");
        reglas = root.Q<VisualElement>("RulesPanel");
        ranking = root.Q<VisualElement>("RankingPanel");
        contenidoReglas = root.Q<ScrollView>("RulesContent");
        contenidoRanking = root.Q<ScrollView>("RankingContent");
        jugar = root.Q<Button>("PlayButton"); salir = root.Q<Button>("QuitButton");
        abrirReglas = root.Q<Button>("RulesButton"); abrirRanking = root.Q<Button>("RankingButton");
        volverReglas = root.Q<Button>("RulesBackButton"); volverRanking = root.Q<Button>("RankingBackButton");
        facil = root.Q<Button>("RankEasyButton"); medio = root.Q<Button>("RankMediumButton"); dificil = root.Q<Button>("RankHardButton");
        if (home == null || reglas == null || ranking == null || contenidoReglas == null || contenidoRanking == null ||
            jugar == null || salir == null || abrirReglas == null || abrirRanking == null ||
            volverReglas == null || volverRanking == null || facil == null || medio == null || dificil == null)
        {
            Debug.LogError("MainMenuManager: falta algún elemento. Comprueba que el UIDocument use el nuevo MainMenu.uxml.");
            return;
        }
        jugar.clicked += Jugar; salir.clicked += Salir;
        abrirReglas.clicked += MostrarReglas; abrirRanking.clicked += MostrarRanking;
        volverReglas.clicked += MostrarInicio; volverRanking.clicked += MostrarInicio;
        facil.clicked += RankingFacil; medio.clicked += RankingMedio; dificil.clicked += RankingDificil;
        ConfigurarImagenMenu(root);
        ConfigurarRanking(root);
        contenidoReglas.verticalScrollerVisibility = mostrarBarraReglas ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        contenidoReglas.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        PrepararReglas();
        MostrarInicio();
    }

    private void ConfigurarImagenMenu(VisualElement root)
    {
        var imagen = root.Q<UnityEngine.UIElements.Image>("MenuLogo");
        if (imagen != null)
        {
            imagen.sprite = imagenMenu;
            imagen.scaleMode = ScaleMode.ScaleToFit;
            imagen.style.width = Mathf.Max(1f, anchoImagen);
            imagen.style.height = Mathf.Max(1f, altoImagen);
            imagen.style.display = imagenMenu == null ? DisplayStyle.None : DisplayStyle.Flex;
        }
        var titulo = root.Q<Label>("MenuTitle");
        if (titulo != null) titulo.style.display = mostrarTituloMenu ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ConfigurarRanking(VisualElement root)
    {
        var titulo = root.Q<Label>("RankingTitle");
        if (titulo != null) { titulo.text = tituloRanking; titulo.style.fontSize = tamanoTituloRanking; }
        var subtitulo = root.Q<Label>("RankingSubtitle");
        if (subtitulo != null) { subtitulo.text = subtituloRanking; subtitulo.style.fontSize = tamanoSubtituloRanking; }
        facil.style.fontSize = tamanoPestanasRanking;
        medio.style.fontSize = tamanoPestanasRanking;
        dificil.style.fontSize = tamanoPestanasRanking;
        volverRanking.style.fontSize = tamanoVolverRanking;
        contenidoRanking.verticalScrollerVisibility = mostrarBarraRanking ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        contenidoRanking.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
    }

    private void OnDisable()
    {
        if (instanciaMenu != null)
        {
            instanciaMenu.gameObject.SetActive(false); Destroy(instanciaMenu.gameObject); instanciaMenu = null;
            var documento = GetComponent<UIDocument>();
            if (documento != null && documento.rootVisualElement != null)
                documento.rootVisualElement.style.display = DisplayStyle.Flex;
        }
        if (eventSystemCreado != null)
        { eventSystemCreado.SetActive(false); Destroy(eventSystemCreado); eventSystemCreado = null; }
        if (jugar != null) jugar.clicked -= Jugar;
        if (salir != null) salir.clicked -= Salir;
        if (abrirReglas != null) abrirReglas.clicked -= MostrarReglas;
        if (abrirRanking != null) abrirRanking.clicked -= MostrarRanking;
        if (volverReglas != null) volverReglas.clicked -= MostrarInicio;
        if (volverRanking != null) volverRanking.clicked -= MostrarInicio;
        if (facil != null) facil.clicked -= RankingFacil;
        if (medio != null) medio.clicked -= RankingMedio;
        if (dificil != null) dificil.clicked -= RankingDificil;
    }

    private void Jugar() { SceneManager.LoadScene("DemoKakuro"); }
    private void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void MostrarInicio()
    {
        home.style.display = DisplayStyle.Flex;
        reglas.style.display = DisplayStyle.None;
        ranking.style.display = DisplayStyle.None;
    }
    private void MostrarReglas()
    {
        home.style.display = DisplayStyle.None;
        reglas.style.display = DisplayStyle.Flex;
        ranking.style.display = DisplayStyle.None;
        contenidoReglas.scrollOffset = Vector2.zero;
    }
    private void MostrarRanking()
    {
        home.style.display = DisplayStyle.None;
        reglas.style.display = DisplayStyle.None;
        ranking.style.display = DisplayStyle.Flex;
        VerRanking(7);
    }
    private void RankingFacil() { VerRanking(7); }
    private void RankingMedio() { VerRanking(9); }
    private void RankingDificil() { VerRanking(12); }

    private void VerRanking(int tamano)
    {
        facil.EnableInClassList("tab-active", tamano == 7);
        medio.EnableInClassList("tab-active", tamano == 9);
        dificil.EnableInClassList("tab-active", tamano == 12);
        contenidoRanking.Clear();
        List<long> tiempos; string error;
        if (!KakuroRanking.Obtener(tamano, out tiempos, out error))
        {
            MensajeRanking("No se pudo leer el ranking. Revisa la consola.");
            Debug.LogError("Kakuro: no se pudo leer el ranking. " + error);
            return;
        }
        if (tiempos.Count == 0)
        {
            MensajeRanking("Aún no hay tiempos guardados en esta dificultad. ¡Completa un puzzle para empezar!");
            return;
        }
        for (int i = 0; i < tiempos.Count; i++)
        {
            var fila = new VisualElement(); fila.AddToClassList("rank-row");
            fila.style.paddingTop = separacionFilasRanking;
            fila.style.paddingBottom = separacionFilasRanking;
            var posicion = new Label((i + 1).ToString() + "."); posicion.AddToClassList("rank-position");
            posicion.style.fontSize = tamanoPosicionesRanking;
            var tiempo = new Label(KakuroRanking.FormatearTiempo(tiempos[i])); tiempo.AddToClassList("rank-time");
            tiempo.style.fontSize = tamanoTiemposRanking;
            fila.Add(posicion); fila.Add(tiempo); contenidoRanking.Add(fila);
        }
        contenidoRanking.scrollOffset = Vector2.zero;
    }
    private void MensajeRanking(string texto)
    {
        var etiqueta = new Label(texto); etiqueta.AddToClassList("empty-message");
        contenidoRanking.Add(etiqueta);
    }
    private void PrepararReglas()
    {
        contenidoReglas.Clear();
        if (secciones == null) return;
        foreach (SeccionRegla seccion in secciones)
        {
            if (seccion == null) continue;
            var encabezado = new Label(seccion.titulo ?? "");
            encabezado.AddToClassList("rule-heading");
            encabezado.style.fontSize = tamanoTituloRegla;
            encabezado.style.color = colorTituloRegla;
            contenidoReglas.Add(encabezado);
            var parrafo = new Label(seccion.texto ?? "");
            parrafo.AddToClassList("rule-body");
            parrafo.style.fontSize = tamanoTextoRegla;
            parrafo.style.color = colorTextoRegla;
            contenidoReglas.Add(parrafo);
        }
    }

    // Útil si la nueva lista aparece vacía en un componente ya existente.
    [ContextMenu("Restaurar reglas predeterminadas")]
    private void RestaurarReglasPredeterminadas()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Restaurar reglas de Kakuro");
#endif
        secciones = CrearReglasPredeterminadas();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        if (Application.isPlaying && contenidoReglas != null) PrepararReglas();
    }

    private static List<SeccionRegla> CrearReglasPredeterminadas()
    {
        return new List<SeccionRegla>
        {
            new SeccionRegla("Objetivo", "Rellena todas las casillas blancas con cifras del 1 al 9. Cada grupo debe sumar el valor de su pista."),
            new SeccionRegla("Sin repetir", "Dentro de un mismo grupo horizontal o vertical no se puede repetir ninguna cifra. Una misma cifra sí puede aparecer en otros grupos."),
            new SeccionRegla("Cómo leer las pistas", "El número de la parte superior derecha indica la suma hacia la derecha. El de la parte inferior izquierda indica la suma hacia abajo. Una casilla puede contener ambas pistas."),
            new SeccionRegla("Combinaciones útiles", "Una suma de 3 en dos casillas solo puede usar 1 y 2; una suma de 4, 1 y 3; y una suma de 17, 8 y 9. En tres casillas, 6 usa 1, 2 y 3, mientras que 24 usa 7, 8 y 9. Aún tendrás que deducir el orden."),
            new SeccionRegla("Cruces", "Cada casilla pertenece a un grupo horizontal y a otro vertical. Busca cifras que puedan satisfacer ambas sumas y descarta las que entren en conflicto."),
            new SeccionRegla("Límites", "Calcula la menor o mayor suma posible sin repetir cifras. Por ejemplo, una suma de 26 en cuatro casillas no puede incluir el 1: incluso 1 + 7 + 8 + 9 solo suma 25."),
            new SeccionRegla("Controles", "Toca una casilla y elige un número; X lo borra. Usa Comprobar para revisar los grupos y Pistas para revelar una casilla. Pausa detiene el tiempo."),
            new SeccionRegla("Mejores tiempos", "Se guardan los ocho tiempos más rápidos de cada dificultad en este dispositivo. Las pistas no modifican la clasificación.")
        };
    }
}
