#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearMenuEditableKakuro
{
    private static Color Azul = new Color32(38, 78, 127, 255);
    private static Color TextoOscuro = new Color32(35, 46, 61, 255);
    private static Color Secundario = new Color32(71, 111, 154, 255);
    private static Color Gris = new Color32(88, 105, 124, 255);

    [MenuItem("Kakuro/Crear prefab editable de MainMenu")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear el menú.", "Aceptar"); return; }
        MainMenuManager manager = Object.FindObjectOfType<MainMenuManager>();
        if (manager == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena MainMenu antes de crear el prefab.", "Aceptar"); return; }
        var datos = new SerializedObject(manager);
        SerializedProperty plantilla = datos.FindProperty("plantillaMenu");
        if (plantilla == null)
        { EditorUtility.DisplayDialog("Kakuro", "Falta el MainMenuManager actualizado. Revisa la consola.", "Aceptar"); return; }
        if (plantilla.objectReferenceValue != null && !EditorUtility.DisplayDialog("Kakuro", "Ya hay un menú editable asignado. Se creará otro prefab y se sustituirá la referencia en esta escena. El anterior no se borrará.", "Crear copia", "Cancelar")) return;
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/MenuPrincipal")) AssetDatabase.CreateFolder("Assets/KakuroUI", "MenuPrincipal");
        GameObject raiz = null;
        try
        {
            raiz = Construir(datos);
            string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/MenuPrincipal/MenuPrincipalEditable.prefab");
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            if (guardado == null) throw new System.InvalidOperationException("No se pudo guardar el prefab.");
            KakuroMenuEditable ui = guardado.GetComponent<KakuroMenuEditable>();
            if (ui == null || !ui.ReferenciasValidas()) throw new System.InvalidOperationException("El prefab tiene referencias incompletas.");
            plantilla.objectReferenceValue = ui;
            datos.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(guardado);
            EditorUtility.DisplayDialog("Kakuro", "Prefab creado y asignado: " + ruta + ".\n\nGuarda la escena. Abre el prefab con doble clic para editar sus pantallas.", "Aceptar");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la creación. Revisa la consola y las referencias de la escena.", "Aceptar");
        }
        finally { if (raiz != null) Object.DestroyImmediate(raiz); }
    }

    private static GameObject Construir(SerializedObject datos)
    {
        var root = new GameObject("MenuPrincipalEditable", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1080,1920);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight = .5f;
        var ui = root.AddComponent<KakuroMenuEditable>();
        TMP_FontAsset fuente = TMP_Settings.defaultFontAsset;
        RectTransform fondo = Rect("Fondo", root.transform, Vector2.zero, Vector2.one);
        fondo.gameObject.AddComponent<Image>().color = new Color32(239,241,245,255);
        ui.zonaSegura = Rect("Zona segura", fondo, Vector2.zero, Vector2.one);
        RectTransform home = Rect("Inicio", ui.zonaSegura, Vector2.zero, Vector2.one); ui.inicio = home.gameObject;
        Sprite logo = datos.FindProperty("imagenMenu").objectReferenceValue as Sprite;
        RectTransform logoRect = Rect("Imagen menu", home, new Vector2(.2f,.72f), new Vector2(.8f,.90f));
        Image logoImg = logoRect.gameObject.AddComponent<Image>(); logoImg.sprite = logo; logoImg.preserveAspect = true; logoImg.raycastTarget = false;
        logoRect.gameObject.SetActive(logo != null);
        TMP_Text titulo = Texto("Titulo", home, "Kakuro", new Vector2(.06f,.62f), new Vector2(.94f,.72f), 86, Azul, fuente, true, true);
        titulo.gameObject.SetActive(datos.FindProperty("mostrarTituloMenu").boolValue);
        Texto("Subtitulo", home, "Un reto, una suma a la vez", new Vector2(.08f,.56f), new Vector2(.92f,.62f), 38, Gris, fuente, false, true);
        RectTransform acciones = Rect("Botones inicio", home, new Vector2(.14f,.14f), new Vector2(.86f,.53f));
        var l = acciones.gameObject.AddComponent<VerticalLayoutGroup>(); l.spacing = 14; l.childForceExpandWidth = true; l.childForceExpandHeight = true;
        ui.jugar = Boton("Jugar", acciones, "Jugar", Azul, fuente, 48);
        ui.abrirReglas = Boton("Como jugar", acciones, "Cómo jugar", Secundario, fuente, 48);
        ui.abrirRanking = Boton("Mejores tiempos", acciones, "Mejores tiempos", Secundario, fuente, 48);
        ui.salir = Boton("Salir", acciones, "Salir", new Color32(109,121,137,255), fuente, 48);

        RectTransform reglas = Rect("Reglas", ui.zonaSegura, Vector2.zero, Vector2.one); ui.reglas = reglas.gameObject;
        Texto("Titulo reglas", reglas, "Cómo jugar", new Vector2(.05f,.90f), new Vector2(.95f,.99f), 74, Azul, fuente, true, true);
        Texto("Subtitulo reglas", reglas, "Reglas y consejos", new Vector2(.05f,.85f), new Vector2(.95f,.90f), 34, Gris, fuente, false, true);
        RectTransform contenidoReglas;
        ui.scrollReglas = Scroll("Texto de reglas", reglas, new Vector2(.05f,.13f), new Vector2(.95f,.84f), out contenidoReglas);
        var lista = contenidoReglas.gameObject.AddComponent<VerticalLayoutGroup>();
        lista.padding = new RectOffset(12,12,18,18); lista.spacing = 14;
        lista.childControlWidth = true; lista.childControlHeight = true; lista.childForceExpandWidth = true; lista.childForceExpandHeight = false;
        Color colorTituloRegla = datos.FindProperty("colorTituloRegla").colorValue;
        Color colorTextoRegla = datos.FindProperty("colorTextoRegla").colorValue;
        float tamTituloRegla = datos.FindProperty("tamanoTituloRegla").intValue * 2.2f;
        float tamTextoRegla = datos.FindProperty("tamanoTextoRegla").intValue * 2.2f;
        SerializedProperty secciones = datos.FindProperty("secciones");
        for (int i = 0; i < secciones.arraySize; i++)
        {
            SerializedProperty sec = secciones.GetArrayElementAtIndex(i);
            string nombre = sec.FindPropertyRelative("titulo").stringValue;
            string cuerpo = sec.FindPropertyRelative("texto").stringValue;
            TextoFlujo("Regla " + (i+1) + " titulo", contenidoReglas, nombre, tamTituloRegla, colorTituloRegla, fuente, true);
            TextoFlujo("Regla " + (i+1) + " texto", contenidoReglas, cuerpo, tamTextoRegla, colorTextoRegla, fuente, false);
        }
        RectTransform zonaVolverReglas = Rect("Zona volver", reglas, new Vector2(.07f,.025f), new Vector2(.93f,.115f));
        ui.volverReglas = Boton("Volver reglas", zonaVolverReglas, "Volver al menú", Azul, fuente, 45);

        RectTransform ranking = Rect("Ranking", ui.zonaSegura, Vector2.zero, Vector2.one); ui.ranking = ranking.gameObject;
        Texto("Titulo ranking", ranking, datos.FindProperty("tituloRanking").stringValue, new Vector2(.05f,.90f), new Vector2(.95f,.99f), 60, Azul, fuente, true, true);
        Texto("Subtitulo ranking", ranking, datos.FindProperty("subtituloRanking").stringValue, new Vector2(.05f,.85f), new Vector2(.95f,.90f), 31, Gris, fuente, false, true);
        RectTransform pestanas = Rect("Dificultades", ranking, new Vector2(.05f,.745f), new Vector2(.95f,.835f));
        var tabs = pestanas.gameObject.AddComponent<HorizontalLayoutGroup>(); tabs.spacing = 10; tabs.childForceExpandWidth = true; tabs.childForceExpandHeight = true;
        ui.facil = Boton("Facil", pestanas, "Fácil", ui.pestanaActiva, fuente, 40);
        ui.medio = Boton("Medio", pestanas, "Medio", ui.pestanaInactiva, fuente, 40);
        ui.dificil = Boton("Dificil", pestanas, "Difícil", ui.pestanaInactiva, fuente, 40);
        RectTransform contenidoRanking;
        ui.scrollRanking = Scroll("Lista ranking", ranking, new Vector2(.05f,.13f), new Vector2(.95f,.73f), out contenidoRanking);
        var filas = contenidoRanking.gameObject.AddComponent<VerticalLayoutGroup>(); filas.padding = new RectOffset(18,18,18,18);
        filas.spacing = Mathf.Max(0, datos.FindProperty("separacionFilasRanking").intValue * 2);
        filas.childControlWidth = true; filas.childControlHeight = true; filas.childForceExpandWidth = true; filas.childForceExpandHeight = false;
        ui.mensajeRanking = TextoFlujo("Mensaje ranking", contenidoRanking, "Aún no hay tiempos guardados en esta dificultad.", 36, Gris, fuente, false);
        ui.filasRanking = new GameObject[8]; ui.posicionesRanking = new TMP_Text[8]; ui.tiemposRanking = new TMP_Text[8];
        for (int i = 0; i < 8; i++)
        {
            RectTransform fila = Rect("Fila " + (i+1), contenidoRanking, Vector2.zero, Vector2.one);
            fila.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            ui.filasRanking[i] = fila.gameObject;
            ui.posicionesRanking[i] = Texto("Posicion", fila, (i+1) + ".", new Vector2(.02f,.05f), new Vector2(.25f,.95f), 42, Gris, fuente, true, true);
            ui.tiemposRanking[i] = Texto("Tiempo", fila, "01:23.456", new Vector2(.27f,.05f), new Vector2(.98f,.95f), 46, Azul, fuente, true, true);
        }
        RectTransform zonaVolverRanking = Rect("Zona volver", ranking, new Vector2(.07f,.025f), new Vector2(.93f,.115f));
        ui.volverRanking = Boton("Volver ranking", zonaVolverRanking, "Volver al menú", Azul, fuente, 45);
        reglas.gameObject.SetActive(false); ranking.gameObject.SetActive(false);
        return root;
    }

    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static TMP_Text Texto(string nombre, Transform padre, string texto, Vector2 min, Vector2 max, float tam, Color color, TMP_FontAsset fuente, bool negrita, bool auto)
    {
        RectTransform r = Rect(nombre, padre, min, max); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = auto; t.fontSize = tam; t.fontSizeMin = 8; t.fontSizeMax = tam;
        t.raycastTarget = false; t.enableWordWrapping = !auto;
        if (negrita) t.fontStyle = FontStyles.Bold;
        return t;
    }
    private static TMP_Text TextoFlujo(string nombre, Transform padre, string texto, float tam, Color color, TMP_FontAsset fuente, bool negrita)
    {
        var t = Texto(nombre, padre, texto, Vector2.zero, Vector2.one, tam, color, fuente, negrita, false);
        t.alignment = TextAlignmentOptions.TopLeft; t.richText = false; return t;
    }
    private static Button Boton(string nombre, Transform padre, string texto, Color color, TMP_FontAsset fuente, float tam)
    {
        RectTransform r = Rect("Boton " + nombre, padre, Vector2.zero, Vector2.one);
        var image = r.gameObject.AddComponent<Image>(); image.color = color;
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        Texto("Etiqueta", r, texto, Vector2.zero, Vector2.one, tam, Color.white, fuente, true, true);
        return b;
    }
    private static ScrollRect Scroll(string nombre, Transform padre, Vector2 min, Vector2 max, out RectTransform contenido)
    {
        RectTransform viewport = Rect(nombre, padre, min, max);
        Image bg = viewport.gameObject.AddComponent<Image>(); bg.color = Color.white; bg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
        contenido = Rect("Contenido", viewport, new Vector2(0,1), new Vector2(1,1));
        contenido.pivot = new Vector2(.5f,1); contenido.offsetMin = new Vector2(10,0); contenido.offsetMax = new Vector2(-10,0);
        contenido.anchoredPosition = Vector2.zero;
        var fit = contenido.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contenido; return scroll;
    }
}
#endif
