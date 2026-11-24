#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static class ConfigurarCabeceraKakuro
{
    [MenuItem("Kakuro/Separar nivel y tiempo, voz a la izquierda")]
    private static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de modificar la cabecera.", "Aceptar"); return; }
        DemoKakuroMovil demo = UnityEngine.Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null) { EditorUtility.DisplayDialog("Kakuro", "Abre la escena DemoKakuro.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        SerializedProperty campo = datos.FindProperty("plantillaInterfaz");
        var plantilla = campo != null ? campo.objectReferenceValue as KakuroPlantillaInterfaz : null;
        if (plantilla == null) { EditorUtility.DisplayDialog("Kakuro", "Asigna primero la plantilla editable de juego en DemoKakuroMovil.", "Aceptar"); return; }
        string ruta = AssetDatabase.GetAssetPath(plantilla);
        if (string.IsNullOrEmpty(ruta) || !ruta.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        { EditorUtility.DisplayDialog("Kakuro", "La plantilla asignada no es un prefab válido.", "Aceptar"); return; }
        try
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                var ui = raiz.GetComponent<KakuroPlantillaInterfaz>();
                if (ui == null || ui.tiempo == null) throw new InvalidOperationException("No hay texto de tiempo en la raíz de la plantilla.");
                if (ui.dificultadNivel != null || ui.rotuloTiempo != null)
                    throw new InvalidOperationException("La cabecera ya tiene referencias asignadas; no se han cambiado. Edítalas en el prefab.");
                if (ui.tiempo.transform.parent != raiz.transform)
                    throw new InvalidOperationException("El texto Tiempo está dentro de otro contenedor. No se ha modificado para evitar alterar tu diseño; consulta la consola.");
                RectTransform tiempo = ui.tiempo.rectTransform;
                TMP_Text nivel = UnityEngine.Object.Instantiate(ui.tiempo, raiz.transform, false);
                nivel.name = "Dificultad y nivel"; nivel.text = "Fácil · Nivel 1"; nivel.gameObject.SetActive(true);
                TMP_Text rotulo = UnityEngine.Object.Instantiate(ui.tiempo, raiz.transform, false);
                rotulo.name = "Rotulo tiempo"; rotulo.text = "TIEMPO"; rotulo.gameObject.SetActive(true);
                Anclar(nivel.rectTransform, new Vector2(.04f, .825f), new Vector2(.65f, .855f));
                Anclar(rotulo.rectTransform, new Vector2(.68f, .855f), new Vector2(.96f, .885f));
                Anclar(tiempo, new Vector2(.68f, .825f), new Vector2(.96f, .855f));
                ui.tiempo.text = "00:00";
                ui.dificultadNivel = nivel; ui.rotuloTiempo = rotulo;
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                if (guardado == null) throw new InvalidOperationException("No se pudo guardar la plantilla.");
            }
            finally { PrefabUtility.UnloadPrefabContents(raiz); }
            // Respetar la altura, ancho y márgenes del botón Deshacer y reflejarlos a la izquierda.
            datos.Update();
            Vector2 minUndo = datos.FindProperty("anclaMinDeshacer").vector2Value;
            Vector2 maxUndo = datos.FindProperty("anclaMaxDeshacer").vector2Value;
            datos.FindProperty("anclaMinVoz").vector2Value = new Vector2(1f - maxUndo.x, minUndo.y);
            datos.FindProperty("anclaMaxVoz").vector2Value = new Vector2(1f - minUndo.x, maxUndo.y);
            datos.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Kakuro", "Cabecera separada y botón Voz colocado a la izquierda.\n\nGuarda la escena y ajusta posiciones, tamaños y fuentes en el prefab de interfaz.", "Aceptar");
        }
        catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("Kakuro", "No se pudo completar la configuración. Revisa la consola.", "Aceptar"); }
    }
    private static void Anclar(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
#endif
