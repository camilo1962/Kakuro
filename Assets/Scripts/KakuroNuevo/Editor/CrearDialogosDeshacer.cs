#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearDialogosDeshacer
{
    [MenuItem("Kakuro/Crear selector, dialogos y boton deshacer")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear los prefabs.", "Aceptar"); return; }
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro antes de crear los prefabs.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        string[] props = { "selectorNumerosPrefab", "dialogoSalidaPrefab", "dialogoCambioPrefab", "botonDeshacerPrefab" };
        foreach (string nombre in props)
        {
            SerializedProperty p = datos.FindProperty(nombre);
            if (p == null) { EditorUtility.DisplayDialog("Kakuro", "Falta el script actualizado. Revisa la consola.", "Aceptar"); return; }
            if (p.objectReferenceValue != null)
            {
                if (!EditorUtility.DisplayDialog("Kakuro", "Ya hay alguna referencia asignada. Se crearán cuatro prefabs nuevos y se sustituirán las referencias de la escena. Los anteriores no se borrarán.", "Crear copias", "Cancelar")) return;
                break;
            }
        }
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/Paneles")) AssetDatabase.CreateFolder("Assets/KakuroUI", "Paneles");
        Color acento = datos.FindProperty("acento").colorValue;
        TMP_FontAsset fuente = datos.FindProperty("fuenteInterfaz").objectReferenceValue as TMP_FontAsset;
        TMP_FontAsset fuenteNumeros = datos.FindProperty("fuenteNumeros").objectReferenceValue as TMP_FontAsset;
        if (fuente == null) fuente = TMP_Settings.defaultFontAsset;
        if (fuenteNumeros == null) fuenteNumeros = fuente;
        string[] nombres = { "SelectorNumeros", "DialogoSalida", "DialogoCambio", "BotonDeshacer" };
        var assets = new Object[4];
        try
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject raiz = Construir(i, nombres[i], acento, fuente, fuenteNumeros);
                try
                {
                    string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/Paneles/" + nombres[i] + ".prefab");
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                    if (prefab == null) throw new System.InvalidOperationException("No se pudo guardar " + ruta);
                    if (i < 3)
                    {
                        var ui = prefab.GetComponent<KakuroDialogoExtra>();
                        if (ui == null || !ui.ReferenciasValidas(i)) throw new System.InvalidOperationException("Referencias incompletas en " + ruta);
                        assets[i] = ui;
                    }
                    else
                    {
                        var boton = prefab.GetComponent<Button>();
                        if (boton == null) throw new System.InvalidOperationException("Falta el botón en " + ruta);
                        assets[i] = boton;
                    }
                }
                finally { Object.DestroyImmediate(raiz); }
            }
            for (int i = 0; i < 4; i++) datos.FindProperty(props[i]).objectReferenceValue = assets[i];
            datos.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets(); EditorGUIUtility.PingObject(assets[0]);
            EditorUtility.DisplayDialog("Kakuro", "Prefabs creados y asignados en Assets/KakuroUI/Paneles.\n\nGuarda la escena. Ábrelos con doble clic para editarlos.", "Aceptar");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la creación. Revisa la consola y las referencias de la escena.", "Aceptar");
        }
    }

    private static GameObject Construir(int tipo, string nombre, Color acento, TMP_FontAsset fuente, TMP_FontAsset fuenteNumeros)
    {
        var root = new GameObject(nombre, typeof(RectTransform));
        RectTransform velo = root.GetComponent<RectTransform>();
        if (tipo == 3)
        {
            velo.sizeDelta = new Vector2(300,80);
            var fondo = root.AddComponent<Image>(); fondo.color = acento;
            var b = root.AddComponent<Button>(); b.targetGraphic = fondo;
            Texto("Etiqueta", velo, "Deshacer", Vector2.zero, Vector2.one, 34, Color.white, fuente, true);
            return root;
        }
        velo.sizeDelta = new Vector2(1080,1920);
        var ui = root.AddComponent<KakuroDialogoExtra>();
        Image sombra = root.AddComponent<Image>(); sombra.raycastTarget = true;
        sombra.color = tipo == 0 ? new Color(0,0,0,0) : new Color(0,0,0,.65f);
        if (tipo == 0)
        {
            ui.cerrarExterior = root.AddComponent<Button>(); ui.cerrarExterior.targetGraphic = sombra;
            ui.cerrarExterior.transition = Selectable.Transition.None;
            RectTransform panel = Rect("Panel numerico", velo, new Vector2(.5f,.495f), new Vector2(.5f,.495f));
            panel.pivot = new Vector2(.5f,.5f); panel.anchoredPosition = Vector2.zero; panel.sizeDelta = new Vector2(360,480);
            panel.gameObject.AddComponent<Image>().color = acento; ui.panelNumerico = panel;
            RectTransform numeros = Rect("Numeros", panel, new Vector2(.02f,.255f), new Vector2(.98f,.985f));
            var rejilla = numeros.gameObject.AddComponent<GridLayoutGroup>(); rejilla.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rejilla.constraintCount = 3; rejilla.childAlignment = TextAnchor.MiddleCenter; rejilla.spacing = new Vector2(4,4); rejilla.cellSize = new Vector2(112,112);
            ui.rejillaNumeros = rejilla; ui.numeros = new Button[9];
            for (int i = 0; i < 9; i++) ui.numeros[i] = Boton((i+1).ToString(), numeros, (i+1).ToString(), Color.black, fuenteNumeros, 64);
            RectTransform zonaBorrar = Rect("Zona borrar", panel, new Vector2(.34f,.015f), new Vector2(.66f,.245f));
            ui.borrar = Boton("Borrar", zonaBorrar, "X", Color.black, fuenteNumeros, 64);
            return root;
        }
        RectTransform caja = Rect("Panel", velo, tipo == 1 ? new Vector2(.08f,.25f) : new Vector2(.08f,.36f), tipo == 1 ? new Vector2(.92f,.75f) : new Vector2(.92f,.64f));
        caja.gameObject.AddComponent<Image>().color = Color.white;
        if (tipo == 1)
        {
            Texto("Titulo", caja, "Salir del juego", new Vector2(.05f,.82f), new Vector2(.95f,.96f), 48, acento, fuente, true);
            ui.pregunta = Texto("Pregunta", caja, "¿Quieres guardar la partida actual?", new Vector2(.05f,.69f), new Vector2(.95f,.82f), 32, Color.black, fuente, false);
            ui.aviso = Texto("Aviso", caja, "Salir sin guardar descarta esta partida.", new Vector2(.05f,.59f), new Vector2(.95f,.69f), 25, Color.red, fuente, false);
            RectTransform botones = Rect("Opciones", caja, new Vector2(.10f,.06f), new Vector2(.90f,.58f));
            var layout = botones.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            ui.guardarSalir = Boton("Guardar y salir", botones, "Guardar y salir", acento, fuente, 34);
            ui.descartarSalir = Boton("Salir sin guardar", botones, "Salir sin guardar", acento, fuente, 34);
            ui.salir = Boton("Salir", botones, "Salir", acento, fuente, 34);
            ui.cancelar = Boton("Cancelar", botones, "Cancelar", acento, fuente, 34);
        }
        else
        {
            ui.pregunta = Texto("Pregunta", caja, "Se perderá la partida actual. ¿Continuar?", new Vector2(.05f,.53f), new Vector2(.95f,.92f), 35, Color.black, fuente, false);
            RectTransform botones = Rect("Opciones", caja, new Vector2(.06f,.10f), new Vector2(.94f,.42f));
            var layout = botones.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 10; layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            ui.cancelar = Boton("Cancelar", botones, "Cancelar", acento, fuente, 34);
            ui.cambiar = Boton("Cambiar", botones, "Cambiar", acento, fuente, 34);
        }
        return root;
    }
    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(nombre, typeof(RectTransform)); obj.transform.SetParent(padre, false);
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static TMP_Text Texto(string nombre, Transform padre, string texto, Vector2 min, Vector2 max, float tam, Color color, TMP_FontAsset fuente, bool negrita)
    {
        RectTransform r = Rect(nombre, padre, min, max); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = tam;
        t.raycastTarget = false; t.enableWordWrapping = false;
        if (negrita) t.fontStyle = FontStyles.Bold;
        return t;
    }
    private static Button Boton(string nombre, Transform padre, string texto, Color color, TMP_FontAsset fuente, float tam)
    {
        var r = Rect("Boton " + nombre, padre, Vector2.zero, Vector2.one);
        var imagen = r.gameObject.AddComponent<Image>(); imagen.color = color;
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = imagen;
        Texto("Etiqueta", r, texto, Vector2.zero, Vector2.one, tam, Color.white, fuente, true);
        return b;
    }
}
#endif
