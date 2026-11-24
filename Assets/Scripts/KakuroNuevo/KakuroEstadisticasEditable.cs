using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Vista local y editable. Solo lee los contadores ya utilizados por el juego.
public sealed class KakuroEstadisticasEditable : MonoBehaviour
{
    [Header("Pantalla y navegación")]
    public GameObject inicio;
    public GameObject panel;
    public Button abrir;
    public Button volver;
    [Header("Valores por dificultad")]
    public TMP_Text ganadasFacil, rendicionesFacil;
    public TMP_Text ganadasMedio, rendicionesMedio;
    public TMP_Text ganadasDificil, rendicionesDificil;
    private bool conectado;

    private void OnEnable()
    {
        if (!Validas()) { Debug.LogWarning("Kakuro: faltan referencias del panel de estadísticas."); return; }
        panel.SetActive(false);
        abrir.onClick.AddListener(Mostrar); volver.onClick.AddListener(Volver);
        conectado = true;
    }
    private void OnDisable()
    {
        if (!conectado) return;
        if (abrir != null) abrir.onClick.RemoveListener(Mostrar);
        if (volver != null) volver.onClick.RemoveListener(Volver);
        conectado = false;
    }
    private bool Validas()
    {
        return inicio != null && panel != null && abrir != null && volver != null &&
            ganadasFacil != null && rendicionesFacil != null && ganadasMedio != null &&
            rendicionesMedio != null && ganadasDificil != null && rendicionesDificil != null;
    }
    private void Update()
    {
        if (panel != null && panel.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape)) Volver();
    }
    public void Mostrar()
    {
        if (!Validas()) return;
        Refrescar(); inicio.SetActive(false); panel.SetActive(true);
    }
    public void Volver()
    {
        if (!Validas()) return;
        panel.SetActive(false); inicio.SetActive(true);
    }
    private void Refrescar()
    {
        Leer(7, ganadasFacil, rendicionesFacil);
        Leer(9, ganadasMedio, rendicionesMedio);
        Leer(12, ganadasDificil, rendicionesDificil);
    }
    private static void Leer(int n, TMP_Text ganadas, TMP_Text rendiciones)
    {
        try
        {
            string tamano = n + "x" + n;
            ganadas.text = Mathf.Max(0, PlayerPrefs.GetInt("Kakuro.V1.Victorias." + tamano, 0)).ToString();
            rendiciones.text = Mathf.Max(0, PlayerPrefs.GetInt("Kakuro.V1.NoResueltos." + tamano, 0)).ToString();
        }
        catch (Exception e)
        {
            ganadas.text = rendiciones.text = "—";
            Debug.LogError("Kakuro: no se pudieron leer las estadísticas. " + e);
        }
    }
}
