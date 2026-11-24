#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearEstadisticasKakuro
{
    [MenuItem("Kakuro/Anadir estadisticas al menu editable")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de añadir estadísticas.", "Aceptar"); return; }
        MainMenuManager manager = UnityEngine.Object.FindObjectOfType<MainMenuManager>();
        if (manager == null) { EditorUtility.DisplayDialog("Kakuro", "Abre primero la escena MainMenu.", "Aceptar"); return; }
        var datos = new SerializedObject(manager);
        SerializedProperty campo = datos.FindProperty("plantillaMenu");
        KakuroMenuEditable plantilla = campo != null ? campo.objectReferenceValue as KakuroMenuEditable : null;
        if (plantilla == null) { EditorUtility.DisplayDialog("Kakuro", "No hay un menú editable asignado en MainMenuManager.", "Aceptar"); return; }
        string ruta = AssetDatabase.GetAssetPath(plantilla);
        if (string.IsNullOrEmpty(ruta) || !ruta.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        { EditorUtility.DisplayDialog("Kakuro", "El menú asignado no es un prefab válido.", "Aceptar"); return; }
        bool usaLayout = true;
        try
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                KakuroMenuEditable ui = raiz.GetComponent<KakuroMenuEditable>();
                if (ui == null || !ui.ReferenciasValidas()) throw new InvalidOperationException("Faltan referencias del menú editable.");
                if (raiz.GetComponent<KakuroEstadisticasEditable>() != null)
                    throw new InvalidOperationException("El menú ya tiene estadísticas. No se ha alterado; edita el prefab existente.");
                if (ui.abrirRanking.transform.parent == null) throw new InvalidOperationException("El botón Ranking no tiene contenedor.");
                usaLayout = ui.abrirRanking.transform.parent.GetComponent<LayoutGroup>() != null;
                var stats = raiz.AddComponent<KakuroEstadisticasEditable>();
                stats.inicio = ui.inicio;
                Button abrir = UnityEngine.Object.Instantiate(ui.abrirRanking, ui.abrirRanking.transform.parent, false);
                abrir.name = "Boton Estadisticas"; abrir.onClick = new Button.ButtonClickedEvent();
                abrir.transform.SetSiblingIndex(ui.abrirRanking.transform.GetSiblingIndex() + 1);
                TMP_Text textoBoton = abrir.GetComponentInChildren<TMP_Text>(true);
                if (textoBoton == null) throw new InvalidOperationException("El botón Ranking no tiene una etiqueta TextMeshPro.");
                textoBoton.text = "Estadísticas"; textoBoton.gameObject.SetActive(true); abrir.gameObject.SetActive(true);
                if (!usaLayout)
                {
                    RectTransform ar = abrir.transform as RectTransform;
                    if (ar != null) ar.anchoredPosition += new Vector2(0f, -Mathf.Max(ar.rect.height, 80f) - 10f);
                }
                stats.abrir = abrir;
                RectTransform panel = Rect("Estadisticas", ui.zonaSegura, Vector2.zero, Vector2.one);
                stats.panel = panel.gameObject;
                Image fondo = panel.gameObject.AddComponent<Image>(); fondo.color = new Color(0,0,0,0); fondo.raycastTarget = true;
                TMP_FontAsset fuente = ui.mensajeRanking != null ? ui.mensajeRanking.font : TMP_Settings.defaultFontAsset;
                Color azul = new Color32(38,78,127,255), oscuro = new Color32(35,46,61,255);
                Texto("Titulo", panel, "Estadísticas", new Vector2(.06f,.88f), new Vector2(.94f,.97f), 64, azul, fuente, true);
                Texto("Subtitulo", panel, "Tu progreso por dificultad", new Vector2(.05f,.82f), new Vector2(.95f,.88f), 33, oscuro, fuente, false);
                Texto("Cabecera dificultad", panel, "Modo", new Vector2(.04f,.71f), new Vector2(.43f,.79f), 32, oscuro, fuente, true);
                Texto("Cabecera victorias", panel, "Ganadas", new Vector2(.43f,.71f), new Vector2(.70f,.79f), 30, oscuro, fuente, true);
                Texto("Cabecera rendiciones", panel, "Me rindo", new Vector2(.70f,.71f), new Vector2(.97f,.79f), 30, oscuro, fuente, true);
                Fila(panel, "Fácil · 7×7", .60f, .70f, fuente, oscuro, out stats.ganadasFacil, out stats.rendicionesFacil);
                Fila(panel, "Medio · 9×9", .48f, .58f, fuente, oscuro, out stats.ganadasMedio, out stats.rendicionesMedio);
                Fila(panel, "Difícil · 12×12", .36f, .46f, fuente, oscuro, out stats.ganadasDificil, out stats.rendicionesDificil);
                Texto("Aclaracion", panel, "Me rindo cuenta solo las rendiciones. Datos de este dispositivo.", new Vector2(.08f,.21f), new Vector2(.92f,.31f), 26, oscuro, fuente, false, true);
                Button volver = UnityEngine.Object.Instantiate(ui.volverRanking, panel, false);
                volver.name = "Volver estadisticas"; volver.onClick = new Button.ButtonClickedEvent();
                var rv = volver.transform as RectTransform;
                if (rv == null) throw new InvalidOperationException("El botón Volver no es un elemento UI.");
                Anclar(rv, new Vector2(.08f,.05f), new Vector2(.92f,.15f));
                volver.gameObject.SetActive(true);
                stats.volver = volver;
                panel.gameObject.SetActive(false);
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                if (guardado == null) throw new InvalidOperationException("No se pudo guardar el menú.");
            }
            finally { PrefabUtility.UnloadPrefabContents(raiz); }
            AssetDatabase.SaveAssets(); EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            EditorUtility.DisplayDialog("Kakuro", "Panel y botón añadidos al prefab de menú actual.\n\nAbre el prefab para personalizarlos." + (usaLayout ? " Si los botones quedan ajustados, amplía su contenedor." : " Coloca el nuevo botón manualmente: su contenedor no usa un Layout Group."), "Aceptar");
        }
        catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Kakuro", "No se ha podido completar el panel. Revisa la consola.", "Aceptar"); }
    }
    private static void Fila(Transform panel, string modo, float y0, float y1, TMP_FontAsset fuente, Color color, out TMP_Text ganadas, out TMP_Text rendiciones)
    {
        Texto("Modo " + modo, panel, modo, new Vector2(.04f,y0), new Vector2(.43f,y1), 30, color, fuente, true);
        ganadas = Texto("Ganadas " + modo, panel, "0", new Vector2(.43f,y0), new Vector2(.70f,y1), 38, color, fuente, true);
        rendiciones = Texto("Me rindo " + modo, panel, "0", new Vector2(.70f,y0), new Vector2(.97f,y1), 38, color, fuente, true);
    }
    private static RectTransform Rect(string nombre, Transform padre, Vector2 min, Vector2 max)
    {
        GameObject o = new GameObject(nombre, typeof(RectTransform)); o.transform.SetParent(padre, false);
        RectTransform r = o.GetComponent<RectTransform>(); Anclar(r, min, max); return r;
    }
    private static void Anclar(RectTransform r, Vector2 min, Vector2 max)
    { r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static TMP_Text Texto(string nombre, Transform padre, string contenido, Vector2 min, Vector2 max, float tam, Color color, TMP_FontAsset fuente, bool bold, bool wrap = false)
    {
        var r = Rect(nombre, padre, min, max);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = contenido; t.color = color; t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = tam;
        t.enableWordWrapping = wrap; t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        return t;
    }
}
#endif
