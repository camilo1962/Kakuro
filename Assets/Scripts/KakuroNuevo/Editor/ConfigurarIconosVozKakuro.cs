#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ConfigurarIconosVozKakuro
{
    private const string RutaApagada = "Assets/KakuroUI/Iconos/VozApagada.png";
    private const string RutaActiva = "Assets/KakuroUI/Iconos/VozActivada.png";

    [MenuItem("Kakuro/Configurar iconos del boton Voz")]
    private static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorUtility.DisplayDialog("Kakuro", "Detén Play antes de configurar los iconos.", "Aceptar"); return; }
        DemoKakuroMovil demo = UnityEngine.Object.FindObjectOfType<DemoKakuroMovil>();
        if (demo == null)
        { EditorUtility.DisplayDialog("Kakuro", "Abre primero la escena DemoKakuro.", "Aceptar"); return; }
        var datos = new SerializedObject(demo);
        SerializedProperty campo = datos.FindProperty("botonVozPrefab");
        Button boton = campo != null ? campo.objectReferenceValue as Button : null;
        if (boton == null)
        { EditorUtility.DisplayDialog("Kakuro", "No hay un prefab de botón de voz asignado en DemoKakuroMovil. Configúralo antes.", "Aceptar"); return; }
        string rutaPrefab = AssetDatabase.GetAssetPath(boton);
        if (string.IsNullOrEmpty(rutaPrefab) || !rutaPrefab.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        { EditorUtility.DisplayDialog("Kakuro", "El botón asignado no es un prefab de proyecto válido.", "Aceptar"); return; }
        try
        {
            Sprite apagada = CargarSprite(RutaApagada);
            Sprite activa = CargarSprite(RutaActiva);
            if (apagada == null || activa == null)
                throw new InvalidOperationException("No se encuentran los dos PNG de Assets/KakuroUI/Iconos.");
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                if (raiz.GetComponent<Button>() == null)
                    throw new InvalidOperationException("El botón debe estar en la raíz del prefab.");
                if (raiz.GetComponent<KakuroIconoVoz>() != null)
                {
                    EditorUtility.DisplayDialog("Kakuro", "Este prefab ya tiene KakuroIconoVoz. No se han cambiado tus iconos. Puedes editarlos directamente en el Inspector del prefab.", "Aceptar");
                    return;
                }
                GameObject nuevo = new GameObject("Icono voz", typeof(RectTransform), typeof(Image));
                nuevo.transform.SetParent(raiz.transform, false);
                RectTransform rect = nuevo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(.1f, .1f); rect.anchorMax = new Vector2(.9f, .9f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Image imagen = nuevo.GetComponent<Image>();
                imagen.sprite = apagada; imagen.preserveAspect = true; imagen.raycastTarget = false;
                var config = raiz.AddComponent<KakuroIconoVoz>();
                config.imagen = imagen; config.vozApagada = apagada; config.vozActiva = activa;
                TMP_Text texto = raiz.GetComponentInChildren<TMP_Text>(true);
                if (texto != null) texto.gameObject.SetActive(false);
                GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
                if (guardado == null) throw new InvalidOperationException("No se pudo guardar el prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(raiz); }
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab));
            EditorUtility.DisplayDialog("Kakuro", "Iconos añadidos al prefab actual sin sustituir el botón.\n\nEn el prefab puedes editar los sprites en KakuroIconoVoz y el tamaño del hijo Icono voz.", "Aceptar");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Kakuro", "No se completó la configuración. Revisa la consola y la copia de seguridad.", "Aceptar");
        }
    }
    private static Sprite CargarSprite(string ruta)
    {
        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
#endif
