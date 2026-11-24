using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Referencias del prefab editable. Los datos del ranking siguen en KakuroRanking.
public sealed class KakuroMenuEditable : MonoBehaviour
{
    [Header("Pantallas")]
    public RectTransform zonaSegura;
    public GameObject inicio;
    public GameObject reglas;
    public GameObject ranking;
    [Header("Botones")]
    public Button jugar;
    public Button salir;
    public Button abrirReglas;
    public Button abrirRanking;
    public Button volverReglas;
    public Button volverRanking;
    public Button facil;
    public Button medio;
    public Button dificil;
    [Header("Contenido desplazable")]
    public ScrollRect scrollReglas;
    public ScrollRect scrollRanking;
    public TMP_Text mensajeRanking;
    public GameObject[] filasRanking;
    public TMP_Text[] posicionesRanking;
    public TMP_Text[] tiemposRanking;
    [Header("Colores de las pestañas del ranking")]
    public Color pestanaActiva = new Color32(38, 78, 127, 255);
    public Color pestanaInactiva = new Color32(105, 128, 151, 255);
    private Rect ultimaZonaSegura;
    private int ultimoAncho = -1, ultimoAlto = -1;

    public bool ReferenciasValidas()
    {
        if (GetComponent<Canvas>() == null || zonaSegura == null || inicio == null || reglas == null || ranking == null ||
            jugar == null || salir == null || abrirReglas == null || abrirRanking == null || volverReglas == null ||
            volverRanking == null || facil == null || medio == null || dificil == null || scrollReglas == null ||
            scrollRanking == null || mensajeRanking == null || filasRanking == null || posicionesRanking == null ||
            tiemposRanking == null || filasRanking.Length != 8 || posicionesRanking.Length != 8 || tiemposRanking.Length != 8)
            return false;
        for (int i = 0; i < 8; i++)
            if (filasRanking[i] == null || posicionesRanking[i] == null || tiemposRanking[i] == null) return false;
        return true;
    }

    public void Inicializar()
    {
        jugar.onClick.AddListener(Jugar);
        salir.onClick.AddListener(Salir);
        abrirReglas.onClick.AddListener(MostrarReglas);
        abrirRanking.onClick.AddListener(MostrarRanking);
        volverReglas.onClick.AddListener(MostrarInicio);
        volverRanking.onClick.AddListener(MostrarInicio);
        facil.onClick.AddListener(() => VerRanking(7));
        medio.onClick.AddListener(() => VerRanking(9));
        dificil.onClick.AddListener(() => VerRanking(12));
        AjustarZonaSegura();
        MostrarInicio();
    }
    private void Update()
    {
        if (Screen.width != ultimoAncho || Screen.height != ultimoAlto || Screen.safeArea != ultimaZonaSegura)
            AjustarZonaSegura();
    }
    private void AjustarZonaSegura()
    {
        if (zonaSegura == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        zonaSegura.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        zonaSegura.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        zonaSegura.offsetMin = zonaSegura.offsetMax = Vector2.zero;
        ultimaZonaSegura = area; ultimoAncho = Screen.width; ultimoAlto = Screen.height;
    }
    private void Jugar() { SceneManager.LoadScene("DemoKakuro"); }
    private void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void MostrarInicio() { inicio.SetActive(true); reglas.SetActive(false); ranking.SetActive(false); }
    private void MostrarReglas()
    {
        inicio.SetActive(false); reglas.SetActive(true); ranking.SetActive(false);
        Canvas.ForceUpdateCanvases(); scrollReglas.verticalNormalizedPosition = 1f;
    }
    private void MostrarRanking()
    {
        inicio.SetActive(false); reglas.SetActive(false); ranking.SetActive(true);
        VerRanking(7);
    }
    private void VerRanking(int tamano)
    {
        PintarPestana(facil, tamano == 7); PintarPestana(medio, tamano == 9); PintarPestana(dificil, tamano == 12);
        List<long> tiempos; string error;
        bool ok = KakuroRanking.Obtener(tamano, out tiempos, out error);
        if (!ok)
        {
            Debug.LogError("Kakuro: no se pudo leer el ranking. " + error);
            mensajeRanking.text = "No se pudo leer el ranking. Revisa la consola.";
        }
        else if (tiempos.Count == 0)
            mensajeRanking.text = "Aún no hay tiempos guardados en esta dificultad. ¡Completa un puzzle para empezar!";
        mensajeRanking.gameObject.SetActive(!ok || tiempos.Count == 0);
        for (int i = 0; i < 8; i++)
        {
            bool visible = ok && i < tiempos.Count;
            filasRanking[i].SetActive(visible);
            if (visible)
            {
                posicionesRanking[i].text = (i + 1) + ".";
                tiemposRanking[i].text = KakuroRanking.FormatearTiempo(tiempos[i]);
            }
        }
        Canvas.ForceUpdateCanvases(); scrollRanking.verticalNormalizedPosition = 1f;
    }
    private void PintarPestana(Button boton, bool activa)
    {
        Image fondo = boton.GetComponent<Image>();
        if (fondo != null) fondo.color = activa ? pestanaActiva : pestanaInactiva;
    }
}
