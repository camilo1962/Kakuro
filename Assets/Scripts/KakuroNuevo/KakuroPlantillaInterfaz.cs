using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Referencias del prefab editable. La lógica permanece en DemoKakuroMovil.
public sealed class KakuroPlantillaInterfaz : MonoBehaviour
{
    [Header("Cabecera")]
    public Button salir;
    public Button pausa;
    public TMP_Text estado;
    public TMP_Text tiempo;
    [Tooltip("Texto editable que muestra la dificultad y el nivel.")]
    public TMP_Text dificultadNivel;
    [Tooltip("Etiqueta fija encima del cronómetro.")]
    public TMP_Text rotuloTiempo;
    [Header("Acciones")]
    [Tooltip("Desactiva para configurar Cell Size manualmente en la rejilla.")]
    public bool ajustarBotonesAutomaticamente = true;
    public RectTransform zonaAcciones;
    public GridLayoutGroup distribucionAcciones;
    public Button menu;
    public Button nuevo;
    public Button reset;
    public Button pistas;
    public TMP_Text textoPistas;
    public Button comprobar;
    public Button rendirse;
    public TMP_Text avisoGuardado;

    public bool ReferenciasValidas()
    {
        return salir != null && pausa != null && estado != null && tiempo != null &&
            zonaAcciones != null && distribucionAcciones != null && menu != null &&
            nuevo != null && reset != null && pistas != null && textoPistas != null &&
            comprobar != null && rendirse != null && avisoGuardado != null &&
            distribucionAcciones.transform == zonaAcciones;
    }
}
