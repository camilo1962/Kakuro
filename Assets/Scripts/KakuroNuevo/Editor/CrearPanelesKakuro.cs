#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearPanelesKakuro
{
    private enum Tipo { Pausa, Reset, ConfirmarRendicion, ResultadoRendicion, Victoria, Combinaciones }

    [MenuItem("Kakuro/Crear prefabs editables de paneles")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear los paneles.", "Aceptar"); return; }
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro antes de crear los paneles.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        string[] propiedades = { "panelPausaPrefab", "panelResetPrefab", "panelConfirmarRendicionPrefab", "panelResultadoRendicionPrefab", "panelVictoriaPrefab", "panelCombinacionesPrefab" };
        foreach (string propiedad in propiedades)
        {
            SerializedProperty p = datos.FindProperty(propiedad);
            if (p == null)
            { EditorUtility.DisplayDialog("Kakuro", "Falta el script actualizado DemoKakuroMovil. Comprueba los errores de consola.", "Aceptar"); return; }
            if (p.objectReferenceValue != null)
            {
                if (!EditorUtility.DisplayDialog("Kakuro", "Ya hay paneles asignados. Se crearán copias nuevas y se cambiarán las seis referencias en la escena. Los prefabs existentes no se borrarán.", "Crear copias", "Cancelar")) return;
                break;
            }
        }
        Color acento = datos.FindProperty("acento").colorValue;
        Color fondoConsulta = datos.FindProperty("fondoConsulta").colorValue;
        Color textoConsulta = datos.FindProperty("colorTextoConsulta").colorValue;
        float mx = Mathf.Clamp(datos.FindProperty("margenHorizontalConsulta").floatValue, .02f, .2f);
        float my = Mathf.Clamp(datos.FindProperty("margenVerticalConsulta").floatValue, .04f, .3f);
        int tamTitulo = Mathf.Clamp(datos.FindProperty("tamanoTituloConsulta").intValue, 16, 60);
        int tamTexto = Mathf.Clamp(datos.FindProperty("tamanoTextoConsulta").intValue, 12, 48);
        TMP_FontAsset fuente = datos.FindProperty("fuenteInterfaz").objectReferenceValue as TMP_FontAsset;
        if (fuente == null) fuente = TMP_Settings.defaultFontAsset;
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/Paneles")) AssetDatabase.CreateFolder("Assets/KakuroUI", "Paneles");
        string[] nombres = { "PanelPausa", "PanelReset", "PanelConfirmarRendicion", "PanelResultadoRendicion", "PanelVictoria", "PanelCombinaciones" };
        var prefabs = new KakuroPanelEditable[6];
        try
        {
            for (int i = 0; i < nombres.Length; i++)
            {
                GameObject raiz = Construir((Tipo)i, nombres[i], acento, fuente, fondoConsulta, textoConsulta, mx, my, tamTitulo, tamTexto);
                try
                {
                    string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/Paneles/" + nombres[i] + ".prefab");
                    GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    if (guardado == null) throw new System.InvalidOperationException("No se pudo guardar " + ruta);
                    prefabs[i] = guardado.GetComponent<KakuroPanelEditable>();
                    if (prefabs[i] == null || !prefabs[i].ReferenciasValidas(i == 5))
                        throw new System.InvalidOperationException("Referencias incompletas en " + ruta);
                }
                finally { Object.DestroyImmediate(raiz); }
            }
            for (int i = 0; i < nombres.Length; i++) datos.FindProperty(propiedades[i]).objectReferenceValue = prefabs[i];
            datos.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(prefabs[0].gameObject);
            EditorUtility.DisplayDialog("Kakuro", "Creados y asignados seis prefabs en Assets/KakuroUI/Paneles.\n\nGuarda la escena. Haz doble clic en un prefab para editarlo.", "Aceptar");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la creación. Revisa la consola. Puede que se hayan creado algunos archivos; revisa también las referencias de la escena.", "Aceptar");
        }
    }

    private static GameObject Construir(Tipo tipo, string nombre, Color acento, TMP_FontAsset fuente,
        Color fondoConsulta, Color colorConsulta, float mx, float my, int tamTitulo, int tamTexto)
    {
        var root = new GameObject(nombre, typeof(RectTransform));
        RectTransform velo = root.GetComponent<RectTransform>(); velo.sizeDelta = new Vector2(1080, 1920);
        Image oscurecer = root.AddComponent<Image>(); oscurecer.color = new Color(0,0,0,.65f); oscurecer.raycastTarget = true;
        var ui = root.AddComponent<KakuroPanelEditable>();
        if (tipo == Tipo.Combinaciones)
        {
            RectTransform panel = Rect("Panel", velo, new Vector2(mx,my), new Vector2(1-mx,1-my));
            panel.gameObject.AddComponent<Image>().color = fondoConsulta;
            ui.titulo = Texto("Titulo", panel, "Combinaciones", new Vector2(.05f,.85f), new Vector2(.95f,.98f), tamTitulo, acento, fuente, true);
            RectTransform direcciones = Rect("Direcciones", panel, new Vector2(.05f,.71f), new Vector2(.95f,.84f));
            var layout = direcciones.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            ui.botonFila = Boton("Fila", direcciones, "Fila → 10", acento, fuente);
            ui.botonColumna = Boton("Columna", direcciones, "Columna ↓ 9", acento, fuente);
            ui.textoFila = ui.botonFila.GetComponentInChildren<TMP_Text>();
            ui.textoColumna = ui.botonColumna.GetComponentInChildren<TMP_Text>();
            RectTransform viewport = Rect("Area desplazable", panel, new Vector2(.05f,.17f), new Vector2(.95f,.69f));
            Image area = viewport.gameObject.AddComponent<Image>(); area.color = new Color(1,1,1,.01f); area.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            RectTransform contenido = Rect("Contenido", viewport, new Vector2(0,1), new Vector2(1,1));
            contenido.pivot = new Vector2(.5f,1); contenido.offsetMin = new Vector2(12,0);
            contenido.offsetMax = new Vector2(-12,0); contenido.anchoredPosition = Vector2.zero;
            var texto = contenido.gameObject.AddComponent<TextMeshProUGUI>();
            if (fuente != null) texto.font = fuente;
            texto.text = "Fila · 2 casillas · suma 10\n\nSiempre: Ninguna\nNunca: 5\n\nCombinaciones:\n1 + 9\n2 + 8\n3 + 7\n4 + 6";
            texto.color = colorConsulta; texto.fontSize = tamTexto; texto.enableAutoSizing = false;
            texto.enableWordWrapping = true; texto.alignment = TextAlignmentOptions.TopLeft;
            texto.raycastTarget = false; texto.richText = false;
            var ajuste = contenido.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contenido; ui.detalle = texto;
            RectTransform zonaCerrar = Rect("Zona cerrar", panel, new Vector2(.20f,.025f), new Vector2(.80f,.14f));
            ui.botonPrincipal = Boton("Cerrar", zonaCerrar, "Cerrar", acento, fuente);
            return root;
        }
        float minY = tipo == Tipo.ConfirmarRendicion ? .30f : (tipo == Tipo.Pausa || tipo == Tipo.Reset ? .32f : .31f);
        float maxY = 1f - minY;
        RectTransform caja = Rect("Panel", velo, new Vector2(.08f,minY), new Vector2(.92f,maxY));
        caja.gameObject.AddComponent<Image>().color = Color.white;
        string titulo, detalle, principal, secundario;
        float minTitulo, maxTitulo, minDetalle, maxDetalle, minBotones, maxBotones, tamanoTitulo, tamanoDetalle;
        switch (tipo)
        {
            case Tipo.Pausa:
                titulo="Partida en pausa"; detalle="El cronómetro está detenido."; principal="Continuar"; secundario="Salir";
                minTitulo=.70f; maxTitulo=.93f; minDetalle=.48f; maxDetalle=.69f; minBotones=.12f; maxBotones=.38f; tamanoTitulo=54; tamanoDetalle=32; break;
            case Tipo.Reset:
                titulo="¿Borrar tus números?"; detalle="Se conservan las pistas y el tiempo."; principal="Cancelar"; secundario="Borrar números";
                minTitulo=.70f; maxTitulo=.94f; minDetalle=.46f; maxDetalle=.67f; minBotones=.10f; maxBotones=.38f; tamanoTitulo=44; tamanoDetalle=30; break;
            case Tipo.ConfirmarRendicion:
                titulo="¿Quieres ver la solución?"; detalle="Se contará como no resuelto.\nNo entrará en el ranking."; principal="Cancelar"; secundario="Me rindo";
                minTitulo=.75f; maxTitulo=.95f; minDetalle=.45f; maxDetalle=.72f; minBotones=.09f; maxBotones=.36f; tamanoTitulo=44; tamanoDetalle=32; break;
            case Tipo.ResultadoRendicion:
                titulo="Partida no resuelta"; detalle="Fácil · Nivel 1\nNo se añadirá al ranking.\nNo resueltos en este modo: 1"; principal="Nuevo puzzle"; secundario="Ver solución";
                minTitulo=.72f; maxTitulo=.94f; minDetalle=.39f; maxDetalle=.70f; minBotones=.10f; maxBotones=.32f; tamanoTitulo=48; tamanoDetalle=34; break;
            default:
                titulo="¡Puzzle completado!"; detalle="¡Enhorabuena!\nFácil · 7 × 7 · Nivel 1\nTiempo: 01:00\nPistas utilizadas: 0 / 5\nVictorias en este modo: 1"; principal="Nuevo puzzle"; secundario="Ver tablero";
                minTitulo=.70f; maxTitulo=.93f; minDetalle=.37f; maxDetalle=.70f; minBotones=.10f; maxBotones=.32f; tamanoTitulo=62; tamanoDetalle=38; break;
        }
        ui.titulo = Texto("Titulo", caja, titulo, new Vector2(.05f,minTitulo), new Vector2(.95f,maxTitulo), tamanoTitulo, acento, fuente, true);
        ui.detalle = Texto("Detalle", caja, detalle, new Vector2(.05f,minDetalle), new Vector2(.95f,maxDetalle), tamanoDetalle, Color.black, fuente, false);
        RectTransform acciones = Rect("Botones", caja, new Vector2(.06f,minBotones), new Vector2(.94f,maxBotones));
        var botones = acciones.gameObject.AddComponent<HorizontalLayoutGroup>();
        botones.spacing = tipo == Tipo.Victoria ? 12 : 10; botones.childForceExpandWidth = true; botones.childForceExpandHeight = true;
        ui.botonPrincipal = Boton("Principal", acciones, principal, acento, fuente);
        ui.botonSecundario = Boton("Secundario", acciones, secundario, acento, fuente);
        return root;
    }

    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static TMP_Text Texto(string nombre, Transform padre, string texto, Vector2 min, Vector2 max, float tam, Color color, TMP_FontAsset fuente, bool negrita)
    {
        var r = Rect(nombre, padre, min, max); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = tam;
        t.raycastTarget = false; t.enableWordWrapping = false;
        if (negrita) t.fontStyle = FontStyles.Bold;
        return t;
    }
    private static Button Boton(string nombre, Transform padre, string etiqueta, Color color, TMP_FontAsset fuente)
    {
        var r = Rect("Boton " + nombre, padre, Vector2.zero, Vector2.one);
        Image img = r.gameObject.AddComponent<Image>(); img.color = color;
        Button b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        Texto("Etiqueta", r, etiqueta, Vector2.zero, Vector2.one, 34, Color.white, fuente, true);
        return b;
    }
}
#endif
