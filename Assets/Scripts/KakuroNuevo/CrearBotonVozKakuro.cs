#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CrearBotonVozKakuro
{
    [MenuItem("Kakuro/Crear boton editable de voz")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de crear el botón.", "Aceptar"); return; }
        DemoKakuroMovil demo = Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro.", "Aceptar"); return; }
        SerializedObject datos = new SerializedObject(demo);
        SerializedProperty propBoton = datos.FindProperty("botonVozPrefab");
        SerializedProperty propVoz = datos.FindProperty("voz");
        if (propBoton == null || propVoz == null)
        { EditorUtility.DisplayDialog("Kakuro", "Falta la versión actualizada de DemoKakuroMovil.", "Aceptar"); return; }
        if (propBoton.objectReferenceValue != null && !EditorUtility.DisplayDialog("Kakuro",
            "Ya tienes un botón de voz asignado. Se creará otro y se asignará a la escena; el anterior no se borrará.", "Crear copia", "Cancelar")) return;
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI")) AssetDatabase.CreateFolder("Assets", "KakuroUI");
        if (!AssetDatabase.IsValidFolder("Assets/KakuroUI/Paneles")) AssetDatabase.CreateFolder("Assets/KakuroUI", "Paneles");
        GameObject temporal = new GameObject("BotonVoz", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            var r = temporal.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(300, 80);
            Image imagen = temporal.GetComponent<Image>(); imagen.color = datos.FindProperty("acento").colorValue;
            Button boton = temporal.GetComponent<Button>(); boton.targetGraphic = imagen;
            GameObject objTexto = new GameObject("Etiqueta", typeof(RectTransform), typeof(TextMeshProUGUI));
            objTexto.transform.SetParent(temporal.transform, false);
            RectTransform rectTexto = objTexto.GetComponent<RectTransform>();
            rectTexto.anchorMin = Vector2.zero; rectTexto.anchorMax = Vector2.one; rectTexto.offsetMin = rectTexto.offsetMax = Vector2.zero;
            var texto = objTexto.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset fuente = datos.FindProperty("fuenteInterfaz").objectReferenceValue as TMP_FontAsset;
            if (fuente != null) texto.font = fuente;
            texto.text = "Voz"; texto.color = Color.white; texto.alignment = TextAlignmentOptions.Center;
            texto.enableAutoSizing = true; texto.fontSizeMin = 8; texto.fontSizeMax = 34;
            texto.fontStyle = FontStyles.Bold; texto.raycastTarget = false; texto.enableWordWrapping = false;
            string ruta = AssetDatabase.GenerateUniqueAssetPath("Assets/KakuroUI/Paneles/BotonVoz.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporal, ruta);
            if (prefab == null) throw new System.InvalidOperationException("No se pudo guardar el prefab.");
            VozKakuro voz = demo.GetComponent<VozKakuro>();
            if (voz == null) voz = Undo.AddComponent<VozKakuro>(demo.gameObject);
            datos.Update(); propBoton = datos.FindProperty("botonVozPrefab"); propVoz = datos.FindProperty("voz");
            propBoton.objectReferenceValue = prefab.GetComponent<Button>(); propVoz.objectReferenceValue = voz;
            datos.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets(); EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Kakuro", "Botón creado y asignado. Guarda la escena.\n\nEdita el prefab BotonVoz y el componente VozKakuro en el Inspector.", "Aceptar");
        }
        catch (System.Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Kakuro", "No se pudo crear el botón; revisa la consola.", "Aceptar"); }
        finally { Object.DestroyImmediate(temporal); }
    }
}
#endif
