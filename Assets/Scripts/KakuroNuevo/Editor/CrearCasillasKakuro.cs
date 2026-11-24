#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearCasillasKakuro
{
    private enum Tipo { Jugable, Gris, Fila, Columna, Doble }

    [MenuItem("Kakuro/Crear prefabs editables de casillas")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear los prefabs.", "Aceptar"); return; }
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro antes de crear las casillas.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        Sprite fila = datos.FindProperty("pistaFila").objectReferenceValue as Sprite;
        Sprite columna = datos.FindProperty("pistaColumna").objectReferenceValue as Sprite;
        Sprite gris = datos.FindProperty("casillaLlena").objectReferenceValue as Sprite;
        if (fila == null || columna == null || gris == null)
        { EditorUtility.DisplayDialog("Kakuro", "Asigna primero los tres sprites de pistas y casilla llena en DemoKakuroMovil.", "Aceptar"); return; }
        TMP_FontAsset general = datos.FindProperty("fuenteInterfaz").objectReferenceValue as TMP_FontAsset;
        TMP_FontAsset numeros = datos.FindProperty("fuenteNumeros").objectReferenceValue as TMP_FontAsset;
        TMP_FontAsset pistas = datos.FindProperty("fuentePistas").objectReferenceValue as TMP_FontAsset;
        if (general == null) general = TMP_Settings.defaultFontAsset;
        if (numeros == null) numeros = general;
        if (pistas == null) pistas = general;
        string[] propiedades = { "casillaJugablePrefab", "casillaGrisPrefab", "pistaFilaPrefab", "pistaColumnaPrefab", "pistaDoblePrefab" };
        foreach (string propiedad in propiedades)
        {
            if (datos.FindProperty(propiedad).objectReferenceValue != null &&
                !EditorUtility.DisplayDialog("Kakuro", "Ya hay prefabs de casillas asignados. Se crearán copias nuevas y se sustituirán las cinco referencias en esta escena. No se borrarán los prefabs existentes.", "Crear copias", "Cancelar")) return;
            if (datos.FindProperty(propiedad).objectReferenceValue != null) break;
        }
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/Casillas")) AssetDatabase.CreateFolder("Assets/KakuroUI", "Casillas");
        string[] nombres = { "CasillaJugable", "CasillaGris", "PistaFila", "PistaColumna", "PistaDoble" };
        var prefabs = new KakuroCasillaEditable[5];
        try
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject raiz = Construir((Tipo)i, nombres[i], fila, columna, gris, numeros, pistas);
                try
                {
                    string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/Casillas/" + nombres[i] + ".prefab");
                    GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    if (guardado == null) throw new System.InvalidOperationException("No se pudo guardar " + ruta);
                    prefabs[i] = guardado.GetComponent<KakuroCasillaEditable>();
                    if (prefabs[i] == null) throw new System.InvalidOperationException("Falta el componente de casilla en " + ruta);
                }
                finally { Object.DestroyImmediate(raiz); }
            }
            for (int i = 0; i < 5; i++) datos.FindProperty(propiedades[i]).objectReferenceValue = prefabs[i];
            datos.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(prefabs[0].gameObject);
            Debug.Log("Kakuro: cinco prefabs creados y asignados en " + demo.gameObject.name + ". Guarda la escena.");
            EditorUtility.DisplayDialog("Kakuro", "Creados y asignados los cinco prefabs en Assets/KakuroUI/Casillas.\n\nGuarda la escena. Abre cada prefab con doble clic para editarlo.", "Aceptar");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la creación. Revisa la consola. Puede que se hayan creado algunos archivos; revisa también las referencias de la escena.", "Aceptar");
        }
    }

    private static GameObject Construir(Tipo tipo, string nombre, Sprite fila, Sprite columna, Sprite gris, TMP_FontAsset numeros, TMP_FontAsset pistas)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(128, 128);
        Image imagen = obj.AddComponent<Image>(); imagen.color = Color.white;
        imagen.sprite = tipo == Tipo.Fila ? fila : tipo == Tipo.Columna ? columna :
            (tipo == Tipo.Gris || tipo == Tipo.Doble ? gris : null);
        var ui = obj.AddComponent<KakuroCasillaEditable>(); ui.fondo = imagen;
        if (tipo != Tipo.Gris)
        {
            ui.boton = obj.AddComponent<Button>(); ui.boton.targetGraphic = imagen;
            ui.boton.transition = Selectable.Transition.None;
        }
        if (tipo == Tipo.Jugable)
        {
            ui.numero = Texto("Numero", rect, Vector2.zero, Vector2.one, 52, numeros);
            ui.numero.text = "5"; // Solo para ver la apariencia al editar el prefab.
        }
        if (tipo == Tipo.Doble)
        {
            RectTransform diagonal = Rect("Diagonal", rect, new Vector2(.5f,.5f), new Vector2(.5f,.5f));
            diagonal.pivot = new Vector2(.5f,.5f); diagonal.anchoredPosition = Vector2.zero;
            diagonal.sizeDelta = new Vector2(128 * 1.4142f, 3.2f);
            diagonal.localRotation = Quaternion.Euler(0, 0, -45);
            Image linea = diagonal.gameObject.AddComponent<Image>(); linea.color = Color.black; linea.raycastTarget = false;
            ui.diagonal = diagonal;
        }
        if (tipo == Tipo.Fila || tipo == Tipo.Doble)
        {
            ui.pistaHorizontal = Texto("Pista fila", rect, new Vector2(.52f,.52f), new Vector2(.98f,.98f), 32, pistas);
            ui.pistaHorizontal.text = "7";
        }
        if (tipo == Tipo.Columna || tipo == Tipo.Doble)
        {
            ui.pistaVertical = Texto("Pista columna", rect, new Vector2(.02f,.02f), new Vector2(.48f,.48f), 32, pistas);
            ui.pistaVertical.text = "9";
        }
        return obj;
    }

    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private static TMP_Text Texto(string nombre, Transform padre, Vector2 min, Vector2 max, float size, TMP_FontAsset fuente)
    {
        var r = Rect(nombre, padre, min, max);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = ""; t.color = Color.black; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true;
        t.fontSizeMin = 8; t.fontSizeMax = size; t.raycastTarget = false; t.enableWordWrapping = false;
        return t;
    }
}
#endif
