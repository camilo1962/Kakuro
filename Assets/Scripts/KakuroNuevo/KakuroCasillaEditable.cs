using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Referencias visuales de un prefab de casilla. El tablero decide su contenido.
public sealed class KakuroCasillaEditable : MonoBehaviour
{
    public Image fondo;
    public Button boton;
    public TMP_Text numero;
    public TMP_Text pistaHorizontal;
    public TMP_Text pistaVertical;
    [Header("Solo para una pista doble")]
    public RectTransform diagonal;
    [Tooltip("Conserva la diagonal proporcionada al tamaño de la casilla.")]
    public bool ajustarDiagonalAutomaticamente = true;
    [Range(0.001f, 0.2f)] public float grosorDiagonalRelativo = 0.025f;
}
