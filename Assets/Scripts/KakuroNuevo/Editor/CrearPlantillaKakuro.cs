#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Genera un prefab real que se puede editar fuera de Play desde el modo Prefab.
public static class CrearPlantillaKakuro
{
    [MenuItem("Kakuro/Crear plantilla editable de juego")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear la plantilla.", "Aceptar");
            return;
        }
        Color acento = new Color32(38, 78, 127, 255);
        TMP_FontAsset fuente = TMP_Settings.defaultFontAsset, fuenteTiempo = fuente;
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo != null)
        {
            var serializado = new SerializedObject(demo);
            SerializedProperty p = serializado.FindProperty("acento");
            if (p != null) acento = p.colorValue;
            p = serializado.FindProperty("fuenteInterfaz");
            if (p != null && p.objectReferenceValue != null) fuente = p.objectReferenceValue as TMP_FontAsset;
            p = serializado.FindProperty("fuenteTiempo");
            if (p != null && p.objectReferenceValue != null) fuenteTiempo = p.objectReferenceValue as TMP_FontAsset;
            else fuenteTiempo = fuente;
        }
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/InterfazJuego.prefab");
        GameObject raiz = new GameObject("InterfazJuego", typeof(RectTransform));
        try
        {
            RectTransform raizRect = raiz.GetComponent<RectTransform>();
            raizRect.sizeDelta = new Vector2(1080, 1920); // Tamaño de referencia para editar el prefab.
            var plantilla = raiz.AddComponent<KakuroPlantillaInterfaz>();
            Texto("Titulo", raizRect, "KAKURO", new Vector2(.28f,.925f), new Vector2(.72f,.985f), 55, acento, fuente, true);
            RectTransform salir = Rect("Zona salir", raizRect, new Vector2(.035f,.925f), new Vector2(.25f,.985f));
            plantilla.salir = Boton("Salir", salir, acento, fuente);
            RectTransform pausa = Rect("Zona pausa", raizRect, new Vector2(.75f,.925f), new Vector2(.965f,.985f));
            plantilla.pausa = Boton("Pausa", pausa, acento, fuente);
            plantilla.estado = Texto("Estado", raizRect, "Preparando…", new Vector2(.04f,.865f), new Vector2(.96f,.92f), 30, Color.black, fuente, false);
            plantilla.tiempo = Texto("Tiempo", raizRect, "Tiempo: 00:00", new Vector2(.04f,.815f), new Vector2(.96f,.86f), 27, acento, fuenteTiempo, true);
            plantilla.zonaAcciones = Rect("Acciones", raizRect, new Vector2(.035f,.035f), new Vector2(.965f,.17f));
            var grid = plantilla.zonaAcciones.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            grid.spacing = new Vector2(8, 8); grid.childAlignment = TextAnchor.MiddleCenter;
            grid.cellSize = new Vector2(330, 125); // En ejecución se adapta al espacio disponible.
            plantilla.distribucionAcciones = grid;
            plantilla.menu = Boton("Menú", plantilla.zonaAcciones, acento, fuente);
            plantilla.nuevo = Boton("Nuevo", plantilla.zonaAcciones, acento, fuente);
            plantilla.reset = Boton("Reset", plantilla.zonaAcciones, acento, fuente);
            plantilla.pistas = Boton("Pistas (0)", plantilla.zonaAcciones, acento, fuente);
            plantilla.textoPistas = plantilla.pistas.GetComponentInChildren<TMP_Text>();
            plantilla.comprobar = Boton("Comprobar", plantilla.zonaAcciones, acento, fuente);
            plantilla.rendirse = Boton("Me rindo", plantilla.zonaAcciones, acento, fuente);
            plantilla.avisoGuardado = Texto("Aviso guardado", raizRect, "", new Vector2(.035f,.003f), new Vector2(.965f,.03f), 24, Color.red, fuente, false);
            GameObject resultado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            if (resultado == null) throw new System.InvalidOperationException("No se pudo guardar el prefab.");
            AssetDatabase.SaveAssets();
            Selection.activeObject = resultado;
            EditorGUIUtility.PingObject(resultado);
            Debug.Log("Kakuro: prefab creado en " + ruta + ". Arrástralo al campo Plantilla interfaz del objeto DemoKakuro.");
            EditorUtility.DisplayDialog("Kakuro", "Plantilla creada en " + ruta + ".\n\nArrástrala al campo Plantilla interfaz del componente DemoKakuroMovil y guarda la escena. Puedes abrirla con doble clic para editarla.", "Aceptar");
        }
        finally { Object.DestroyImmediate(raiz); }
    }

    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static TMP_Text Texto(string nombre, Transform padre, string texto, Vector2 min, Vector2 max, float size, Color color, TMP_FontAsset fuente, bool negrita)
    {
        var r = Rect(nombre, padre, min, max);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = size;
        t.raycastTarget = false; t.enableWordWrapping = false;
        if (negrita) t.fontStyle = FontStyles.Bold;
        return t;
    }
    private static Button Boton(string nombre, Transform padre, Color color, TMP_FontAsset fuente)
    {
        var r = Rect("Boton " + nombre, padre, Vector2.zero, Vector2.one);
        var image = r.gameObject.AddComponent<Image>(); image.color = color;
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        Texto("Etiqueta", r, nombre, Vector2.zero, Vector2.one, 34, Color.white, fuente, true);
        return b;
    }
}
#endif
