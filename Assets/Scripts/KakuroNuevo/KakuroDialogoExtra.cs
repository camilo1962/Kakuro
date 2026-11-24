using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Referencias de los prefabs de selector, salida y confirmación de cambio.
public sealed class KakuroDialogoExtra : MonoBehaviour
{
    [Header("Selector de números")]
    public RectTransform panelNumerico;
    public GridLayoutGroup rejillaNumeros;
    public Button cerrarExterior;
    public Button[] numeros;
    public Button borrar;
    public bool ajustarTamanoAutomaticamente = true;
    [Header("Salida y confirmación")]
    public TMP_Text pregunta;
    public TMP_Text aviso;
    public Button guardarSalir;
    public Button descartarSalir;
    public Button salir;
    public Button cancelar;
    public Button cambiar;

    public bool ReferenciasValidas(int tipo)
    {
        if (!(transform is RectTransform)) return false;
        if (tipo == 0)
        {
            if (panelNumerico == null || rejillaNumeros == null || cerrarExterior == null || borrar == null || numeros == null || numeros.Length != 9) return false;
            for (int i = 0; i < numeros.Length; i++) if (numeros[i] == null) return false;
            return true;
        }
        if (tipo == 1) return pregunta != null && aviso != null && guardarSalir != null && descartarSalir != null && salir != null && cancelar != null;
        return tipo == 2 && pregunta != null && cancelar != null && cambiar != null;
    }
}
