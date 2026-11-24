#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearPanelDificultad
{
    [MenuItem("Kakuro/Crear prefab editable de dificultad")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear el panel.", "Aceptar"); return; }
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro antes de crear el panel.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        SerializedProperty referencia = datos.FindProperty("panelDificultadPrefab");
        if (referencia == null)
        { EditorUtility.DisplayDialog("Kakuro", "Falta el script DemoKakuroMovil actualizado. Revisa la consola.", "Aceptar"); return; }
        if (referencia.objectReferenceValue != null && !EditorUtility.DisplayDialog("Kakuro", "Ya tienes un panel de dificultad asignado. Se creará otro prefab y se cambiará la referencia en la escena. El anterior no se borrará.", "Crear copia", "Cancelar")) return;
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/Paneles")) AssetDatabase.CreateFolder("Assets/KakuroUI", "Paneles");
        Color acento = datos.FindProperty("acento").colorValue;
        TMP_FontAsset fuente = datos.FindProperty("fuenteInterfaz").objectReferenceValue as TMP_FontAsset;
        if (fuente == null) fuente = TMP_Settings.defaultFontAsset;
        GameObject raiz = null;
        try
        {
            raiz = Construir(acento, fuente);
            string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/Paneles/PanelDificultad.prefab");
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            if (guardado == null) throw new System.InvalidOperationException("No se pudo guardar el prefab.");
            KakuroPanelDificultad ui = guardado.GetComponent<KakuroPanelDificultad>();
            if (ui == null || !ui.ReferenciasValidas()) throw new System.InvalidOperationException("El panel tiene referencias incompletas.");
            referencia.objectReferenceValue = ui;
            datos.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(guardado);
            EditorUtility.DisplayDialog("Kakuro", "Panel creado y asignado: " + ruta + ".\n\nGuarda la escena y haz doble clic en el prefab para editarlo.", "Aceptar");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la creación. Revisa la consola y la referencia de la escena.", "Aceptar");
        }
        finally { if (raiz != null) Object.DestroyImmediate(raiz); }
    }

    private static GameObject Construir(Color acento, TMP_FontAsset fuente)
    {
        var root = new GameObject("PanelDificultad", typeof(RectTransform));
        RectTransform velo = root.GetComponent<RectTransform>(); velo.sizeDelta = new Vector2(1080, 1920);
        Image sombra = root.AddComponent<Image>(); sombra.color = new Color(0f, 0f, 0f, .65f); sombra.raycastTarget = true;
        KakuroPanelDificultad ui = root.AddComponent<KakuroPanelDificultad>();
        RectTransform panel = Rect("Panel", velo, new Vector2(.08f, .23f), new Vector2(.92f, .77f));
        panel.gameObject.AddComponent<Image>().color = Color.white;
        ui.titulo = Texto("Titulo", panel, "Elige una dificultad", new Vector2(.05f,.83f), new Vector2(.95f,.97f), 45, acento, fuente, true);
        ui.aviso = Texto("Aviso", panel, "", new Vector2(.05f,.75f), new Vector2(.95f,.83f), 25, Color.red, fuente, false);
        RectTransform botones = Rect("Opciones", panel, new Vector2(.10f,.06f), new Vector2(.90f,.74f));
        var layout = botones.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        ui.facil = Boton("Facil", botones, "Fácil 7×7", acento, fuente);
        ui.medio = Boton("Medio", botones, "Medio 9×9", acento, fuente);
        ui.dificil = Boton("Dificil", botones, "Difícil 12×12", acento, fuente);
        ui.continuar = Boton("Continuar", botones, "Continuar partida", acento, fuente);
        ui.menuPrincipal = Boton("Menu principal", botones, "Menú principal", acento, fuente);
        ui.volverTablero = Boton("Volver tablero", botones, "Volver al tablero", acento, fuente);
        return root;
    }
    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static TMP_Text Texto(string nombre, Transform padre, string contenido, Vector2 min, Vector2 max,
        float tam, Color color, TMP_FontAsset fuente, bool negrita)
    {
        RectTransform rect = Rect(nombre, padre, min, max);
        var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = contenido; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = tam;
        t.raycastTarget = false; t.enableWordWrapping = false;
        if (negrita) t.fontStyle = FontStyles.Bold;
        return t;
    }
    private static Button Boton(string nombre, Transform padre, string texto, Color color, TMP_FontAsset fuente)
    {
        RectTransform rect = Rect("Boton " + nombre, padre, Vector2.zero, Vector2.one);
        Image imagen = rect.gameObject.AddComponent<Image>(); imagen.color = color;
        Button boton = rect.gameObject.AddComponent<Button>(); boton.targetGraphic = imagen;
        Texto("Etiqueta", rect, texto, Vector2.zero, Vector2.one, 34, Color.white, fuente, true);
        return boton;
    }
}
#endif
