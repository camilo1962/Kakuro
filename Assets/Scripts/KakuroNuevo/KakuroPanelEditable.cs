using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Referencias visuales de los prefabs de paneles: la lógica vive en DemoKakuroMovil.
public sealed class KakuroPanelEditable : MonoBehaviour
{
    [Header("Contenido")]
    public TMP_Text titulo;
    public TMP_Text detalle;
    [Header("Botones principales")]
    public Button botonPrincipal;
    public Button botonSecundario;
    [Header("Solo consulta de combinaciones")]
    public Button botonFila;
    public Button botonColumna;
    public TMP_Text textoFila;
    public TMP_Text textoColumna;

    public bool ReferenciasValidas(bool esConsulta)
    {
        if (!(transform is RectTransform) || titulo == null || detalle == null || botonPrincipal == null)
            return false;
        if (esConsulta)
            return botonFila != null && botonColumna != null && textoFila != null && textoColumna != null;
        return botonSecundario != null;
    }
}
